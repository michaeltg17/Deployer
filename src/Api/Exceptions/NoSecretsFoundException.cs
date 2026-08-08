namespace Api.Exceptions;

internal sealed class NoSecretsFoundException(string project, string environment)
    : DeployerException($"No secrets found for project '{project}' and environment '{environment}'")
{
}