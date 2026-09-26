using System.ComponentModel.DataAnnotations;

namespace Mesh.Api.Journal;

public record CreateJournalRequest(
    [property: Required] DateTime Date,
    [property: Required] string LogType,
    string? Content,
    List<string>? Wins,
    List<string>? Mistakes,
    List<string>? Learnings,
    List<string>? Gratitude,
    int? Mood,
    List<string>? Tags);

public record UpdateJournalRequest(
    DateTime? Date,
    string? LogType,
    string? Content,
    List<string>? Wins,
    List<string>? Mistakes,
    List<string>? Learnings,
    List<string>? Gratitude,
    int? Mood,
    List<string>? Tags);

public record MoveJournalRequest([property: Required] string Direction); // "up" or "down"

public record JournalResponse(
    Guid Id,
    DateTime Date,
    string LogType,
    string Content,
    List<string> Wins,
    List<string> Mistakes,
    List<string> Learnings,
    List<string> Gratitude,
    int Mood,
    List<string> Tags,
    int Order,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
