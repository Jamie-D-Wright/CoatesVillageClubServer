using VillageClub.Events.Core.Models;

namespace VillageClub.Events.Core.Services;

/// <summary>
/// Contains pure business logic for event state transitions and validation.
/// </summary>
/// <remarks>
/// This class is stateless and framework-agnostic, containing only pure functions
/// for event business rules. All methods are static to enforce functional programming principles.
/// </remarks>
public static class EventBusinessLogic
{
    /// <summary>
    /// Determines if an event can be published.
    /// </summary>
    /// <param name="currentStatus">The current status of the event.</param>
    /// <param name="endDateTime">The end date/time of the event.</param>
    /// <returns>True if the event can be published; otherwise, false.</returns>
    /// <remarks>
    /// An event can be published if:
    /// - Current status is Draft.
    /// - End date/time is in the future.
    /// </remarks>
    public static bool CanPublish(EventStatus currentStatus, DateTime endDateTime)
    {
        return currentStatus == EventStatus.Draft && endDateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Determines if an event can be completed.
    /// </summary>
    /// <param name="currentStatus">The current status of the event.</param>
    /// <returns>True if the event can be completed; otherwise, false.</returns>
    /// <remarks>
    /// An event can be completed only if current status is Published.
    /// </remarks>
    public static bool CanComplete(EventStatus currentStatus)
    {
        return currentStatus == EventStatus.Published;
    }

    /// <summary>
    /// Determines if an event can be cancelled.
    /// </summary>
    /// <param name="currentStatus">The current status of the event.</param>
    /// <returns>True if the event can be cancelled; otherwise, false.</returns>
    /// <remarks>
    /// An event can be cancelled if current status is Draft or Published.
    /// Completed and already Cancelled events cannot be cancelled.
    /// </remarks>
    public static bool CanCancel(EventStatus currentStatus)
    {
        return currentStatus == EventStatus.Draft || currentStatus == EventStatus.Published;
    }

    /// <summary>
    /// Validates a state transition from current status to new status.
    /// </summary>
    /// <param name="currentStatus">The current status of the event.</param>
    /// <param name="newStatus">The desired new status.</param>
    /// <param name="endDateTime">The end date/time of the event.</param>
    /// <returns>A validation result indicating whether the transition is valid.</returns>
    public static StateTransitionResult ValidateStateTransition(
        EventStatus currentStatus,
        EventStatus newStatus,
        DateTime endDateTime)
    {
        // No transition needed if status is the same
        if (currentStatus == newStatus)
        {
            return StateTransitionResult.Success();
        }

        // Validate specific transitions based on state diagram:
        // Draft → Published → Completed
        //   ↓         ↓
        //   └──→ Cancelled
        switch (newStatus)
        {
            case EventStatus.Published:
                if (!CanPublish(currentStatus, endDateTime))
                {
                    if (currentStatus != EventStatus.Draft)
                    {
                        return StateTransitionResult.Failure(
                            $"Cannot publish event from {currentStatus} status. Only Draft events can be published.");
                    }
                    else
                    {
                        return StateTransitionResult.Failure(
                            "Cannot publish event with end date in the past.");
                    }
                }

                break;

            case EventStatus.Completed:
                if (!CanComplete(currentStatus))
                {
                    return StateTransitionResult.Failure(
                        $"Cannot transition from {currentStatus} to Completed status. Only Published events can be completed.");
                }

                break;

            case EventStatus.Cancelled:
                if (!CanCancel(currentStatus))
                {
                    return StateTransitionResult.Failure(
                        $"Cannot cancel event from {currentStatus} status. Only Draft or Published events can be cancelled.");
                }

                break;

            case EventStatus.Draft:
                return StateTransitionResult.Failure(
                    $"Cannot move event back to Draft from {currentStatus} status.");

            default:
                return StateTransitionResult.Failure($"Unknown status: {newStatus}");
        }

        return StateTransitionResult.Success();
    }
}
