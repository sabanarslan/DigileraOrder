using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace OrderApi.Extensions
{
    public  static class ValidationExtension
    {
        public static ValidationProblemDetails ToProblemDetails(this ValidationResult validationResult)
        {
            if (validationResult == null)
            {
                return null;
            }

            var problemDetails = new ValidationProblemDetails(
                validationResult.Errors
                                .GroupBy(e => e.PropertyName)
                                .ToDictionary(
                                    g => g.Key,
                                    g => g.Select(e => e.ErrorMessage).ToArray()
                                )
            )
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = "See the errors property for details."
            };

            return problemDetails;
        }
    }
}
