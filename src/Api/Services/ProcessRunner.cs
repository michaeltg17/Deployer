using Api.Models;
using System.Diagnostics;

namespace Api.Services;

internal sealed class ProcessRunner
{
    internal async Task<ProcessResult> Run(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        string? stdinInput = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdinInput != null,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (workingDirectory != null)
            psi.WorkingDirectory = workingDirectory;

        if (environmentVariables != null)
        {
            foreach (var (key, value) in environmentVariables)
                psi.EnvironmentVariables[key] = value;
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

        if (stdinInput != null)
        {
            await process.StandardInput.WriteAsync(stdinInput.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
        }

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Stdout = await stdoutTask.ConfigureAwait(false),
            Stderr = await stderrTask.ConfigureAwait(false)
        };
    }
}