# Implementation report: feature 7 - cli_recent

- **Agent:** implementer (Claude Opus 5.5), launched by the leader
- **Date:** 2026-11-04
- **Status left in `feature_list.json`:** `in_progress` (waiting for review; not marked `done`)

## Files touched

| File | Change |
|------|--------|
| `src/NotesCli/Cli.cs` | `DefaultRecentLimit` const; `recent` entry in `Commands` (option `limit`); new `CmdRecent` handler; new `Invocation.GetPositiveInt(name, defaultValue)` and private `Invocation.Label(name)`; `GetInt` error message now uses `Label(name)` (wrapped to stay within 120 columns). |
| `tests/NotesCli.Tests/CliTests.cs` | 9 new `Recent_*` test methods (10 cases) + private `Seed(params Note[])` helper. |
| `feature_list.json` | Feature 7 status `pending` -> `in_progress`. |
| `progress/current.md` | Session plan and log. |

No new files in `src/`, no new dependencies, `Note.cs` / `NoteStorage.cs` / `Program.cs` unchanged.

## Acceptance criteria -> tests

| Acceptance criterion | Test(s) in `CliTests.cs` |
|----------------------|--------------------------|
| `notes recent` lists the 5 most recent by default | `Recent_ListsFiveNewestNotesFirst_ByDefault` (7 notes seeded -> exactly 5 lines) |
| `notes recent --limit 10` changes the number | `Recent_WithLimit_ListsThatManyNewestNotes` (12 notes, `--limit 10` -> ids 12..3); `Recent_WithLimitAboveCount_ListsAllNotes` (2 notes, `--limit 10` -> 2 lines) |
| Order by `created_at` newest -> oldest | `Recent_ListsFiveNewestNotesFirst_ByDefault` (file order and id order deliberately scrambled vs. date order, so only a real sort by `created_at` passes); `Recent_ListsHigherIdFirst_WhenCreatedAtIsEqual` (tie-breaker) |
| Line format `<id>\t<created_at>\t<title>` (same as `list`) | `Recent_ListsFiveNewestNotesFirst_ByDefault` (exact lines asserted); `Recent_UsesSameLineFormatAsList` (recent output == reversed `list` output) |
| No notes -> exit 0, nothing printed | `Recent_WhenFileMissing_OutputsNothing`; `Recent_WhenFileHasNoNotes_OutputsNothing` (file exists with `[]`). Both also assert stderr is empty |
| `--limit <= 0` -> exit != 0 + clear stderr message | `Recent_WithNonPositiveLimit_ReturnsUsageError` (Theory: `0`, `-1`) -> exit 2, empty stdout, stderr has `usage: notes recent ...` and `argument --limit: must be a positive integer, got '<value>'` |
| (extra error path) non-numeric limit | `Recent_WithNonNumericLimit_ReturnsUsageError` -> exit 2, `argument --limit: invalid int value: 'abc'` |

TDD evidence: with the tests written and before any change to `Cli.cs`,
`dotnet test --filter CliTests.Recent_` gave `Failed: 10, Passed: 0` (the command did not
exist, so every case got exit 2 "invalid choice"). The other 32 tests stayed green.

## Design decisions

1. **Reuse of the `list` formatter.** `CmdRecent` writes each note with the existing
   `FormatSummary(note)` used by `list` and `search`. The format is not duplicated.
2. **Deterministic tests.** Ordering tests seed notes straight through
   `NoteStorage.Save` with explicit, distinct `CreatedAt` values (helper `Seed`), never
   through `add` with the wall clock. This avoids same-second ties and flakiness.
3. **Ordering = ordinal string compare on `CreatedAt`, descending.** Feature 2 guarantees
   `CreatedAt` is ISO 8601 in UTC with second precision (`yyyy-MM-ddTHH:mm:ss+00:00`; the
   Python version writes the same format), so ordinal string order is the same as
   chronological order. This is culture-free and never throws, so no `FormatException` on
   odd data. A one-line comment in `CmdRecent` records this invariant.
