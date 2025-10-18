namespace VillageClub.Contracts.Models;

/// <summary>
/// Standard error response returned by all services
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Machine-readable error code (e.g., VALIDATION_ERROR, UNAUTHORIZED)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the error occurred (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional detailed validation errors (field-level)
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public string? CorrelationId { get; set; }
}
