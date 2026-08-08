using Microsoft.Extensions.Options;
using Api.Settings;

namespace Api.Validation;

public sealed class DeployerSettingsValidator : IValidateOptions<DeployerSettings>
{
    public ValidateOptionsResult Validate(string? name, DeployerSettings options)
    {
        if (options is null)
            return ValidateOptionsResult.Fail("DeployerSettings must not be null");

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.KeePassDbPath))
            errors.Add($"The '{nameof(options.KeePassDbPath)}' setting is required");

        if (string.IsNullOrWhiteSpace(options.KeePassDbPassword))
            errors.Add($"The '{nameof(options.KeePassDbPassword)}' setting is required");

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}