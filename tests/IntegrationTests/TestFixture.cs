using Api.Models;
using Api.Services;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.XUnit.Injectable;

namespace IntegrationTests;

public sealed class TestFixture : WebApplicationFactory<Program>, IAsyncDisposable
{
    public string TestProjectsDir { get; }
    private readonly string testKdbxPath;
    private readonly DockerClient dockerClient;

    public IDockerClient DockerClient => dockerClient;

    public InjectableTestOutputSink InjectableTestOutputSink { get; set; } = new();

    public TestFixture()
    {
        var testRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(TestFixture).Assembly.Location)!, "..", "..", ".."));
        testKdbxPath = Path.Combine(testRoot, "test.kdbx");
        TestProjectsDir = Path.Combine(testRoot, "projects");

        var config = new DockerClientConfiguration();
        dockerClient = config.CreateClient();
        config.Dispose();
        CleanupTestContainers().Wait();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseSerilog((context, services, configuration) =>
        {
            Api.DependencyConfigurator.ApplyCommonSerilogConfiguration(context, services, configuration);
            configuration.WriteTo.Sink(InjectableTestOutputSink);
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { nameof(DeployerSettings.KeePassDbPath), testKdbxPath },
                { nameof(DeployerSettings.KeePassDbPassword), "test" },
                { nameof(DeployerSettings.ProjectsDir), TestProjectsDir },
                { nameof(DeployerSettings.ThrowIfNoSecrets), "false" },
            });
        });

        builder.ConfigureServices(services =>
        {
            var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IProcessRunner));
            if (existing != null)
                services.Remove(existing);
            services.AddSingleton<IProcessRunner>(sp => new ProcessRunner());
        });
    }

    private async Task CleanupTestContainers()
    {
        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true });
        var testContainers = containers
            .Where(c => c.Names.Any(n => n.StartsWith("/deployer-test-", StringComparison.Ordinal)))
            .ToList();

        foreach (var container in testContainers)
        {
            if (container.State == "running")
            {
                await dockerClient.Containers.StopContainerAsync(container.ID,
                    new ContainerStopParameters());
            }
            await dockerClient.Containers.RemoveContainerAsync(container.ID,
                new ContainerRemoveParameters { Force = true });
        }
    }

    public async override ValueTask DisposeAsync()
    {
        await CleanupTestContainers();
        dockerClient.Dispose();
        await base.DisposeAsync();
    }
}