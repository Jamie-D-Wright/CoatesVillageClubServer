using System.Text.RegularExpressions;
using FluentValidation;
using VillageClub.Contracts.Enums;
using VillageClub.Membership.Models;

namespace VillageClub.Membership.Validators;

/// <summary>
/// Validator for CreateUserRequest.
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    private static readonly Regex _emailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserRequestValidator"/> class.
    /// </summary>
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .Must(email => _emailRegex.IsMatch(email ?? string.Empty))
            .WithMessage("Email must be a valid email address with a proper domain")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid user role");

        RuleFor(x => x.CommitteeRole)
            .IsInEnum().WithMessage("Invalid committee role")
            .When(x => x.CommitteeRole.HasValue);

        RuleFor(x => x.CommitteeRole)
            .NotNull().WithMessage("Committee role is required for Committee members")
            .When(x => x.Role == UserRole.Committee);
    }
}
