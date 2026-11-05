namespace NotesCli.Tests;

/// <summary>Integration tests for src/NotesCli/Cli.cs against a real temporary notes file.</summary>
public sealed class CliTests : IDisposable
{
    private readonly TempDirectory _tmp = new();
    private readonly string _path;

    public CliTests() => _path = _tmp.Combine("notes.json");

    public void Dispose() => _tmp.Dispose();

    [Fact]
    public void Add_CreatesNote_AndPrintsId()
    {
        var (code, output, _) = Run("add", "primera", "--body", "hola");

        Assert.Equal(0, code);
        Assert.Contains("id=1", output);
        var notes = NoteStorage.Load(_path);
        Assert.Single(notes);
        Assert.Equal("primera", notes[0].Title);
    }

    [Fact]
    public void Add_AcceptsInlineOptionValue()
    {
        var (code, _, _) = Run("add", "titulo", "--body=cuerpo");

        Assert.Equal(0, code);
        Assert.Equal("cuerpo", NoteStorage.Load(_path)[0].Body);
    }

    [Fact]
    public void List_ShowsExistingNotes()
    {
        Run("add", "uno", "--body", "a");
        Run("add", "dos", "--body", "b");

        var (code, output, _) = Run("list");

        Assert.Equal(0, code);
        Assert.Contains("uno", output);
        Assert.Contains("dos", output);
        Assert.Equal(2, Lines(output).Length);
    }

    [Fact]
    public void List_WhenEmpty_OutputsNothing()
    {
        var (code, output, _) = Run("list");

        Assert.Equal(0, code);
        Assert.Equal("", output);
    }

    [Fact]
    public void Show_PrintsTitleDateAndBody()
    {
        Run("add", "titulo-uno", "--body", "cuerpo-uno");

        var (code, output, _) = Run("show", "1");

        Assert.Equal(0, code);
        var lines = Lines(output);
        Assert.Equal("titulo-uno", lines[0]);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T", lines[1]);
        Assert.Equal("cuerpo-uno", lines[2]);
    }

    [Fact]
    public void Show_WithMissingId_ReturnsError()
    {
        var (code, output, error) = Run("show", "99");

        Assert.NotEqual(0, code);
        Assert.Equal("", output);
        Assert.Contains("99", error);
    }

    [Fact]
    public void Show_WithNonNumericId_ReturnsUsageError()
    {
        var (code, output, error) = Run("show", "abc");

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Contains("invalid int value", error);
    }

    [Fact]
    public void Delete_RemovesNote_AndConfirms()
    {
        Run("add", "uno", "--body", "a");
        Run("add", "dos", "--body", "b");

        var (code, output, _) = Run("delete", "1");

        Assert.Equal(0, code);
        Assert.Contains("id=1", output);
        var notes = NoteStorage.Load(_path);
        Assert.Single(notes);
        Assert.Equal(2, notes[0].Id);
    }

    [Fact]
    public void Delete_WithMissingId_ReturnsError()
    {
        Run("add", "uno", "--body", "a");

        var (code, output, error) = Run("delete", "42");

        Assert.NotEqual(0, code);
        Assert.Equal("", output);
        Assert.Contains("42", error);
        Assert.Single(NoteStorage.Load(_path));
    }

    [Fact]
    public void Search_FindsMatchingNotes()
    {
        Run("add", "comprar leche", "--body", "en el super");
        Run("add", "llamar doctor", "--body", "cita lunes");

        var (code, output, _) = Run("search", "leche");

        Assert.Equal(0, code);
        Assert.Contains("comprar leche", output);
        Assert.DoesNotContain("llamar doctor", output);
    }

    [Fact]
    public void Search_WithNoMatch_ReturnsError()
    {
        Run("add", "comprar leche", "--body", "en el super");

        var (code, output, error) = Run("search", "zapatos");

        Assert.NotEqual(0, code);
        Assert.Equal("", output);
        Assert.Contains("zapatos", error);
    }

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        Run("add", "Comprar LECHE", "--body", "en el super");

        var (code, output, _) = Run("search", "comprar leche");

