using Mesh.Api.Auth;
using Mesh.Api.Journal;
using Mesh.Domain.Entities;
using Mesh.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mesh.Api.Controllers;

[ApiController]
[Route("api/v1/journal")]
[Authorize]
public class JournalController : ControllerBase
{
    private readonly MeshDbContext _db;

    public JournalController(MeshDbContext db) => _db = db;

    /// <summary>
    /// Sorted by Order, not Date - a deliberate deviation from the frontend
    /// mock, which sorts by Date on every fetch but has MoveEntry swap raw
    /// array positions and push that directly into the cache. Taken
    /// literally, that means a manual reorder gets silently undone the
    /// next time the list is refetched, since Date-sort doesn't care about
    /// the swap. That reads like a mock-only quirk, not intended behavior -
    /// Entries and Tasks already sort by Order for the same "a manual
    /// reorder should stick" reason, so Journal matches them here.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = this.GetUserId();
        var entries = await _db.JournalEntries
            .Where(j => j.UserId == userId)
            .OrderBy(j => j.Order)
            .ToListAsync();

        return Ok(entries.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = this.GetUserId();
        var entry = await _db.JournalEntries.FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);
        if (entry is null) return NotFound();

        return Ok(ToResponse(entry));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateJournalRequest request)
    {
        var userId = this.GetUserId();
        var maxOrder = await _db.JournalEntries
            .Where(j => j.UserId == userId)
            .Select(j => (int?)j.Order)
            .MaxAsync() ?? -1;

        var entry = new JournalEntry
        {
            UserId = userId,
            Date = request.Date,
            LogType = request.LogType,
            Content = request.Content ?? string.Empty,
            Wins = request.Wins ?? new List<string>(),
            Mistakes = request.Mistakes ?? new List<string>(),
            Learnings = request.Learnings ?? new List<string>(),
            Gratitude = request.Gratitude ?? new List<string>(),
            Mood = request.Mood ?? 3,
            Tags = request.Tags ?? new List<string>(),
            Order = maxOrder + 1,
        };

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, ToResponse(entry));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateJournalRequest request)
    {
        var userId = this.GetUserId();
        var entry = await _db.JournalEntries.FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);
        if (entry is null) return NotFound();

        if (request.Date.HasValue) entry.Date = request.Date.Value;
        if (request.LogType is not null) entry.LogType = request.LogType;
        if (request.Content is not null) entry.Content = request.Content;
        if (request.Wins is not null) entry.Wins = request.Wins;
        if (request.Mistakes is not null) entry.Mistakes = request.Mistakes;
        if (request.Learnings is not null) entry.Learnings = request.Learnings;
        if (request.Gratitude is not null) entry.Gratitude = request.Gratitude;
        if (request.Mood.HasValue) entry.Mood = request.Mood.Value;
        if (request.Tags is not null) entry.Tags = request.Tags;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(entry));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = this.GetUserId();
        var entry = await _db.JournalEntries.FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);
        if (entry is null) return NotFound();

        _db.JournalEntries.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Swaps this entry's Order with its immediate neighbor (by current
    /// Order ranking) and returns the whole list, re-sorted - matches the
    /// frontend's moveEntry contract exactly (it also returns the full list
    /// so the caller can replace its cache in one shot, not just this entry).
    /// </summary>
    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, MoveJournalRequest request)
    {
        var userId = this.GetUserId();
        var entries = await _db.JournalEntries
            .Where(j => j.UserId == userId)
            .OrderBy(j => j.Order)
            .ToListAsync();

        var idx = entries.FindIndex(j => j.Id == id);
        if (idx == -1) return NotFound();

        var swapWith = request.Direction == "up" ? idx - 1
                     : request.Direction == "down" ? idx + 1
                     : -1;

        if (swapWith is >= 0 && swapWith < entries.Count)
        {
            (entries[idx].Order, entries[swapWith].Order) = (entries[swapWith].Order, entries[idx].Order);
            await _db.SaveChangesAsync();
        }

        var reordered = entries.OrderBy(j => j.Order).Select(ToResponse);
        return Ok(reordered);
    }

    /// <summary>
    /// Computed aggregate, not tied to any single entity - counts
    /// consecutive calendar days with at least one entry, starting from
    /// today and going backward. Matches the frontend mock's algorithm
    /// exactly.
    /// </summary>
    [HttpGet("streak")]
    public async Task<IActionResult> GetStreakDays()
    {
        var userId = this.GetUserId();
        var dates = await _db.JournalEntries
            .Where(j => j.UserId == userId)
            .Select(j => j.Date.Date)
            .Distinct()
            .ToListAsync();
        var dateSet = dates.ToHashSet();

        var streak = 0;
        var cursor = DateTime.UtcNow.Date;
        while (dateSet.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return Ok(new { streakDays = streak });
    }

    private static JournalResponse ToResponse(JournalEntry j) => new(
        j.Id, j.Date, j.LogType, j.Content, j.Wins, j.Mistakes, j.Learnings, j.Gratitude,
        j.Mood, j.Tags, j.Order, j.CreatedAtUtc, j.UpdatedAtUtc);
}
