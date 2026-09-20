namespace Mesh.Domain.Entities;

/// <summary>
/// Named TaskItem, not Task, to avoid colliding with System.Threading.Tasks.Task
/// (which is used constantly throughout this codebase for async methods).
/// </summary>
public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";       // Open / In Progress / Done / Archived
    public string Priority { get; set; } = "Medium";   // Low / Medium / High
    public DateTime? DueDate { get; set; }
    public int? EffortEstimateHours { get; set; }
    public string Recurring { get; set; } = "None";    // None / Daily / Weekly / Monthly

    public List<string> Tags { get; set; } = new();    // JSON column, same pattern as Entry.Tags
    public bool Favorite { get; set; }
    public int Order { get; set; }

    public List<ChecklistItem> Checklist { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
