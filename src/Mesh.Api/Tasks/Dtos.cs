using System.ComponentModel.DataAnnotations;

namespace Mesh.Api.Tasks;

public record CreateTaskRequest(
    [property: Required] string Title,
    string? Description,
    string? Priority,
    DateTime? DueDate,
    int? EffortEstimateHours,
    string? Recurring,
    List<string>? Tags);

// All optional, same partial-update approach as Entries' UpdateEntryRequest -
// the frontend's status-cycle and favorite-toggle actions each only send
// the one field they're changing. Checklist is included here (not as
// separate add/remove endpoints) because that's how the frontend's real
// TaskUpdate type works: adding or removing a checklist item means sending
// the whole modified array through this same endpoint. Only toggling a
// single item's done state is a dedicated action (see TogglePosition below).
public record UpdateTaskRequest(
    string? Title,
    string? Description,
    string? Status,
    string? Priority,
    DateTime? DueDate,
    int? EffortEstimateHours,
    string? Recurring,
    List<string>? Tags,
    List<ChecklistItemRequest>? Checklist,
    bool? Favorite);

public record ChecklistItemRequest(Guid? Id, [property: Required] string Text, bool Done);

public record ChecklistItemResponse(Guid Id, string Text, bool Done, int Order);

public record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    DateTime? DueDate,
    int? EffortEstimateHours,
    string Recurring,
    List<string> Tags,
    bool Favorite,
    int Order,
    List<ChecklistItemResponse> Checklist,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