        Assert.Equal(0, code);
        Assert.Contains("Comprar LECHE", output);
    }

    [Fact]
    public void Edit_WithTitleOnly_UpdatesOnlyTitle()
    {
        Run("add", "viejo", "--body", "cuerpo");

        var (code, output, _) = Run("edit", "1", "--title", "nuevo");

        Assert.Equal(0, code);
        Assert.Contains("id=1", output);
        var note = NoteStorage.Load(_path)[0];
        Assert.Equal("nuevo", note.Title);
        Assert.Equal("cuerpo", note.Body);
    }

    [Fact]
    public void Edit_WithBodyOnly_UpdatesOnlyBody()
    {
        Run("add", "titulo", "--body", "viejo");

        var (code, output, _) = Run("edit", "1", "--body", "nuevo");

        Assert.Equal(0, code);
        Assert.Contains("id=1", output);
        var note = NoteStorage.Load(_path)[0];
        Assert.Equal("titulo", note.Title);
        Assert.Equal("nuevo", note.Body);
    }

    [Fact]
    public void Edit_WithBothFlags_UpdatesBothFields()
    {
        Run("add", "viejo-t", "--body", "viejo-b");

        var (code, output, _) = Run("edit", "1", "--title", "nuevo-t", "--body", "nuevo-b");

        Assert.Equal(0, code);
        Assert.Contains("id=1", output);
        var note = NoteStorage.Load(_path)[0];
        Assert.Equal("nuevo-t", note.Title);
        Assert.Equal("nuevo-b", note.Body);
    }

    [Fact]
    public void Edit_WithMissingId_ReturnsError()
    {
        Run("add", "uno", "--body", "a");

        var (code, output, error) = Run("edit", "99", "--title", "x");

        Assert.NotEqual(0, code);
        Assert.Equal("", output);
        Assert.Contains("99", error);
    }

    [Fact]
    public void Edit_WithoutFlags_ReturnsError()
    {
        Run("add", "uno", "--body", "a");

        var (code, output, error) = Run("edit", "1");

        Assert.NotEqual(0, code);
        Assert.Equal("", output);
        Assert.NotEqual("", error);
        var note = NoteStorage.Load(_path)[0];
        Assert.Equal("uno", note.Title);
        Assert.Equal("a", note.Body);
    }

    [Fact]
    public void Recent_ListsFiveNewestNotesFirst_ByDefault()
    {
        Seed(
            new Note(1, "uno", "", "2026-04-20T10:00:00+00:00"),
            new Note(2, "dos", "", "2026-04-26T10:00:00+00:00"),
            new Note(3, "tres", "", "2026-04-22T10:00:00+00:00"),
            new Note(4, "cuatro", "", "2026-04-25T10:00:00+00:00"),
            new Note(5, "cinco", "", "2026-04-21T10:00:00+00:00"),
            new Note(6, "seis", "", "2026-04-24T10:00:00+00:00"),
            new Note(7, "siete", "", "2026-04-23T10:00:00+00:00"));

        var (code, output, _) = Run("recent");

        Assert.Equal(0, code);
        string[] expected =
        [
            "2\t2026-04-26T10:00:00+00:00\tdos",
            "4\t2026-04-25T10:00:00+00:00\tcuatro",
            "6\t2026-04-24T10:00:00+00:00\tseis",
            "7\t2026-04-23T10:00:00+00:00\tsiete",
            "3\t2026-04-22T10:00:00+00:00\ttres",
        ];
        Assert.Equal(expected, Lines(output));
    }

    [Fact]
    public void Recent_WithLimit_ListsThatManyNewestNotes()
    {
        Seed([.. Enumerable.Range(1, 12).Select(i => new Note(i, $"nota-{i}", "", $"2026-04-{i:D2}T09:00:00+00:00"))]);

        var (code, output, _) = Run("recent", "--limit", "10");

        Assert.Equal(0, code);
        string[] expected = ["12", "11", "10", "9", "8", "7", "6", "5", "4", "3"];
        Assert.Equal(expected, Lines(output).Select(line => line.Split('\t')[0]));
    }

    [Fact]
    public void Recent_WithLimitAboveCount_ListsAllNotes()
    {
        Seed(
            new Note(1, "uno", "", "2026-04-20T10:00:00+00:00"),
            new Note(2, "dos", "", "2026-04-21T10:00:00+00:00"));

        var (code, output, _) = Run("recent", "--limit", "10");

        Assert.Equal(0, code);
        string[] expected = ["2\t2026-04-21T10:00:00+00:00\tdos", "1\t2026-04-20T10:00:00+00:00\tuno"];
        Assert.Equal(expected, Lines(output));
    }

    [Fact]
    public void Recent_ListsHigherIdFirst_WhenCreatedAtIsEqual()
    {
        Seed(
            new Note(1, "uno", "", "2026-04-20T10:00:00+00:00"),
            new Note(3, "tres", "", "2026-04-20T10:00:00+00:00"),
            new Note(2, "dos", "", "2026-04-20T10:00:00+00:00"));

        var (code, output, _) = Run("recent");

        Assert.Equal(0, code);
        string[] expected = ["3", "2", "1"];
        Assert.Equal(expected, Lines(output).Select(line => line.Split('\t')[0]));
    }

    [Fact]
    public void Recent_UsesSameLineFormatAsList()
    {
        Seed(
            new Note(1, "uno", "a", "2026-04-20T10:00:00+00:00"),
            new Note(2, "dos", "b", "2026-04-21T10:00:00+00:00"));
        var (_, listOutput, _) = Run("list");

        var (code, recentOutput, _) = Run("recent");

        Assert.Equal(0, code);
        Assert.Equal(Enumerable.Reverse(Lines(listOutput)), Lines(recentOutput));
    }

    [Fact]
    public void Recent_WhenFileMissing_OutputsNothing()
    {
        var (code, output, error) = Run("recent");

        Assert.Equal(0, code);
        Assert.Equal("", output);
        Assert.Equal("", error);
    }

    [Fact]
    public void Recent_WhenFileHasNoNotes_OutputsNothing()
    {
        Seed();

        var (code, output, error) = Run("recent");

        Assert.Equal(0, code);
        Assert.Equal("", output);
        Assert.Equal("", error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Recent_WithNonPositiveLimit_ReturnsUsageError(string limit)
    {
        Seed(new Note(1, "uno", "", "2026-04-20T10:00:00+00:00"));

        var (code, output, error) = Run("recent", "--limit", limit);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Contains("usage: notes recent", error);
        Assert.Contains($"argument --limit: must be a positive integer, got '{limit}'", error);
    }

    [Fact]
    public void Recent_WithNonNumericLimit_ReturnsUsageError()
    {
        var (code, output, error) = Run("recent", "--limit", "abc");

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Contains("argument --limit: invalid int value: 'abc'", error);
    }

    [Fact]
    public void Run_WithUnknownCommand_ReturnsUsageError()
    {
        var (code, output, error) = Run("fly");

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Contains("invalid choice", error);
    }

    [Fact]
    public void Run_WithoutArguments_ReturnsUsageError()
    {
        var (code, _, error) = Run();

        Assert.Equal(2, code);
        Assert.Contains("usage:", error);
    }

    [Fact]
    public void Run_WithHelp_PrintsUsageAndSucceeds()
    {
        var (code, output, _) = Run("--help");

        Assert.Equal(0, code);
        Assert.Contains("usage:", output);
        Assert.Contains("search", output);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Run_PrintsTest23DescriptionOnStdout_WhenHelpRequested(string flag)
    {
        var (code, output, error) = Run(flag);

        Assert.Equal(0, code);
        Assert.Equal("", error);
        AssertGeneralUsageHeader(output);
    }

    [Fact]
    public void Run_PrintsTest23DescriptionOnStderr_WhenNoCommand()
    {
        var (code, output, error) = Run();

        Assert.Equal(2, code);
        Assert.Equal("", output);
        AssertGeneralUsageHeader(error);
        Assert.Contains("notes: error: the following arguments are required: command", error);
    }

    [Fact]
    public void Run_PrintsTest23DescriptionOnStderr_WhenCommandUnknown()
    {
        var (code, output, error) = Run("fly");

        Assert.Equal(2, code);
        Assert.Equal("", output);
        AssertGeneralUsageHeader(error);
        Assert.Contains("notes: error: invalid choice: 'fly'", error);
    }

    [Fact]
    public void Run_ListsEveryCommandAfterDescription_WhenHelpRequested()
    {
        var (code, output, _) = Run("--help");

        Assert.Equal(0, code);
        string[] expected = ["add", "list", "show", "delete", "search", "edit", "recent"];
        Assert.Equal(expected, Lines(output)[5..].Select(line => line.TrimStart().Split(' ')[0]));
    }

    private static void AssertGeneralUsageHeader(string text)
    {
        string[] expected = ["usage: notes <command> [options]", "", "TEST23", "", "commands:"];
        Assert.Equal(expected, Lines(text)[..expected.Length]);
        Assert.DoesNotContain("Minimal notes CLI.", text);
    }

    private (int Code, string Output, string Error) Run(params string[] argv)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var code = Cli.Run(argv, stdout, stderr, _path);
        return (code, stdout.ToString(), stderr.ToString());
    }

    private void Seed(params Note[] notes) => NoteStorage.Save(notes, _path);

    private static string[] Lines(string output) => output.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
}