4. **Tie-breaker: higher `Id` first** (`ThenByDescending(n => n.Id)`). Ids are incremental
   (`Note.New` uses max + 1), so when two notes share the same second the higher id was
   created later. Without it, the stable `OrderByDescending` would fall back to file order.
   `Recent_ListsHigherIdFirst_WhenCreatedAtIsEqual` stores ids in the order 1, 3, 2 to
   prove it. The smoke test below shows it on real same-second notes (3, 2, 1).
5. **Invalid limit is a usage error (exit 2), not a domain error (exit 1).** An
   out-of-range option value is bad CLI input, like a non-numeric id (`docs/conventions.md`:
   usage errors -> usage line on stderr + exit 2, as argparse does). The acceptance only
   requires `!= 0`. The check lives in `Invocation.GetPositiveInt(name, defaultValue)`,
   next to `GetInt`, because that is where `command.Usage` is available. It returns the
   default when the option is absent.
6. **Error message wording.** `argument --limit: must be a positive integer, got '<raw>'`.
   It echoes the raw token in quotes, the same way `GetInt` reports
   `invalid int value: '<raw>'`, so no culture-dependent number formatting reaches stderr
   (for example, the U+2212 minus sign in some ICU cultures).
7. **`Invocation.Label(name)`** shows option names as `--limit` (argparse style) and keeps
   positionals as bare names. This is the only change to existing shared code: `GetInt`'s
   message now uses it. Existing behaviour does not change, because `recent --limit` is the
   first int-typed option (positionals still print `argument id: invalid int value: ...`).
8. **Help text** for `recent` is `List the most recent notes (default 5).`, built from the
   `DefaultRecentLimit` const so the number cannot drift.

## Out-of-scope observations (not changed)

- **`feature_list.json` has a duplicate feature.** During this session someone else
  edited the file on disk and added feature id 8, which is an exact copy of feature 7
  (same `name: cli_recent`, same acceptance, status `pending`). I did not create or change
  it. `init.ps1` only checks for duplicate ids, so it still passes ("8 features, 1
  pending"). The leader should decide whether to remove it; this implementation already
  covers its acceptance.
- Lines 224 and 243 of `src/NotesCli/Cli.cs` (`ParseArguments` / `GeneralUsage`) were
  already over the 120-column limit before this feature. I left them alone because they
  are outside this feature's scope.

## Manual smoke test (Level 3, built `NotesCli.dll`, `NOTES_FILE` in a temp folder)

```
--- recent on missing file:
exit=0
--- recent:            (3 notes added in the same second)
3	2026-11-04T15:45:39+00:00	c
2	2026-11-04T15:45:39+00:00	b
1	2026-11-04T15:45:39+00:00	a
exit=0
--- recent --limit 2:
3	2026-11-04T15:45:39+00:00	c
2	2026-11-04T15:45:39+00:00	b
exit=0
--- recent --limit 0:
usage: notes recent [--limit LIMIT]
notes: error: argument --limit: must be a positive integer, got '0'
exit=2
--- help (last line):
  recent [--limit LIMIT]                   List the most recent notes (default 5).
```

The temp notes file and `NOTES_FILE` were removed afterwards.

## Output of the last `./init.sh`

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
[OK]    feature_list.json is valid (8 features, 1 pending)
[WARN]  Feature 7 'cli_recent' is in_progress: resume it (see progress/current.md)

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

Passed!  - Failed:     0, Passed:    42, Skipped:     0, Total:    42, Duration: 715 ms - NotesCli.Tests.dll (net10.0)
[OK]    Build is clean (warnings are errors) and all tests pass

-- 6. Summary ------------------------------------------
[OK]    Environment ready. You can start working.
EXIT CODE: 0
```

The `[WARN]` is expected: feature 7 stays `in_progress` until an `APPROVED` review.
