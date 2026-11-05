namespace NotesCli.Tests;

/// <summary>A real temporary directory, deleted on Dispose. The C# twin of Python's tempfile.TemporaryDirectory().</summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("notes-tests-").FullName;

    public string Combine(string fileName) => System.IO.Path.Combine(Path, fileName);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
