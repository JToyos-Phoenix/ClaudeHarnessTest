# Verification: how to prove the work works

> Golden rule: **the agent does not say "it works", it proves it**.
> Every feature ends with executable evidence, not with claims.

## Verification levels

### Level 1: Unit tests (mandatory)

Every public member in `src/NotesCli/` has at least one test in `tests/NotesCli.Tests/` that:

1. Covers the happy path.
2. Covers at least one error path if the member can fail.

Command:

```bash
dotnet test NotesCli.slnx --nologo
```

### Level 2: CLI integration tests (mandatory for CLI features)

Features that add CLI commands are verified by running the real CLI against a temporary
file. Two flavours exist in this repo:

**In-process** (`CliTests`, fast, used for every scenario):

```csharp
using var stdout = new StringWriter();
using var stderr = new StringWriter();
var code = Cli.Run(["add", "hello", "--body", "world"], stdout, stderr, tmp.Combine("notes.json"));
Assert.Equal(0, code);
Assert.Contains("id=", stdout.ToString());
```

**Out-of-process** (`ProgramEndToEndTests`, proves the real executable and `NOTES_FILE`):

```csharp
var startInfo = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, ... };
startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "NotesCli.dll"));
startInfo.ArgumentList.Add("list");
startInfo.Environment["NOTES_FILE"] = tmp.Combine("notes.json");
```

### Level 3: Manual smoke test (optional but recommended)

Before closing the session, run an end-to-end flow against a temporary file.

PowerShell:

```powershell
$env:NOTES_FILE = Join-Path $env:TEMP 'notes_demo.json'
dotnet run --project src/NotesCli -- add "test" --body "x"
dotnet run --project src/NotesCli -- list
Remove-Item $env:NOTES_FILE; Remove-Item Env:NOTES_FILE
```

Bash:

```bash
NOTES_FILE=/tmp/notes_demo.json dotnet run --project src/NotesCli -- add "test" --body "x"
NOTES_FILE=/tmp/notes_demo.json dotnet run --project src/NotesCli -- list
rm /tmp/notes_demo.json
```

## Anti-patterns (do not do this)

- "I added the command, it should work." -> missing executable test.
- A test that only checks that nothing throws. -> it must assert the concrete result.
- Mocking the filesystem. -> use `TempDirectory` (a real temp folder).
- Writing to `Console` from `Cli.cs` to "see" something. -> use the injected writers.
- Marking the feature `done` without running `./init.sh`.

## Final check before closing

```bash
./init.sh           # must end with [OK] Environment ready
```

If `./init.sh` is red, do **not** mark anything `done`. Record the blocker in
`progress/current.md` and set the feature to `blocked` in `feature_list.json`.
