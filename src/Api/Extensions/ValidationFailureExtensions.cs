using FluentValidation.Results;

namespace Api.Extensions
{
    internal static class ValidationFailureExtensions
    {
        public static IDictionary<string, string[]> ToValidationProblemErrors(
            this IEnumerable<ValidationFailure> validationFailures)
        {
            return validationFailures
                .GroupBy(vf => vf.PropertyName)
                .ToDictionary(gvf => gvf.Key, gvf => gvf.Select(vf => vf.ErrorMessage).ToArray());
        }
    }
}