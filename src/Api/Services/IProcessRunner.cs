using Api.Models;

namespace Api.Services;

public interface IProcessRunner
{
    Task<ProcessResult> Run(
        string fileName,
        string arguments,
        int timeoutMs,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        string? stdinInput = null,
        CancellationToken cancellationToken = default);
}
