using System.ComponentModel.DataAnnotations;

namespace Mesh.Api.Auth;

public record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password,
    [property: Required] string DisplayName);

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public record RefreshRequest([property: Required] string RefreshToken);

public record GoogleAuthRequest([property: Required] string IdToken);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
