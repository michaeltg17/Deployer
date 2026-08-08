using Api.Exceptions;
using Api.Settings;

namespace Api.Services;

internal sealed partial class KeePassEnvService(
    ILogger<KeePassEnvService> logger,
    IDeployerSettings settings,
    ProcessRunner processRunner)
{
    private readonly string projectsGroup = "projects";

    internal async Task<Dictionary<string, string>> ExtractEnvVariables(string project, string environment)
    {
        var vars = new Dictionary<string, string>(StringComparer.Ordinal);

        var common = await ExtractAttachment(project, ".env");
        if (!string.IsNullOrEmpty(common))
            ParseEnvContent(common, vars);

        var envSpecific = await ExtractAttachment(project, $".env.{environment}");
        if (!string.IsNullOrEmpty(envSpecific))
            ParseEnvContent(envSpecific, vars);

        LogEnvExtracted(project, environment, vars.Count);

        return settings.ThrowIfNoSecrets && vars.Count == 0
            ? throw new NoSecretsFoundException(project, environment)
            : vars;
    }

    private static void ParseEnvContent(string content, Dictionary<string, string> vars)
    {
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var equalsIndex = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex <= 0)
                continue;

            var key = trimmed[..equalsIndex].Trim();
            var value = trimmed[(equalsIndex + 1)..].Trim();

            if (key.Length > 0)
                vars[key] = value;
        }
    }

    private async Task<string> ExtractAttachment(string project, string attachmentName)
    {
        var arguments = $"attachment-export --stdout \"{settings.KeePassDbPath}\" \"{projectsGroup}/{project}\" \"{attachmentName}\"";
        var result = await processRunner.Run("keepassxc-cli", arguments, stdinInput: $"{settings.KeePassDbPassword}\n");

        if (result.ExitCode != 0)
        {
            LogKeePassCliFailed(result.ExitCode, projectsGroup, project, attachmentName, result.Stderr);
            return string.Empty;
        }

        return result.Stdout;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracted {count} environment variables for {project}/{environment}.")]
    partial void LogEnvExtracted(string project, string environment, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "keepassxc-cli exit={exitCode} for {group}/{entry}/{attachment}: {stderr}.")]
    partial void LogKeePassCliFailed(int exitCode, string group, string entry, string attachment, string stderr);
}