using FluentValidation;
using VillageClub.Contracts.Enums;
using VillageClub.Membership.Models;

namespace VillageClub.Membership.Validators;

/// <summary>
/// Validator for UpdateUserRequest.
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserRequestValidator"/> class.
    /// </summary>
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("First name cannot be empty or whitespace. Use null to keep existing value.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters")
            .When(x => x.FirstName != null);

        RuleFor(x => x.LastName)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("Last name cannot be empty or whitespace. Use null to keep existing value.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters")
            .When(x => x.LastName != null);

        RuleFor(x => x.PhoneNumber)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("Phone number cannot be empty or whitespace. Use null to keep existing value.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters")
            .When(x => x.PhoneNumber != null);

        RuleFor(x => x.Address)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("Address cannot be empty or whitespace. Use null to keep existing value.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters")
            .When(x => x.Address != null);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid user role")
            .When(x => x.Role.HasValue);

        RuleFor(x => x.CommitteeRole)
            .IsInEnum().WithMessage("Invalid committee role")
            .When(x => x.CommitteeRole.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid user status")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.CommitteeRole)
            .NotNull().WithMessage("Committee role is required for Committee members")
            .When(x => x.Role == UserRole.Committee);
    }
}
