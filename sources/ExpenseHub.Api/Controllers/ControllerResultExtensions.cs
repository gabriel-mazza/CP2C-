using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Converte resultados de serviço em respostas HTTP com <c>ProblemDetails</c>.
/// </summary>
internal static class ControllerResultExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result) =>
        result.IsSuccess ? controller.Ok(result.Value) : controller.ToProblem(result.Error, result.Message);

    public static ObjectResult ToProblem(this ControllerBase controller, ServiceError error, string? detail)
    {
        int statusCode = error switch
        {
            ServiceError.Validation => StatusCodes.Status400BadRequest,
            ServiceError.Unauthorized => StatusCodes.Status401Unauthorized,
            ServiceError.Forbidden => StatusCodes.Status403Forbidden,
            ServiceError.NotFound => StatusCodes.Status404NotFound,
            ServiceError.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return controller.Problem(detail: detail, statusCode: statusCode);
    }
}
