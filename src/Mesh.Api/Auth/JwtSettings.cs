namespace Mesh.Api.Auth;

/// <summary>
/// Bound from the "Jwt" config section. Key comes from user-secrets in dev
/// (never committed) - see README for the exact command to set it.
/// </summary>
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
