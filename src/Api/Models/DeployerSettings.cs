namespace Api.Models;

internal sealed class DeployerSettings
{
    public required string KeePassDbPath { get; set; } = "deployer.kdbx";

    public required string KeePassDbPassword { get; set; }

    public string ProjectsDir { get; set; } = "/projects";
}
