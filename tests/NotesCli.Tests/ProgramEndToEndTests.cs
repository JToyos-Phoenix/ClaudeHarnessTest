using System.Diagnostics;

namespace NotesCli.Tests;

/// <summary>Level 2 verification (docs/verification.md): runs the real built executable as a separate process.</summary>
public sealed class ProgramEndToEndTests : IDisposable
{
    private readonly TempDirectory _tmp = new();

    public void Dispose() => _tmp.Dispose();

    [Fact]
    public void Executable_UsesNotesFileEnvironmentVariable()
    {
        var notesFile = _tmp.Combine("notes.json");

        var add = RunExecutable(notesFile, "add", "hola", "--body", "mundo");
        var list = RunExecutable(notesFile, "list");

        Assert.Equal(0, add.Code);
        Assert.Contains("id=1", add.Output);
        Assert.Equal(0, list.Code);
        Assert.Contains("hola", list.Output);
        Assert.True(File.Exists(notesFile));
    }

    [Fact]
    public void Executable_ReturnsExitCodeOne_ForDomainErrors()
    {
        var result = RunExecutable(_tmp.Combine("notes.json"), "show", "99");

        Assert.Equal(1, result.Code);
        Assert.Contains("99", result.Error);
    }

    private static (int Code, string Output, string Error) RunExecutable(string notesFile, params string[] args)
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "NotesCli.dll");
        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(dll);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        startInfo.Environment["NOTES_FILE"] = notesFile;

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, output.Result, error.Result);
    }
}
