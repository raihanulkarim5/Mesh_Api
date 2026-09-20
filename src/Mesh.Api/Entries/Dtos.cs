using System.ComponentModel.DataAnnotations;

namespace Mesh.Api.Entries;

public record CreateEntryRequest(
    [property: Required] string Title,
    [property: Required] string Type,
    string? Description,
    List<string>? Tags,
    string? ImageUrl);

// All optional - this supports true partial updates (e.g. the frontend's
// favorite-toggle only ever sends { favorite }, not the whole entry).
// Known limitation: there's no way yet to distinguish "ImageUrl omitted"
// from "explicitly clear the image" - both look like null here. Fine for
// now since the only partial-update caller today is the favorite toggle;
// revisit if/when "remove image" needs to go through this same endpoint.
public record UpdateEntryRequest(
    string? Title,
    string? Type,
    string? Description,
    List<string>? Tags,
    string? ImageUrl,
    bool? Favorite);

public record EntryResponse(
    Guid Id,
    string Title,
    string Type,
    string Description,
    List<string> Tags,
    string? ImageUrl,
    bool Favorite,
    int Order,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
