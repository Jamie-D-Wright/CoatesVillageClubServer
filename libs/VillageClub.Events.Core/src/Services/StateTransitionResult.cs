namespace VillageClub.Events.Core.Services;

/// <summary>
/// Represents the result of a state transition validation.
/// </summary>
public class StateTransitionResult
{
    private StateTransitionResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the state transition is valid.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the error message if the transition is invalid.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful state transition result.</returns>
    public static StateTransitionResult Success()
    {
        return new StateTransitionResult(true);
    }

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the transition failed.</param>
    /// <returns>A failed state transition result.</returns>
    public static StateTransitionResult Failure(string errorMessage)
    {
        return new StateTransitionResult(false, errorMessage);
    }
}
