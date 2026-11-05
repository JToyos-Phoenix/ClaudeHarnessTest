using System.Globalization;

namespace NotesCli;

/// <summary>Domain model: the Note. Immutable; "modifying" means creating a new instance with <c>with</c>.</summary>
public sealed record Note(int Id, string Title, string Body, string CreatedAt)
{
    public static Note New(string title, string body, IEnumerable<Note> existing)
    {
        var nextId = existing.Select(n => n.Id).DefaultIfEmpty(0).Max() + 1;
        var createdAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        return new Note(nextId, title, body, createdAt);
    }
}

/// <summary>Base class for domain errors.</summary>
public class NoteException(string message) : Exception(message);

/// <summary>Thrown when a note that does not exist is requested.</summary>
public sealed class NoteNotFoundException(string message) : NoteException(message);
