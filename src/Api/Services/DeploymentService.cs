using Api.Exceptions;
using Api.Logging;
using Api.Models;
using Api.Validation;

namespace Api.Services;

internal sealed class DeploymentService(
    ILogger<DeploymentService> logger,
    IDeployerSettings settings,
    KeePassEnvService keepassEnvService, IProcessRunner processRunner)
{
    public async Task Deploy(DeployRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (DeployRequestValidator.Validate(request) is { } validationEx)
            throw validationEx;

        var projectDir = Path.Combine(settings.ProjectsDir, request.Project!);
        var baseComposeFile = Path.Combine(projectDir, "docker-compose.yml");

        if (!File.Exists(baseComposeFile))
            throw new InvalidDeployRequestException($"Docker compose file not found for project '{request.Project}': {baseComposeFile}");

        logger.LogDeploying(request.Project!, request.Environment!);

        logger.LogExtractingEnv(request.Project!, request.Environment!);
        var envVars = await keepassEnvService.ExtractEnvVariables(request.Project!, request.Environment!);
        envVars["TAG"] = request.Tag!;

        var composeArgs = BuildComposeArgs(projectDir, request.Environment!);
        logger.LogRunningCompose(composeArgs);
        var composeResult = await processRunner.Run("docker", composeArgs, 300_000, projectDir, envVars);
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