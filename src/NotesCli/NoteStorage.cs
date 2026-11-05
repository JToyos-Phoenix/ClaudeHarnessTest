using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NotesCli;

/// <summary>Atomic persistence of notes in a JSON file.</summary>
public static class NoteStorage
{
    public const string DefaultFileName = ".notes.json";

    // snake_case + 2-space indent + raw UTF-8 keeps the file byte-compatible with the Python version.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string DefaultNotesPath =>
        Environment.GetEnvironmentVariable("NOTES_FILE") is { Length: > 0 } fromEnv ? fromEnv : DefaultFileName;

    public static List<Note> Load(string? path = null)
    {
        path ??= DefaultNotesPath;
        if (!File.Exists(path))
        {
            return [];
        }

        var json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<Note>>(json, JsonOptions) ?? [];
    }

    public static void Save(IReadOnlyList<Note> notes, string? path = null)
    {
        path ??= DefaultNotesPath;
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var tempPath = Path.Combine(directory, $".notes_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempPath, JsonSerializer.Serialize(notes, JsonOptions), new UTF8Encoding(false));
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            File.Delete(tempPath);
            throw;
        }
    }
}
