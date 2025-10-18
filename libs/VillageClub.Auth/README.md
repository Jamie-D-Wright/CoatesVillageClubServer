# VillageClub.Auth

**Framework-agnostic authentication utilities library for Coates Village Club Server**

## Purpose

Provides **low-level authentication utilities** that can be reused across all microservices in the Village Club system. This library is completely independent of Azure Functions, ASP.NET, Entity Framework, or any specific hosting framework.

## What Belongs Here

✅ **Pure utilities with no infrastructure dependencies:**
- `PasswordHashService` - BCrypt password hashing
- `JwtTokenService` - JWT token generation and validation

❌ **What does NOT belong here:**
- AuthService - stays in Membership service (requires DbContext, domain entities, orchestration)
- UserService - stays in Membership service (requires DbContext, domain entities)
- Any service that depends on Entity Framework, databases, or domain entities

## Responsibilities

- **JWT Token Management**: Generate, validate JWT tokens with role-based claims
- **Password Security**: Hash and verify passwords using BCrypt
- **No Business Logic**: This is a utility library, not a domain service library

## Public API

### IJwtTokenService

```csharp
public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string role, string? committeeRole = null);
    string GenerateRefreshToken();
    TokenValidationResult ValidateToken(string token);
    string GetPublicKey();
}
```

### IPasswordHashService

```csharp
public interface IPasswordHashService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

## Dependencies

- **BCrypt.Net-Next**: Secure password hashing
- **System.IdentityModel.Tokens.Jwt**: JWT token generation and validation
- **VillageClub.Contracts**: Shared DTOs and interfaces

## Framework Independence

This library:
- ✅ Has NO dependencies on Azure Functions
- ✅ Has NO dependencies on ASP.NET Core
- ✅ Has NO dependencies on Entity Framework Core
- ✅ Can be used in any .NET 8.0 application (Functions, Web API, Console, Worker Services)

## Testing Strategy

### Contract Tests
- Interface compliance for all public APIs
- Password hashing behavior (BCrypt format, length, verification)
- JWT token operations (generation, validation)

### Unit Tests
- JWT token validation with expired tokens
- Password verification with incorrect passwords
- Edge cases and error scenarios

**All tests use REAL implementations** - no mocking of BCrypt or JWT cryptography (per Constitution Principle VI).

## Usage Example

```csharp
// In your Membership service (or any microservice)
public class AuthService
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly MembershipDbContext _context;

    public AuthService(
        IJwtTokenService jwtTokenService,
        IPasswordHashService passwordHashService,
        MembershipDbContext context)
    {
        _jwtTokenService = jwtTokenService;
        _passwordHashService = passwordHashService;
        _context = context;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        
        if (user == null || !_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return null; // Invalid credentials
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, 
            user.Email, 
            user.Role.ToString(),
            user.CommitteeRole?.ToString());

        return new AuthResponse
        {
            AccessToken = accessToken,
            // ... other properties
        };
    }
}
```

## Version

**1.0.0** - Initial extraction from Membership service

## License

Internal use only - Coates Village Club Server
