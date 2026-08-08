using Api.Builders;
using Api.Settings;
using Core.Builders;

namespace Core.Testing.Builders;

public class DeployerSettingsBuilder : BuilderWithValues<DeployerSettingsBuilder, DeployerSettings>
{
    protected override DeployerSettings Item { get; set; }

    public DeployerSettingsBuilder()
    {
        Item = new DeployerSettings
        {
            KeePassDbPath = "secrets.kdbx",
            KeePassDbPassword = "password",
            ThrowIfNoSecrets = true,
            ProjectsDir = "/projects"
        };
    }
}
