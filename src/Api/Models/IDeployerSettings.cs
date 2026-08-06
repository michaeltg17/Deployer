namespace Api.Models;

public interface IDeployerSettings
{
    public string KeePassDbPath { get; }
    public string KeePassDbPassword { get; }
    public bool ThrowIfNoSecrets { get; }
    public string ProjectsDir { get; }
}
