using Api.Exceptions;
using Api.Models;
using Api.Validation;

namespace Api.Services;

internal sealed partial class DeploymentService(
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

        LogDeploying(request.Project!, request.Environment!);

        LogExtractingEnv(request.Project!, request.Environment!);
        var envVars = await keepassEnvService.ExtractEnvVariables(request.Project!, request.Environment!);
        envVars["TAG"] = request.Tag!;

        var composeArgs = BuildComposeArgs(projectDir, request.Environment!);
        LogRunningCompose(composeArgs);
        var composeResult = await processRunner.Run("docker", composeArgs, 300_000, projectDir, envVars);
        if (composeResult.ExitCode != 0)
        {
            LogComposeFailed(composeResult.Stderr);
            throw new DeployerException($"Failed to start services: {composeResult.Stderr}");
        }

        LogDeploySuccess(request.Tag!, request.Project!, request.Environment!);
    }

    static string BuildComposeArgs(string projectDir, string environment)
    {
        var envComposeFile = Path.Combine(projectDir, $"docker-compose.{environment}.yml");
        var hasEnvFile = File.Exists(envComposeFile);

        return hasEnvFile
            ? $"compose -f \"docker-compose.yml\" -f \"docker-compose.{environment}.yml\" up -d --force-recreate"
            : $"compose -f \"docker-compose.yml\" up -d --force-recreate";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Deploying {project}/{environment}.")]
    partial void LogDeploying(string project, string environment);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracting .env for {project}/{environment} from KeePass.")]
    partial void LogExtractingEnv(string project, string environment);

    [LoggerMessage(Level = LogLevel.Information, Message = "Running docker compose: {composeArgs}.")]
    partial void LogRunningCompose(string composeArgs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Compose up failed: {stderr}.")]
    partial void LogComposeFailed(string stderr);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully deployed tag {tag} to {project}/{environment}.")]
    partial void LogDeploySuccess(string tag, string project, string environment);
}