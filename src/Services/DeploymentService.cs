using Api.Exceptions;
using Api.Logging;
using Api.Models;
using Api.Validation;
using Microsoft.Extensions.Options;

namespace Api.Services;

internal sealed class DeploymentService(
    ILogger<DeploymentService> logger,
    IOptions<DeployerSettings> settings,
    KeePassEnvService keepassEnvService, IProcessRunner processRunner)
{
    private readonly DeployerSettings deployerSettings = settings.Value;

    public async Task Deploy(DeployRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (DeployRequestValidator.Validate(request) is { } validationEx)
            throw validationEx;

        var projectDir = Path.Combine(deployerSettings.ProjectsDir, request.Project!);
        var baseComposeFile = Path.Combine(projectDir, "docker-compose.yml");

        if (!File.Exists(baseComposeFile))
            throw new InvalidDeployRequestException($"Docker compose file not found for project '{request.Project}': {baseComposeFile}");

        logger.LogDeploying(request.Project!, request.Environment!);

        logger.LogExtractingEnv(request.Project!, request.Environment!);
        var envVars = await keepassEnvService.ExtractEnvVariables(request.Project!, request.Environment!).ConfigureAwait(false);
        envVars["TAG"] = request.Tag!;

        var composeArgs = BuildComposeArgs(projectDir, request.Environment!);
        logger.LogRunningCompose(composeArgs);
        var composeResult = await processRunner.Run("docker", composeArgs, 300_000, projectDir, envVars).ConfigureAwait(false);
        if (composeResult.ExitCode != 0)
        {
            logger.LogComposeFailed(composeResult.Stderr);
            throw new DeployerException($"Failed to start services: {composeResult.Stderr}");
        }

        logger.LogDeploySuccess(request.Tag!, request.Project!, request.Environment!);
    }

    static string BuildComposeArgs(string projectDir, string environment)
    {
        var envComposeFile = Path.Combine(projectDir, $"docker-compose.{environment}.yml");
        var hasEnvFile = File.Exists(envComposeFile);

        return hasEnvFile
            ? $"compose -f \"docker-compose.yml\" -f \"docker-compose.{environment}.yml\" up -d --force-recreate"
            : $"compose -f \"docker-compose.yml\" up -d --force-recreate";
    }
}