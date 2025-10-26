using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using VillageClub.Contracts.Models;

namespace VillageClub.Membership.Functions;

/// <summary>
/// Base class for Azure Functions providing consistent JSON serialization for HTTP responses.
/// All function classes should inherit from this to ensure camelCase responses.
/// </summary>
public abstract class BaseFunctionWithJson
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>
    /// Creates an HTTP response with JSON body using camelCase serialization.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="data">The data to serialize.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>HTTP response with JSON body.</returns>
    protected static async Task<HttpResponseData> CreateJsonResponse(
        HttpRequestData req,
        object data,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = req.CreateResponse(statusCode);
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await response.WriteStringAsync(json, Encoding.UTF8);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        return response;
    }

    /// <summary>
    /// Creates a success response with data (200 OK or custom status).
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="data">The data to return.</param>
    /// <param name="statusCode">The HTTP status code (default: 200 OK).</param>
    /// <returns>HTTP response with JSON body.</returns>
    protected static Task<HttpResponseData> CreateSuccessResponse(
        HttpRequestData req,
        object data,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return CreateJsonResponse(req, data, statusCode);
    }

    /// <summary>
    /// Creates an error response with ErrorResponse DTO.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="statusCode">The HTTP error status code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional error details.</param>
    /// <returns>HTTP response with error details.</returns>
    protected static Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string message,
        string? details = null)
    {
        var errorResponse = new ErrorResponse
        {
            Message = message,
            Code = ConvertStatusCodeToErrorCode(statusCode),
            Timestamp = DateTime.UtcNow,
        };

        return CreateJsonResponse(req, errorResponse, statusCode);
    }

    /// <summary>
    /// Creates a validation error response (400 Bad Request).
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="errors">The validation errors.</param>
    /// <returns>HTTP response with validation errors.</returns>
    protected static Task<HttpResponseData> CreateValidationErrorResponse(
        HttpRequestData req,
        string[] errors)
    {
        var errorResponse = new ErrorResponse
        {
            Message = "Validation failed",
            Code = "VALIDATION_ERROR",
            Timestamp = DateTime.UtcNow,
            ValidationErrors = new Dictionary<string, string[]>
            {
                { "general", errors },
            },
        };

        return CreateJsonResponse(req, errorResponse, HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Deserializes request body with case-insensitive property matching.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="req">The HTTP request.</param>
    /// <returns>The deserialized object or null.</returns>
    protected static async Task<T?> DeserializeRequestAsync<T>(HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        return JsonSerializer.Deserialize<T>(body, options);
    }

    /// <summary>
    /// Converts HTTP status code to machine-readable error code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>Error code string.</returns>
    private static string ConvertStatusCodeToErrorCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "BAD_REQUEST",
            HttpStatusCode.Unauthorized => "UNAUTHORIZED",
            HttpStatusCode.Forbidden => "FORBIDDEN",
            HttpStatusCode.NotFound => "NOT_FOUND",
            HttpStatusCode.Conflict => "CONFLICT",
            HttpStatusCode.InternalServerError => "INTERNAL_SERVER_ERROR",
            _ => "ERROR",
        };
    }
}
