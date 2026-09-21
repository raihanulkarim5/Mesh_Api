using Mesh.Api.Auth;
using Mesh.Api.Tasks;
using Mesh.Domain.Entities;
using Mesh.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mesh.Api.Controllers;

[ApiController]
[Route("api/v1/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly MeshDbContext _db;

    public TasksController(MeshDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = this.GetUserId();
        var tasks = await _db.Tasks
            .Include(t => t.Checklist)
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Order)
            .ToListAsync();

        return Ok(tasks.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = this.GetUserId();
        var task = await _db.Tasks
            .Include(t => t.Checklist)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (task is null) return NotFound();

        return Ok(ToResponse(task));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        var userId = this.GetUserId();
        var maxOrder = await _db.Tasks
            .Where(t => t.UserId == userId)
            .Select(t => (int?)t.Order)
            .MaxAsync() ?? -1;

        var task = new TaskItem
        {
            UserId = userId,
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            Priority = request.Priority ?? "Medium",
            DueDate = request.DueDate,
            EffortEstimateHours = request.EffortEstimateHours,
            Recurring = request.Recurring ?? "None",
            Tags = request.Tags ?? new List<string>(),
            Order = maxOrder + 1,
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, ToResponse(task));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request)
    {
        var userId = this.GetUserId();
        var task = await _db.Tasks
            .Include(t => t.Checklist)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (task is null) return NotFound();

        if (request.Title is not null) task.Title = request.Title;
        if (request.Description is not null) task.Description = request.Description;
        if (request.Status is not null) task.Status = request.Status;
        if (request.Priority is not null) task.Priority = request.Priority;
        if (request.DueDate.HasValue) task.DueDate = request.DueDate;
        if (request.EffortEstimateHours.HasValue) task.EffortEstimateHours = request.EffortEstimateHours;
        if (request.Recurring is not null) task.Recurring = request.Recurring;
        if (request.Tags is not null) task.Tags = request.Tags;
        if (request.Favorite.HasValue) task.Favorite = request.Favorite.Value;
        task.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(task));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = this.GetUserId();
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (task is null) return NotFound();

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ---- Checklist sub-resource ----

    [HttpPost("{taskId:guid}/checklist")]
    public async Task<IActionResult> AddChecklistItem(Guid taskId, ChecklistItemRequest request)
    {
        var userId = this.GetUserId();
        var task = await _db.Tasks
            .Include(t => t.Checklist)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        if (task is null) return NotFound();

        var item = new ChecklistItem
        {
            TaskItemId = task.Id,
            Text = request.Text,
            Order = task.Checklist.Count,
        };
        task.Checklist.Add(item);
        task.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(task));
    }

    [HttpPatch("{taskId:guid}/checklist/{itemId:guid}")]
    public async Task<IActionResult> ToggleChecklistItem(Guid taskId, Guid itemId)
    {
        var userId = this.GetUserId();
        var task = await _db.Tasks
            .Include(t => t.Checklist)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        if (task is null) return NotFound();

        var item = task.Checklist.FirstOrDefault(c => c.Id == itemId);
        if (item is null) return NotFound();

        item.Done = !item.Done;
        task.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(task));
    }

    [HttpDelete("{taskId:guid}/checklist/{itemId:guid}")]
    public async Task<IActionResult> RemoveChecklistItem(Guid taskId, Guid itemId)
    {
        var userId = this.GetUserId();
        var task = await _db.Tasks
            .Include(t => t.Checklist)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        if (task is null) return NotFound();

        var item = task.Checklist.FirstOrDefault(c => c.Id == itemId);
        if (item is null) return NotFound();

        task.Checklist.Remove(item);
        _db.Remove(item);
        task.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(task));
    }

    private static TaskResponse ToResponse(TaskItem t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate, t.EffortEstimateHours,
        t.Recurring, t.Tags, t.Favorite, t.Order,
        t.Checklist.OrderBy(c => c.Order).Select(c => new ChecklistItemResponse(c.Id, c.Text, c.Done, c.Order)).ToList(),
        t.CreatedAtUtc, t.UpdatedAtUtc);
}
