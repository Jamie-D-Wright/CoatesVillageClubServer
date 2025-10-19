using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using VillageClub.Contracts.Models;

namespace VillageClub.Membership.Middleware;

/// <summary>
/// Middleware to handle exceptions globally and return standardized error responses.
/// </summary>
public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred in function {FunctionName}", 
                context.FunctionDefinition.Name);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(FunctionContext context, Exception exception)
    {
        var (statusCode, errorCode, message) = MapException(exception);

        var errorResponse = new ErrorResponse
        {
            Message = message,
            Code = errorCode,
            Timestamp = DateTime.UtcNow,
            CorrelationId = context.InvocationId
        };

        // Add validation errors if it's a FluentValidation exception
        if (exception is ValidationException validationException)
        {
            errorResponse.ValidationErrors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        var httpRequestData = await context.GetHttpRequestDataAsync();
        if (httpRequestData != null)
        {
            var response = httpRequestData.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(errorResponse);
            
            var invocationResult = context.GetInvocationResult();
            invocationResult.Value = response;
        }
    }

    private (HttpStatusCode StatusCode, string ErrorCode, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                "One or more validation errors occurred."
            ),
            
            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                "FORBIDDEN",
                "You do not have permission to perform this action."
            ),
            
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                exception.Message
            ),
            
            InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("exist") => (
                HttpStatusCode.Conflict,
                "CONFLICT",
                invalidOpEx.Message
            ),
            
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                "BAD_REQUEST",
                argEx.Message
            ),
            
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred. Please try again later."
            )
        };
    }
}
