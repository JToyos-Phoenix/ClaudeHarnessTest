# Architecture: what "good work" means

> This document defines the quality bar. Reviewer agents evaluate code against
> this file. If it is not written here, it is not a requirement.

## Principles

1. **Clear layers.** The project has three layers and only three, one file each:
   - `src/NotesCli/NoteStorage.cs`: persistence (JSON on disk).
   - `src/NotesCli/Note.cs`: domain model (`Note` record + domain exceptions).
   - `src/NotesCli/Cli.cs`: user interface (argument parsing, commands, error reporting).

   Plus `src/NotesCli/Program.cs`, the entry point, which only wires `Console` to
   `Cli.Run`. Do not add layers (services, repositories, ORMs, DI containers) until
   there is a concrete reason documented in `feature_list.json`. `init.ps1` rejects
   any other `.cs` file in `src/NotesCli/`.

2. **No external dependencies.** `src/NotesCli` uses the .NET Base Class Library only:
   no `<PackageReference>` (no System.CommandLine, no Newtonsoft.Json). If a feature
   needs a dependency, discuss it first (status `blocked`). The test project may use
   xUnit and the test SDK.

3. **Explicit errors.** Code that can fail (id does not exist, invalid input) throws a
   named exception from the `NoteException` hierarchy. It never returns `null` to signal
   failure.

4. **Immutable by default.** `Note` is a `sealed record` with init-only properties.
   Modifying means creating a new instance with `with { ... }`.

5. **Atomic disk writes.** Every write to the notes file goes first to a temporary file
   in the same folder, then `File.Move(temp, path, overwrite: true)`. Never leave the
   file half-written.

6. **IO is injected.** `Cli.Run(argv, stdout, stderr, notesPath)` receives its writers
   and the notes path. Only `Program.cs` touches `Console`. This is what makes the CLI
   testable in-process without mocks.

## Data flow

```
user  ->  Program.cs  ->  Cli.Run(argv, Console.Out, Console.Error)
                            |
                            |- parses argv into a command + values
                            |- builds a Note with Note.New(...) / note with { ... }
                            |
                            '->  NoteStorage.Load() / NoteStorage.Save()
                                    |
                                    '->  .notes.json (in CWD, or $NOTES_FILE)
```

Dependency direction: `Cli -> NoteStorage -> Note`, and `Cli -> Note`. `Note` depends
on nothing.

## The JSON file format (shared with the Python version)

```json
[
  { "id": 1, "title": "...", "body": "...", "created_at": "2026-04-27T14:00:56+00:00" }
]
```

snake_case keys, 2-space indentation, UTF-8 without escaping non-ASCII characters.
A file written by the Python original can be read by this version and vice versa.

## What NOT to do

- Do not write errors with `stdout`. Use the injected `stderr` and exit code != 0
  (1 = domain error, 2 = usage error).
- Do not mix IO with domain logic inside `Note.cs`.
- Do not read/write the file once per item inside a loop. Load at the start, modify in
  memory, save at the end.
- Do not add a configuration system. The file path is passed explicitly or uses the
  default (`$NOTES_FILE` or `.notes.json`).
