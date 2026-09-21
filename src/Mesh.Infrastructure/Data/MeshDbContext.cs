using System.Text.Json;
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
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

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

        builder.Entity<Entry>(entity =>
        {
            entity.Property(e => e.Tags)
                  .HasConversion(
                      tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
                      json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
        });

        builder.Entity<TaskItem>(entity =>
        {
            entity.Property(t => t.Tags)
                  .HasConversion(
                      tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
                      json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.HasOne(t => t.User)
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => t.UserId);

            entity.HasMany(t => t.Checklist)
                  .WithOne(c => c.TaskItem)
                  .HasForeignKey(c => c.TaskItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
