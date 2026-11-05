# Review: feature 7 `cli_recent`

**Verdict:** APPROVED

- **Reviewer:** reviewer agent (Claude Opus 5.5), launched by the leader
- **Date:** 2026-11-04
- **Inputs:** `progress/impl_cli_recent.md`, `feature_list.json` (feature 7), `docs/architecture.md`,
  `docs/conventions.md`, `docs/verification.md`, `CHECKPOINTS.md`

## Acceptance criteria

- [x] `notes recent` lists the 5 most recent notes by default
      -> `CliTests.Recent_ListsFiveNewestNotesFirst_ByDefault` (7 notes seeded, exactly 5 lines asserted)
- [x] `notes recent --limit 10` changes the number
      -> `CliTests.Recent_WithLimit_ListsThatManyNewestNotes` (12 seeded -> ids 12..3, 10 lines);
         `CliTests.Recent_WithLimitAboveCount_ListsAllNotes` (2 seeded, `--limit 10` -> 2 lines)
- [x] Order is by `created_at` from newest to oldest
      -> `CliTests.Recent_ListsFiveNewestNotesFirst_ByDefault` (file order and id order are scrambled
         against date order, so "file order", "reverse file order" and "sort by id" would all fail;
         only a real `created_at` sort passes); tie-break -> `CliTests.Recent_ListsHigherIdFirst_WhenCreatedAtIsEqual`
- [x] Each line follows `<id>\t<created_at>\t<title>` (same as `list`)
      -> `CliTests.Recent_ListsFiveNewestNotesFirst_ByDefault` (exact lines asserted);
         `CliTests.Recent_UsesSameLineFormatAsList` (recent output == reversed `list` output).
         Format is reused, not duplicated: `CmdRecent` (`src/NotesCli/Cli.cs:158`) calls the existing
         `FormatSummary` (`Cli.cs:164`), the same one used by `CmdList` (`Cli.cs:70`) and `CmdSearch` (`Cli.cs:116`).
- [x] No notes -> exit 0 and nothing printed
      -> `CliTests.Recent_WhenFileMissing_OutputsNothing`; `CliTests.Recent_WhenFileHasNoNotes_OutputsNothing`
         (file exists with `[]`); both also assert stderr is empty
- [x] `--limit <= 0` -> exit != 0 and a clear message on stderr
      -> `CliTests.Recent_WithNonPositiveLimit_ReturnsUsageError(limit: "0")`,
         `CliTests.Recent_WithNonPositiveLimit_ReturnsUsageError(limit: "-1")`
         (exit 2, empty stdout, stderr = usage line + `argument --limit: must be a positive integer, got '<v>'`)
- [x] `CliTests.cs` covers default order, custom limit, empty file, invalid limit
      -> all of the above, `tests/NotesCli.Tests/CliTests.cs:224-350`; extra error path
         `CliTests.Recent_WithNonNumericLimit_ReturnsUsageError`

**Determinism of ordering tests:** every ordering test seeds notes through `Seed` (`CliTests.cs:389`) ->
`NoteStorage.Save` with explicit `CreatedAt` values; none uses `add` with the wall clock, so same-second
`created_at` collisions cannot make them flaky. The same-second case is pinned explicitly by
`Recent_ListsHigherIdFirst_WhenCreatedAtIsEqual` (ids stored 1, 3, 2 with identical timestamps; expects 3, 2, 1)
against the `ThenByDescending(n => n.Id)` tie-breaker at `Cli.cs:154`.

## Files reviewed

| File | Architecture | Conventions | Test |
|------|--------------|-------------|------|
| `src/NotesCli/Cli.cs` (lines 9, 19, 146-162, 277-299) | OK: stays in the UI layer; one `NoteStorage.Load`, no write, no `Console`; dependency `Cli -> NoteStorage -> Note` unchanged | OK: `DefaultRecentLimit` PascalCase const, `CmdRecent` handler name, Allman + braces, `$"..."` only, no line > 120 in the diff (max 114, line 278); comment at 150-151 states a non-obvious invariant (allowed) | `CliTests.Recent_*` (10 cases) |
| `tests/NotesCli.Tests/CliTests.cs` (lines 224-350, 389) | OK: in-process `Cli.Run` with `StringWriter`s via `Run` helper; real temp file via `TempDirectory` (`_tmp`) | OK: `sealed` class, `Method_ExpectedResult_WhenCondition` names (same shape as existing tests), Arrange/Act/Assert separated by blank lines, collection expressions | n/a |
| `feature_list.json` | Feature 7 `pending -> in_progress` only (allowed for the implementer) | - | - |
| `progress/current.md` | Describes the active session | - | - |

