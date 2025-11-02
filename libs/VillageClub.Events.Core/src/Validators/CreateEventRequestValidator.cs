using FluentValidation;
using VillageClub.Events.Core.Models;

namespace VillageClub.Events.Core.Validators;

/// <summary>
/// Validator for CreateEventRequest.
/// Validates event creation requests according to business rules in data-model.md.
/// </summary>
public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventRequestValidator"/> class.
    /// </summary>
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(3, 200).WithMessage("Title must be between 3 and 200 characters.");

        RuleFor(x => x.StartDateTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("Start date and time must be in the future.");

        RuleFor(x => x.EndDateTime)
            .GreaterThan(x => x.StartDateTime).WithMessage("End date and time must be after start date and time.");

        RuleFor(x => x)
            .Must(x => (x.EndDateTime - x.StartDateTime).TotalMinutes >= 30)
            .WithMessage("Event duration must be at least 30 minutes.")
            .WithName("Duration");

        RuleFor(x => x)
            .Must(x => (x.EndDateTime - x.StartDateTime).TotalHours <= 12)
            .WithMessage("Event duration must not exceed 12 hours.")
            .WithName("Duration");
    }
}
