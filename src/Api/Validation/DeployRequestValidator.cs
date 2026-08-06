using Api.Models;
using Api.Settings;
using FluentValidation;

namespace Api.Validation;

public sealed class DeployRequestValidator : AbstractValidator<DeployRequest>
{
    public DeployRequestValidator(IDeployerSettings settings)
    {
        RuleFor(x => x.Project).NotEmpty();
        RuleFor(x => x.Project)
            .Must((request, project) => ComposeFileExists(project, settings))
            .WithMessage((request, project) => $"Docker compose file not found for project '{project}': docker-compose.yml");

        RuleFor(x => x.Environment).NotEmpty();
        RuleFor(x => x.Tag).NotEmpty();
    }

    static bool ComposeFileExists(string? project, IDeployerSettings settings)
    {
        if (string.IsNullOrEmpty(project)) return true;
        var projectDir = Path.Combine(settings.ProjectsDir, project);
        return File.Exists(Path.Combine(projectDir, "docker-compose.yml"));
    }
}
