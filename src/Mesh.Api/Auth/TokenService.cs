using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Mesh.Domain.Entities;
using Mesh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Mesh.Api.Auth;

/// <summary>
/// Issues/validates/revokes tokens. One concrete class, no interface -
/// there's only ever going to be one implementation of this.
/// </summary>
public class TokenService
{
    private readonly JwtSettings _settings;
    private readonly MeshDbContext _db;

    public TokenService(IOptions<JwtSettings> settings, MeshDbContext db)
    {
        _settings = settings.Value;
        _db = db;
    }

    public string CreateAccessToken(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> CreateRefreshTokenAsync(string userId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_settings.RefreshTokenDays),
        });
        await _db.SaveChangesAsync();
        return rawToken;
    }

    public async Task<RefreshToken?> FindActiveRefreshTokenAsync(string rawToken)
    {
        var hash = Hash(rawToken);
        var entity = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        return entity is { IsActive: true } ? entity : null;
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken token)
    {
        token.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static string Hash(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
