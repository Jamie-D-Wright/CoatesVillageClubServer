namespace VillageClub.Contracts.Models;

/// <summary>
/// Standard error response returned by all services.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the machine-readable error code (e.g., VALIDATION_ERROR, UNAUTHORIZED).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the error occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the optional detailed validation errors (field-level).
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }
}
