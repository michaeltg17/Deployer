namespace Api.Models;

public sealed class DeployerSettings : IDeployerSettings
{
    public required string KeePassDbPath { get; set; } = "secrets.kdbx";
    public required string KeePassDbPassword { get; set; }
    public required bool ThrowIfNoSecrets { get; set; } = true;
    public required string ProjectsDir { get; set; } = "/projects";
}