Error handling: `--limit <= 0` and non-numeric `--limit` throw the private `Cli.UsageException` (exit 2, usage
line + `notes: error: ...` on stderr). This matches `docs/conventions.md` "Error handling" (usage errors -> 2,
like a non-numeric id) and the existing `Invocation.GetInt` precedent. The `GetInt` message change
(`Label(name)`, `Cli.cs:282`) does not alter existing output for positionals (`Show_WithNonNumericId_ReturnsUsageError`
still green). No new public members, so `docs/verification.md` Level 1 has nothing new to cover.

## Evidence I produced myself

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\init.ps1` -> `Passed! - Failed: 0, Passed: 42,
  Skipped: 0, Total: 42`, `[OK] Environment ready`, **exit code 0**. Only `[WARN]` is feature 7 `in_progress`
  (expected; clears with this approval).
- `dotnet test --list-tests` shows the 10 `Recent_*` cases are discovered.
- Level 3 smoke test with the built `NotesCli.dll` and `NOTES_FILE` in a scratch folder (deleted afterwards):
  missing file -> no output, exit 0; 7 real `add`s (several in the same second) -> `recent` prints ids 7,6,5,4,3;
  `--limit 2` and `--limit=3` OK; `--limit 0` / `--limit -5` / `--limit abc` / `--limit` (no value) /
  extra positional -> exit 2 with usage + clear message; a hand-written file with dates out of file order
  (ids 1,2,3 = Jan, Dec, Jun) -> `recent` prints 2, 3, 1 while `list` prints 1, 2, 3 in the same line format.

## Checkpoints

- C1: [x]
  - [x] 4 base files exist (init step 2)
  - [x] 3 docs exist (init step 2)
  - [x] `./init.sh` (init.ps1) ends with exit code 0 (my run above)
- C2: [x]
  - [x] At most one feature `in_progress` (only 7; init step 3)
  - [x] Every `done` feature has tests that pass (42/42 green; feature 7 adds 10)
  - [x] `progress/current.md` describes the active session (feature 7)
- C3: [x]
  - [x] `src/NotesCli/` only has `Cli.cs`, `Note.cs`, `NoteStorage.cs`, `Program.cs` (+ csproj) (init step 4)
  - [x] `NotesCli.csproj` has no `<PackageReference>` (init step 4)
  - [x] No `Console.*` outside `Program.cs`; no debug output; no TODO/FIXME/HACK in `src/` or `tests/`
- C4: [x]
  - [x] Test class per source file: `CliTests`, `NoteTests`, `NoteStorageTests`, `ProgramEndToEndTests`
  - [x] Tests use `TempDirectory`; no filesystem mocks (new tests write a real temp file via `NoteStorage.Save`)
  - [x] `dotnet test NotesCli.slnx` runs 42 tests, all green
- C5: [x]
  - [x] No suspicious untracked files (no `*.tmp`, `.notes.json`, `.notes_*.json` in the repo; `bin/`, `obj/`,
        `.vs/` covered by `.gitignore`)
  - [x] `progress/history.md` has an entry for the last closed session (2026-11-03). The current session's
        entry is appended by the leader after this approval (`.claude/agents/leader.md:30-31`)
  - [x] Feature 7 moves to `done` with this approval

## Required changes

None for feature 7.

## Observations for the leader (not caused by this feature, not blocking it)

1. **`feature_list.json:104-117`, feature id 8 is bogus and must be resolved before the next session.**
   It reuses `"name": "cli_recent"`, has `"title": "description change"`,
   `"description": "Changed the project description to TEST23."`, a first acceptance item
   `"Changed description when run the project to simply say TEST23"` (line 110) followed by four items copied
   verbatim from feature 7 (`--limit`, empty file, ...) that have nothing to do with that title, status
   `pending`. It is not the implementer's work and it kept changing on disk during this review (line 110 was
   `""` on my first read, then the text above; the implementer's report describes it as "an exact copy of
   feature 7"). The top-level `description` was not actually changed. Risks: per `AGENTS.md` section 4 the next session would pick it as the lowest pending id;
   its name collides with `progress/impl_cli_recent.md` / `progress/review_cli_recent.md`. `init.ps1:92` only
   checks duplicate ids, not duplicate names, so init does not catch it. I did not touch it (the reviewer
   may only change feature 7's `status`). The leader/human should remove it or give it a real definition.
2. **Pre-existing convention debt in `src/NotesCli/Cli.cs`, outside this feature's diff:** line 224 is 123 cols
   and line 243 is 128 cols (limit 120); line 245 uses `+ Environment.NewLine` (conventions: no `+`
   concatenation). They belong to `ParseArguments` / `GeneralUsage`, which existed before feature 7
   (`progress/explore_cli_surface.md`, 2026-11-03) and carry no `recent`-specific content. Worth a small
   cleanup feature.
3. Session close: append the session summary to `progress/history.md` and reset `progress/current.md`
   (`.claude/agents/leader.md:30-31`).
