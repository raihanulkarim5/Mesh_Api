namespace Mesh.Domain.Entities;

/// <summary>
/// Its own table, not JSON like Tags - checklist items need individual
/// identity so a single item can be toggled (done/not done) without
/// touching the rest of the list.
/// </summary>
public class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    public string Text { get; set; } = string.Empty;
    public bool Done { get; set; }
    public int Order { get; set; }
}
