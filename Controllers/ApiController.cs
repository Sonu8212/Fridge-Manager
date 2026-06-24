using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace FridgeManager.Api.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected IActionResult Problem(List<Error> errors)
    {
        if (errors.Count == 0) return Problem();

        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            var modelStateDictionary = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            foreach (var error in errors)
                modelStateDictionary.AddModelError(error.Code, error.Description);
            return ValidationProblem(modelStateDictionary);
        }

        var first = errors[0];
        var statusCode = first.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, title: first.Description);
    }
}
