# Implementation report: feature 8 - cli_description

- **Agent:** implementer
- **Date:** 2026-11-04
- **Status left in `feature_list.json`:** `in_progress` (waiting for review; not marked `done`)

## Files touched

| File | Change |
|------|--------|
| `src/NotesCli/Cli.cs` | Line 243 (`GeneralUsage()`): the description literal `"Minimal notes CLI."` becomes `"TEST23"`. One line, nothing else in `src/` changed. |
| `tests/NotesCli.Tests/CliTests.cs` | 4 new test methods (5 test cases) and 1 private helper, inserted after `Run_WithHelp_PrintsUsageAndSucceeds`. No existing test changed. |
| `feature_list.json` | Feature 8 status `pending` -> `in_progress`. |
| `progress/current.md` | Session plan and log. |

## Existing tests that hard-coded the old text

None. A repo-wide search (excluding `guide/`) for `Minimal notes CLI` found it only in
`src/NotesCli/Cli.cs:243` and in `feature_list.json` (the top-level project description, which is
documentation, not the help text, so it was left alone). `ProgramEndToEndTests.cs` has no help/usage
assertions, and the existing `CliTests` help/usage tests (`Run_WithHelp_PrintsUsageAndSucceeds`,
`Run_WithoutArguments_ReturnsUsageError`, `Run_WithUnknownCommand_ReturnsUsageError`) only assert on
`usage:`, `search` and `invalid choice`, so they did not need updating.

## Acceptance criteria -> tests

| # | Criterion | Test(s) in `CliTests.cs` |
|---|-----------|--------------------------|
| 1 | `notes --help` prints `TEST23` as the description line instead of `Minimal notes CLI.` (stdout, exit 0) | `Run_PrintsTest23DescriptionOnStdout_WhenHelpRequested` (Theory: `--help`, `-h`): exit 0, stderr empty, line 3 of stdout is exactly `TEST23`, `Minimal notes CLI.` absent |
| 2 | Usage on stderr for no command (and unknown command) shows `TEST23` (exit 2) | `Run_PrintsTest23DescriptionOnStderr_WhenNoCommand` and `Run_PrintsTest23DescriptionOnStderr_WhenCommandUnknown`: exit 2, stdout empty, same header check on stderr, old text absent, the existing `notes: error: ...` line still present |
| 3 | Usage line, command list and exit codes unchanged | The header check pins `usage: notes <command> [options]`, the blank lines and `commands:` exactly. `Run_ListsEveryCommandAfterDescription_WhenHelpRequested` pins the command list order (add, list, show, delete, search, edit, recent) after the header. Exit codes 0 / 2 are asserted in every new test. |
| 4 | `CliTests.cs` asserts `TEST23` present and `Minimal notes CLI.` absent for `--help` and no command | Helper `AssertGeneralUsageHeader`, used by the tests for `--help`/`-h`, no command and unknown command |

## Design decisions

- **Changed only the string literal.** The leader and the human asked for only the description line to
  change, so I did not extract a `Description` constant or reformat `GeneralUsage()`. The touched line
  goes from 128 to 116 columns, which fixes its own over-120 debt as a side effect.
- **Out-of-scope debt left untouched, as instructed.** `Cli.cs:224` is still 123 columns and `Cli.cs:245`
  still uses `+ Environment.NewLine`. Both predate this feature.
- **The test checks the exact line position, not just `Contains("TEST23")`.** The criterion says `TEST23`
  must be *the description line*, so the helper compares the first 5 lines exactly
  (`usage: ...`, `""`, `TEST23`, `""`, `commands:`). That one assertion covers "TEST23 is the description"
  and "the usage line is unchanged". The separate `DoesNotContain("Minimal notes CLI.")` covers the
  "absent" requirement explicitly.
- **The command-list check uses names only, not padding or help text.** `Run_ListsEveryCommandAfterDescription_WhenHelpRequested`
  takes the first word of each line after the header. That catches a dropped or reordered command
  without copying the `{Signature,-40}` padding into the test.
- **`-h` and unknown command are covered too.** The acceptance list requires tests for `--help` and no
  command only. `-h` and the unknown-command path share `GeneralUsage()` and appear in criterion 2 and
  the leader's notes, so covering them was cheap (one extra `InlineData` and one `Fact`).
