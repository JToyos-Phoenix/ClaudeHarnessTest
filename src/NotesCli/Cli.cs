using System.Globalization;

namespace NotesCli;

/// <summary>Command-line interface: argument parsing, command dispatch and error reporting.</summary>
public static class Cli
{
    private const string ProgramName = "notes";
    private const int DefaultRecentLimit = 5;

    private static readonly Command[] Commands =
    [
        new("add", "Add a note.", ["title"], ["body"], CmdAdd),
        new("list", "List all notes.", [], [], CmdList),
        new("show", "Show a note by id.", ["id"], [], CmdShow),
        new("delete", "Delete a note by id.", ["id"], [], CmdDelete),
        new("search", "Search notes by word.", ["query"], [], CmdSearch),
        new("edit", "Edit a note by id.", ["id"], ["title", "body"], CmdEdit),
        new("recent", $"List the most recent notes (default {DefaultRecentLimit}).", [], ["limit"], CmdRecent),
    ];

    public static int Run(string[] argv, TextWriter stdout, TextWriter stderr, string? notesPath = null)
    {
        if (argv.Length > 0 && argv[0] is "-h" or "--help")
        {
            stdout.Write(GeneralUsage());
            return 0;
        }

        try
        {
            var command = FindCommand(argv);
            var tokens = argv[1..];
            if (tokens.Any(t => t is "-h" or "--help"))
            {
                stdout.WriteLine(command.Usage);
                return 0;
            }

            var values = ParseArguments(command, tokens);
            return command.Handler(new Invocation(command, values, stdout, notesPath));
        }
        catch (UsageException ex)
        {
            stderr.WriteLine(ex.Usage);
            stderr.WriteLine($"{ProgramName}: error: {ex.Message}");
            return 2;
        }
        catch (NoteException ex)
        {
            stderr.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int CmdAdd(Invocation inv)
    {
        var notes = NoteStorage.Load(inv.NotesPath);
        var note = Note.New(inv.Get("title"), inv.GetOptional("body") ?? "", notes);
        notes.Add(note);
        NoteStorage.Save(notes, inv.NotesPath);
        inv.Out.WriteLine($"id={note.Id}");
        return 0;
    }

    private static int CmdList(Invocation inv)
    {
        foreach (var note in NoteStorage.Load(inv.NotesPath))
        {
            inv.Out.WriteLine(FormatSummary(note));
        }

        return 0;
    }

    private static int CmdShow(Invocation inv)
    {
        var id = inv.GetInt("id");
        var note = NoteStorage.Load(inv.NotesPath).FirstOrDefault(n => n.Id == id)
            ?? throw new NoteNotFoundException($"note with id={id} does not exist");
        inv.Out.WriteLine(note.Title);
        inv.Out.WriteLine(note.CreatedAt);
        inv.Out.WriteLine(note.Body);
        return 0;
    }

    private static int CmdDelete(Invocation inv)
    {
        var id = inv.GetInt("id");
        var notes = NoteStorage.Load(inv.NotesPath);
        var remaining = notes.Where(n => n.Id != id).ToList();
        if (remaining.Count == notes.Count)
        {
            throw new NoteNotFoundException($"note with id={id} does not exist");
        }

        NoteStorage.Save(remaining, inv.NotesPath);
        inv.Out.WriteLine($"deleted id={id}");
        return 0;
    }

    private static int CmdSearch(Invocation inv)
    {
        var query = inv.Get("query");
        var matches = NoteStorage.Load(inv.NotesPath)
            .Where(n => n.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || n.Body.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 0)
        {
            throw new NoteNotFoundException($"no notes contain \"{query}\"");
        }

        foreach (var note in matches)
        {
            inv.Out.WriteLine(FormatSummary(note));
        }

        return 0;
    }

    private static int CmdEdit(Invocation inv)
    {
        var id = inv.GetInt("id");
        var title = inv.GetOptional("title");
        var body = inv.GetOptional("body");
        if (title is null && body is null)
        {
            throw new NoteException("you must pass --title and/or --body");
        }

        var notes = NoteStorage.Load(inv.NotesPath);
        var index = notes.FindIndex(n => n.Id == id);
        if (index < 0)
        {
            throw new NoteNotFoundException($"note with id={id} does not exist");
        }

        var current = notes[index];
        notes[index] = current with { Title = title ?? current.Title, Body = body ?? current.Body };
        NoteStorage.Save(notes, inv.NotesPath);
        inv.Out.WriteLine($"edited id={id}");
        return 0;
    }

    private static int CmdRecent(Invocation inv)
    {
        var limit = inv.GetPositiveInt("limit", DefaultRecentLimit);

        // CreatedAt is always UTC ISO 8601 with second precision, so ordinal order is chronological order.
        // Ids are incremental, so the higher id is the newer note when two share the same second.
        var recent = NoteStorage.Load(inv.NotesPath)
            .OrderByDescending(n => n.CreatedAt, StringComparer.Ordinal)
            .ThenByDescending(n => n.Id)
            .Take(limit);
        foreach (var note in recent)
        {
            inv.Out.WriteLine(FormatSummary(note));
        }

        return 0;
    }

    private static string FormatSummary(Note note) => $"{note.Id}\t{note.CreatedAt}\t{note.Title}";

    private static Command FindCommand(string[] argv)
    {
        if (argv.Length == 0)
        {
            throw new UsageException(GeneralUsage(), "the following arguments are required: command");
        }

        return Commands.FirstOrDefault(c => c.Name == argv[0])
            ?? throw new UsageException(
                GeneralUsage(),
                $"invalid choice: '{argv[0]}' (choose from {string.Join(", ", Commands.Select(c => c.Name))})");
    }

    private static Dictionary<string, string> ParseArguments(Command command, string[] tokens)
    {
        var values = new Dictionary<string, string>();
        var positionals = new List<string>();
        var optionsEnded = false;

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (optionsEnded || !token.StartsWith("--", StringComparison.Ordinal))
            {
                positionals.Add(token);
                continue;
            }

            if (token == "--")
            {
                optionsEnded = true;
                continue;
            }

            var separator = token.IndexOf('=');
            var name = separator < 0 ? token[2..] : token[2..separator];
            if (!command.Options.Contains(name))
            {
                throw new UsageException(command.Usage, $"unrecognized arguments: {token}");
            }

            if (separator >= 0)
            {
                values[name] = token[(separator + 1)..];
            }
            else if (i + 1 < tokens.Length)
            {
                values[name] = tokens[++i];
            }
            else
            {
                throw new UsageException(command.Usage, $"argument --{name}: expected one argument");
            }
        }

        if (positionals.Count < command.Positionals.Length)
        {
            var missing = command.Positionals[positionals.Count..];
            throw new UsageException(
                command.Usage,
                $"the following arguments are required: {string.Join(", ", missing)}");
        }

        if (positionals.Count > command.Positionals.Length)
        {
            var extra = positionals.Skip(command.Positionals.Length);
            throw new UsageException(command.Usage, $"unrecognized arguments: {string.Join(" ", extra)}");
        }

        for (var i = 0; i < command.Positionals.Length; i++)
        {
            values[command.Positionals[i]] = positionals[i];
        }

        return values;
    }

    private static string GeneralUsage()
    {
        string[] lines =
        [
            $"usage: {ProgramName} <command> [options]",
            "",
            "TEST23",
            "",
            "commands:",
            .. Commands.Select(c => $"  {c.Signature,-40} {c.Help}"),
        ];
        return $"{string.Join(Environment.NewLine, lines)}{Environment.NewLine}";
    }

    private sealed record Command(
        string Name,
        string Help,
        string[] Positionals,
        string[] Options,
        Func<Invocation, int> Handler)
    {
        public string Signature =>
            string.Join(" ", new[] { Name }
                .Concat(Positionals.Select(p => $"<{p}>"))
                .Concat(Options.Select(o => $"[--{o} {o.ToUpperInvariant()}]")));

        public string Usage => $"usage: {ProgramName} {Signature}";
    }

    private sealed class Invocation(
        Command command,
        IReadOnlyDictionary<string, string> values,
        TextWriter stdout,
        string? notesPath)
    {
        public TextWriter Out => stdout;

        public string? NotesPath => notesPath;

        public string Get(string name) => values[name];

        public string? GetOptional(string name) => values.GetValueOrDefault(name);

        public int GetInt(string name) =>
            int.TryParse(values[name], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new UsageException(
                    command.Usage,
                    $"argument {Label(name)}: invalid int value: '{values[name]}'");

        public int GetPositiveInt(string name, int defaultValue)
        {
            if (!values.ContainsKey(name))
            {
                return defaultValue;
            }

            var value = GetInt(name);
            return value > 0
                ? value
                : throw new UsageException(
                    command.Usage,
                    $"argument {Label(name)}: must be a positive integer, got '{values[name]}'");
        }

        private string Label(string name) => command.Options.Contains(name) ? $"--{name}" : name;
    }

    private sealed class UsageException(string usage, string message) : Exception(message)
    {
        public string Usage { get; } = usage;
    }
}
