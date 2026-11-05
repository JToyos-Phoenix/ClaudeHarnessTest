#!/usr/bin/env dotnet
// demo-orchestration.cs: the Leader/Worker pattern with the anti "broken telephone" rule.
// Run from the repo root:  dotnet run scripts/demo-orchestration.cs
//
// The leader launches one worker per question, in parallel. Each worker analyses ONE file,
// writes its full findings to progress/explore_<topic>.md and returns only a short reference.
// The leader never holds the findings (its "context window" stays small): it holds pointers.
using System.Text.RegularExpressions;

var root = FindRepositoryRoot(Directory.GetCurrentDirectory());

WorkerTask[] tasks =
[
    new("storage_layer", "How state is persisted on disk", "src/NotesCli/NoteStorage.cs"),
    new("note_model", "Structure of the Note domain model", "src/NotesCli/Note.cs"),
    new("cli_surface", "Which commands the CLI exposes", "src/NotesCli/Cli.cs"),
];

Console.WriteLine($"[leader] launching {tasks.Length} workers in parallel");
var references = await Task.WhenAll(tasks.Select(task => Task.Run(() => RunWorker(root, task))));
foreach (var reference in references)
{
    Console.WriteLine($"[leader] received: {reference}");
}

Console.WriteLine("[leader] done. I only hold references; the findings live in progress/.");

static string RunWorker(string root, WorkerTask task)
{
    var lines = File.ReadAllLines(Path.Combine(root, task.File));
    var text = string.Join('\n', lines);

    var types = Regex.Matches(text, @"\b(?:class|record|struct|interface|enum)\s+(\w+)")
        .Select(m => m.Groups[1].Value)
        .Distinct()
        .ToList();
    var methods = Regex.Matches(
            text,
            @"^[ \t]*(public|private|internal|protected)[ \t][\w \t<>\[\],?]*?\b(\w+)[ \t]*\(",
            RegexOptions.Multiline)
        .Select(m => (Access: m.Groups[1].Value, Name: m.Groups[2].Value))
        .Where(m => !types.Contains(m.Name))
        .Distinct()
        .ToList();
    var usings = lines.Where(l => l.StartsWith("using ", StringComparison.Ordinal)).ToList();

    var report = $"""
        # Exploration: {task.Topic}

        **Question:** {task.Question}
        **File analysed:** `{task.File}`
        **Generated:** {DateTimeOffset.UtcNow:yyyy-MM-dd'T'HH:mm:sszzz} by `scripts/demo-orchestration.cs`

        ## Metrics
        - Lines of code: {lines.Length}
        - Public methods: {methods.Count(m => m.Access == "public")}
        - Types: {types.Count}
        - Explicit usings: {usings.Count}

        ## Methods
        {Bullets(methods.Select(m => $"`{m.Name}` ({m.Access})"))}

        ## Types
        {Bullets(types.Select(t => $"`{t}`"))}

        ## Dependencies
        {Bullets(usings.Select(u => $"`{u}`"))}

        """;

    var relativePath = $"progress/explore_{task.Topic}.md";
    File.WriteAllText(Path.Combine(root, relativePath), report);
    return $"done -> {relativePath}";
}

static string Bullets(IEnumerable<string> items)
{
    var bullets = items.Select(item => $"- {item}").ToList();
    return bullets.Count == 0 ? "- (none)" : string.Join('\n', bullets);
}

static string FindRepositoryRoot(string start)
{
    for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            return directory.FullName;
        }
    }

    throw new InvalidOperationException("Run this script from inside the repository (AGENTS.md not found).");
}

internal sealed record WorkerTask(string Topic, string Question, string File);
