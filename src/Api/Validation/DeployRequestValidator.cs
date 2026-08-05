using Api.Models;
using FluentValidation;

namespace Api.Validation;

public sealed class DeployRequestValidator : AbstractValidator<DeployRequest>
{
    public DeployRequestValidator(IDeployerSettings settings)
    {
        RuleFor(x => x.Project)
            .NotEmpty()
            .Must((request, project) => ComposeFileExists(project, settings))
                .WithMessage((request, project) => $"Docker compose file not found for project '{project}': docker-compose.yml");

        RuleFor(x => x.Environment).NotEmpty();
        RuleFor(x => x.Tag).NotEmpty();
    }

    static bool ComposeFileExists(string? project, IDeployerSettings settings)
    {
        var projectDir = Path.Combine(settings.ProjectsDir, project!);
        return File.Exists(Path.Combine(projectDir, "docker-compose.yml"));
    }
}
