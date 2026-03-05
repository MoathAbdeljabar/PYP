namespace Identity.API.Helpers;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Shared;

public static class ResponseHelper
{
    public static IActionResult ToActionResult<T>(this ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            var response = new
            {
                IsSuccess = result.IsSuccess,
                Data = result.Data,
                Message = result.Message,
            };
            return new OkObjectResult(response);
        }

        var errorResponse = new
        {
            IsSuccess = result.IsSuccess,
            Message = result.Message,
        };

        return result.ErrorType switch
        {
            BusinessErrorType.None => new ObjectResult(errorResponse) { StatusCode = 500 },
            BusinessErrorType.EmailAlreadyExists => new BadRequestObjectResult(errorResponse),
            BusinessErrorType.CanNotCreateUser => new BadRequestObjectResult(errorResponse),
            BusinessErrorType.UserAlreadyExists => new BadRequestObjectResult(errorResponse),
            BusinessErrorType.UserNotFound => new NotFoundObjectResult(errorResponse),
            BusinessErrorType.InvalidToken => new BadRequestObjectResult(errorResponse),
            BusinessErrorType.InvalidCredentials => new BadRequestObjectResult(errorResponse),
            BusinessErrorType.InsufficientPermissions => new UnauthorizedObjectResult(errorResponse),
            BusinessErrorType.ResourceNotFound => new NotFoundObjectResult(errorResponse),
            BusinessErrorType.ValidationFailed => new BadRequestObjectResult(errorResponse),
            BusinessErrorType.OperationNotAllowed => new ObjectResult(errorResponse) { StatusCode = 403 },
            BusinessErrorType.PaymentRequired => new ObjectResult(errorResponse) { StatusCode = 402 },
            BusinessErrorType.SubscriptionExpired => new ObjectResult(errorResponse) { StatusCode = 402 },
            BusinessErrorType.EmailNotConfirmed => new BadRequestObjectResult(errorResponse),
            BusinessErrorType.AccountLocked => new ObjectResult(errorResponse) { StatusCode = 423 },
            BusinessErrorType.AccountDisabled => new ObjectResult(errorResponse) { StatusCode = 423 },
            BusinessErrorType.InvalidImage => new BadRequestObjectResult(errorResponse),
            BusinessErrorType.ConcurrencyConflict => new ConflictObjectResult(errorResponse),
            _ => new ObjectResult(errorResponse) { StatusCode = 500 }
        };
    }
}