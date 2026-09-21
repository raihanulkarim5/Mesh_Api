namespace Mesh.Domain.Entities;

/// <summary>
/// No Favorite field, unlike Entry/TaskItem - the frontend's Journal
/// entries never had a favorite concept by design. Has Order for the
/// same manual reordering (move up/down) the frontend supports.
/// </summary>
public class JournalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime Date { get; set; }
    public string LogType { get; set; } = "Daily"; // Daily / Office / Personal / Meeting
    public int Mood { get; set; } = 3;              // 1-5
    public string Content { get; set; } = string.Empty; // rich HTML

    // All JSON columns, same pattern as Entry.Tags / TaskItem.Tags.
    public List<string> Wins { get; set; } = new();
    public List<string> Mistakes { get; set; } = new();
    public List<string> Learnings { get; set; } = new();
    public List<string> Gratitude { get; set; } = new();
    public List<string> Tags { get; set; } = new();

    public int Order { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
