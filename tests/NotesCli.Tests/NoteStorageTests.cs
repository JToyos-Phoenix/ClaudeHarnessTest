using System.Text.Json;

namespace NotesCli.Tests;

/// <summary>Tests for src/NotesCli/NoteStorage.cs.</summary>
public sealed class NoteStorageTests : IDisposable
{
    private readonly TempDirectory _tmp = new();
    private readonly string _path;

    public NoteStorageTests() => _path = _tmp.Combine("notes.json");

    public void Dispose() => _tmp.Dispose();

    [Fact]
    public void Load_ReturnsEmpty_WhenFileMissing()
    {
        Assert.Empty(NoteStorage.Load(_path));
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        List<Note> notes = [new(1, "hola", "mundo", "2026-04-27T00:00:00+00:00")];

        NoteStorage.Save(notes, _path);

        Assert.Equal(notes, NoteStorage.Load(_path));
    }

    [Fact]
    public void Save_IsAtomic_AndLeavesNoTempFiles()
    {
        List<Note> notes = [new(1, "x", "y", "z")];

        NoteStorage.Save(notes, _path);
        NoteStorage.Save(notes, _path);

        Assert.Equal(notes, NoteStorage.Load(_path));
        Assert.Empty(Directory.GetFiles(_tmp.Path, ".notes_*"));
    }

    [Fact]
    public void Save_WritesSnakeCaseKeys()
    {
        NoteStorage.Save([new(1, "a", "b", "2026-04-27T00:00:00+00:00")], _path);

        using var document = JsonDocument.Parse(File.ReadAllText(_path));
        var keys = document.RootElement[0].EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Equal(new[] { "id", "title", "body", "created_at" }, keys);
    }

    [Fact]
    public void Load_ReadsFileWrittenByPythonVersion()
    {
        const string pythonJson = """
            [
              {
                "id": 7,
                "title": "comprar pan",
                "body": "y leche, señor",
                "created_at": "2026-04-27T14:00:56+00:00"
              }
            ]
            """;
        File.WriteAllText(_path, pythonJson);

        var notes = NoteStorage.Load(_path);

        Assert.Equal([new Note(7, "comprar pan", "y leche, señor", "2026-04-27T14:00:56+00:00")], notes);
    }
}
