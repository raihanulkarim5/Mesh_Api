namespace Mesh.Domain.Entities;

/// <summary>
/// Server-side record of an issued refresh token, so logout can actually
/// revoke it rather than just letting it expire naturally. We store a hash
/// of the token (not the raw value) - same principle as password storage:
/// if the database is ever exposed, the tokens in it aren't directly usable.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    /// SHA-256 hash of the actual refresh token value handed to the client.
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;
}
