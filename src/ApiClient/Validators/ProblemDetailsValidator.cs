using ApiClient.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ApiClient.Validators
{
    public sealed class ProblemDetailsValidator : AbstractValidator<ProblemDetails>
    {
        public ProblemDetailsValidator()
        {
            RuleFor(p => p.Type)
                .NotNull().WithMessage("Type is null.")
                .NotEmpty().WithMessage("Type is empty or whitespace.");

            RuleFor(p => p.Title)
                .NotNull().WithMessage("Title is null.")
                .NotEmpty().WithMessage("Title is empty or whitespace.");

            RuleFor(p => p.Status)
                .NotNull().WithMessage("Status is null.");
        }

        public static void EnsureValid(ProblemDetails problemDetails)
        {
            var validator = new ProblemDetailsValidator();
            var result = validator.Validate(problemDetails);

            if (!result.IsValid)
                throw new ApiClientException("Invalid ProblemDetails: " + string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
        }
    }
}