namespace Mesh.Domain.Entities;

/// <summary>
/// Type is a plain string, not a C# enum - it just needs to match whatever
/// the frontend's EntryType union sends (Note/Idea/Problem-Solution/
/// Reminder/Reference/Decision/Meeting Note). Validating against that set
/// happens at the request/DTO level, not here.
/// </summary>
public class Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // rich HTML

    /// Stored as a JSON array string via EF's value conversion (see
    /// MeshDbContext) - simplest option for a plain list of strings,
    /// no separate join table needed for something this small.
    public List<string> Tags { get; set; } = new();

    public string? ImageUrl { get; set; }
    public bool Favorite { get; set; }
    public int Order { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
