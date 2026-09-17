using Microsoft.AspNetCore.Identity;

namespace Mesh.Domain.Entities;

/// <summary>
/// Extends ASP.NET Core Identity's default user with the bits Mesh needs.
/// Email doubles as the username (Identity's UserName field is set to the
/// email on registration) - there's no separate username concept.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
