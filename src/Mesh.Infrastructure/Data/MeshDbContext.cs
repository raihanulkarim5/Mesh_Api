using Mesh.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Mesh.Infrastructure.Data;

/// <summary>
/// Inherits from IdentityDbContext to get the full Identity schema
/// (AspNetUsers, AspNetRoles, AspNetUserLogins - the last one is what
/// makes Google Sign-In auto-linking to an existing email work, it's
/// built in) for free. Domain entities for each module (Entries, Tasks,
/// Journal, Finance, etc.) get their own DbSet here as those modules'
/// backends get built - this is intentionally just Auth for now.
/// </summary>
public class MeshDbContext : IdentityDbContext<ApplicationUser>
{
    public MeshDbContext(DbContextOptions<MeshDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasOne(t => t.User)
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
