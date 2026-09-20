using Mesh.Api.Auth;
using Mesh.Api.Entries;
using Mesh.Domain.Entities;
using Mesh.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mesh.Api.Controllers;

[ApiController]
[Route("api/v1/entries")]
[Authorize]
public class EntriesController : ControllerBase
{
    private readonly MeshDbContext _db;

    public EntriesController(MeshDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = this.GetUserId();
        var entries = await _db.Entries
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Order)
            .ToListAsync();

        return Ok(entries.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = this.GetUserId();
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entry is null) return NotFound();

        return Ok(ToResponse(entry));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEntryRequest request)
    {
        var userId = this.GetUserId();
        var maxOrder = await _db.Entries
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.Order)
            .MaxAsync() ?? -1;

        var entry = new Entry
        {
            UserId = userId,
            Title = request.Title,
            Type = request.Type,
            Description = request.Description ?? string.Empty,
            Tags = request.Tags ?? new List<string>(),
            ImageUrl = request.ImageUrl,
            Order = maxOrder + 1,
        };

        _db.Entries.Add(entry);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, ToResponse(entry));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateEntryRequest request)
    {
        var userId = this.GetUserId();
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entry is null) return NotFound();

        if (request.Title is not null) entry.Title = request.Title;
        if (request.Type is not null) entry.Type = request.Type;
        if (request.Description is not null) entry.Description = request.Description;
        if (request.Tags is not null) entry.Tags = request.Tags;
        if (request.ImageUrl is not null) entry.ImageUrl = request.ImageUrl;
        if (request.Favorite.HasValue) entry.Favorite = request.Favorite.Value;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(entry));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = this.GetUserId();
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entry is null) return NotFound();

        _db.Entries.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static EntryResponse ToResponse(Entry e) => new(
        e.Id, e.Title, e.Type, e.Description, e.Tags, e.ImageUrl, e.Favorite, e.Order, e.CreatedAtUtc, e.UpdatedAtUtc);
}
