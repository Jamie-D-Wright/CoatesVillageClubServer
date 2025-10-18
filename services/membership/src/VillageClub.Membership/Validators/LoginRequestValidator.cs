using System.Text.RegularExpressions;
using FluentValidation;
using VillageClub.Membership.Models;

namespace VillageClub.Membership.Validators;

/// <summary>
/// Validator for LoginRequest.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    private static readonly Regex _emailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginRequestValidator"/> class.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .Must(email => _emailRegex.IsMatch(email ?? string.Empty))
            .WithMessage("Email must be a valid email address with a proper domain");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