- **Test naming** follows `docs/conventions.md` (`Method_ExpectedResult_WhenCondition`).

## Red phase (tests written first)

The post-edit hook ran the suite right after the tests were added and before the source change:

```
Failed NotesCli.Tests.CliTests.Run_PrintsTest23DescriptionOnStdout_WhenHelpRequested(flag: "-h")
Failed NotesCli.Tests.CliTests.Run_PrintsTest23DescriptionOnStdout_WhenHelpRequested(flag: "--help")
Failed NotesCli.Tests.CliTests.Run_PrintsTest23DescriptionOnStderr_WhenNoCommand
  Expected: ["usage: notes <command> [options]", "", "TEST23", "", "commands:"]
  Actual:   ["usage: notes <command> [options]", "", "Minimal notes CLI.", "", "commands:"]
Failed!  - Failed:     4, Passed:    43, Skipped:     0, Total:    47
```

The 4th failure is `Run_PrintsTest23DescriptionOnStderr_WhenCommandUnknown`, which failed with the same
message. `Run_ListsEveryCommandAfterDescription_WhenHelpRequested` passed during the red phase, which is
expected: it guards the part that must *not* change.

## Level 3 smoke test (built `NotesCli.dll`, stderr merged for the error cases)

```
== notes --help ==
usage: notes <command> [options]

TEST23

commands:
  add <title> [--body BODY]                Add a note.
  list                                     List all notes.
  show <id>                                Show a note by id.
  delete <id>                              Delete a note by id.
  search <query>                           Search notes by word.
  edit <id> [--title TITLE] [--body BODY]  Edit a note by id.
  recent [--limit LIMIT]                   List the most recent notes (default 5).
exit=0
== notes (no command) ==
... same usage block with TEST23 ...
notes: error: the following arguments are required: command
exit=2
== notes fly ==
... same usage block with TEST23 ...
notes: error: invalid choice: 'fly' (choose from add, list, show, delete, search, edit, recent)
exit=2
```

No temp notes file and no `.notes.json` were left behind.

## Output of the last `init.ps1`

`powershell -NoProfile -ExecutionPolicy Bypass -File .\init.ps1`

```
-- 1. Checking environment -----------------------------
[OK]    dotnet SDK -> 10.0.403
[OK]    Compatible .NET SDK version (net10.0)

-- 2. Checking harness base files ----------------------
[OK]    Found AGENTS.md
[OK]    Found feature_list.json
[OK]    Found progress/current.md
[OK]    Found docs/architecture.md
[OK]    Found docs/conventions.md
[OK]    Found docs/verification.md
[OK]    Found CHECKPOINTS.md

-- 3. Validating feature_list.json ---------------------
[OK]    feature_list.json is valid (8 features, 0 pending)
[WARN]  Feature 8 'cli_description' is in_progress: resume it (see progress/current.md)

-- 4. Checking architecture rules ----------------------
[OK]    src/NotesCli/NotesCli.csproj has no external dependencies
[OK]    src/NotesCli only contains the layers from docs/architecture.md
[OK]    No Console.* calls outside Program.cs (IO goes through injected TextWriters)

-- 5. Building and running tests -----------------------
  Determining projects to restore...
  All projects are up-to-date for restore.
  NotesCli -> C:\PhoenixDevelopment\HarnessTestProject\src\NotesCli\bin\Debug\net10.0\NotesCli.dll
  NotesCli.Tests -> C:\PhoenixDevelopment\HarnessTestProject\tests\NotesCli.Tests\bin\Debug\net10.0\NotesCli.Tests.dll
Test run for C:\PhoenixDevelopment\HarnessTestProject\tests\NotesCli.Tests\bin\Debug\net10.0\NotesCli.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    47, Skipped:     0, Total:    47, Duration: 517 ms - NotesCli.Tests.dll (net10.0)
[OK]    Build is clean (warnings are errors) and all tests pass

-- 6. Summary ------------------------------------------
[OK]    Environment ready. You can start working.
```

The `[WARN]` is expected: feature 8 stays `in_progress` until the reviewer approves it.
47 tests = 42 before this feature + 5 new test cases.
