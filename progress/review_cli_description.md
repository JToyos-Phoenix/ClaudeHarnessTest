# Review: feature 8 `cli_description`

**Verdict:** APPROVED

- **Reviewer:** reviewer agent (Claude Opus 5.5), launched by the leader
- **Date:** 2026-11-04
- **Inputs:** `progress/impl_cli_description.md`, `feature_list.json` (feature 8, the current rewritten text),
  `docs/architecture.md`, `docs/conventions.md`, `docs/verification.md`, `CHECKPOINTS.md`

## Acceptance criteria

- [x] `notes --help` prints `TEST23` as the description line instead of `Minimal notes CLI.` (stdout, exit 0)
      -> `CliTests.Run_PrintsTest23DescriptionOnStdout_WhenHelpRequested(flag: "--help")` and `(flag: "-h")`
         (`tests/NotesCli.Tests/CliTests.cs:381-391`): exit 0, stderr `""`, then `AssertGeneralUsageHeader(output)`.
- [x] The general usage on stderr for no command (and for an unknown command) shows `TEST23` instead of
      `Minimal notes CLI.` (exit 2)
      -> `CliTests.Run_PrintsTest23DescriptionOnStderr_WhenNoCommand` (`CliTests.cs:393-402`) and
         `CliTests.Run_PrintsTest23DescriptionOnStderr_WhenCommandUnknown` (`CliTests.cs:404-413`): exit 2, stdout `""`,
         `AssertGeneralUsageHeader(error)`, and the `notes: error: ...` line is still there.
- [x] The rest of the general help (the usage line and the command list) and all exit codes are unchanged
      -> usage line: `AssertGeneralUsageHeader` (`CliTests.cs:427-428`) checks the first 5 lines exactly
         (`usage: notes <command> [options]`, `""`, `TEST23`, `""`, `commands:`).
         Command list: `CliTests.Run_ListsEveryCommandAfterDescription_WhenHelpRequested` (`CliTests.cs:415-423`)
         checks exactly 7 rows after the header, in order add, list, show, delete, search, edit, recent.
         Exit codes: 0 for `--help` / `-h` (stdout) and 2 for no command / unknown command (stderr) are asserted in
         every new test. The existing tests `Run_WithHelp_PrintsUsageAndSucceeds`, `Run_WithoutArguments_ReturnsUsageError`
         and `Run_WithUnknownCommand_ReturnsUsageError` were not changed and still pass.
         The test only checks command names, not the signature and help columns. I checked those directly
         (see "Evidence" below): the old and new builds give the same output byte for byte except line 3.
- [x] `CliTests.cs` asserts `TEST23` is present and `Minimal notes CLI.` is absent, for both `--help` and no command
      -> helper `AssertGeneralUsageHeader` (`CliTests.cs:425-430`): line 427/428 requires `TEST23` as the exact
         description line (present). Line 429 `Assert.DoesNotContain("Minimal notes CLI.", text)` checks that the old
         text is absent. It is called for `--help` and `-h` (`:390`), no command (`:400`) and unknown command (`:411`).

## Files reviewed

| File | Architecture | Conventions | Test |
|------|--------------|-------------|------|
| `src/NotesCli/Cli.cs` (line 243 only) | OK: change stays inside `GeneralUsage()` in the UI layer. No new type, dependency or `Console` call | OK: only the literal `"Minimal notes CLI."` changed, to `"TEST23"`. The line went from 128 to 116 columns, so it is now within the 120 limit. LF endings, no tabs | `CliTests.Run_PrintsTest23*`, `Run_ListsEveryCommandAfterDescription_WhenHelpRequested` |
| `tests/NotesCli.Tests/CliTests.cs` (lines 381-431, added only) | OK: in-process `Cli.Run` with `StringWriter`s through the existing `Run` helper and the `TempDirectory` path. No `Console`, no mocks | OK: `Method_ExpectedResult_WhenCondition` names, Arrange/Act/Assert separated by blank lines, collection expressions (`string[] expected = [...]`), `private static` helper. Longest new line is 98 columns (`:422`) | n/a |
| `feature_list.json` | Feature 8 `pending -> in_progress` only (the implementer is allowed to do this) | - | - |
| `progress/current.md` | Describes the active session (feature 8) | - | - |

No new public members, so `docs/verification.md` Level 1 has nothing new to cover. Feature 8 does not touch
error handling.

## Evidence I produced myself

- **Only line 243 changed in `src/`.** The repo is not a git repository, so I used Claude Code's file-history backups
  from the 2026-11-03 port session (`~/.claude/file-history/41fb7853-.../`) as the baseline:
  - `Note.cs`, `NoteStorage.cs`, `Program.cs` and `NotesCli.csproj` are byte-identical to their baselines. Their
    mtimes are still 2026-11-03 18:16.
  - Diffing `Cli.cs` against its baseline (`42ef6dd6243a1126@v2`) gives five hunks. Four are the already-approved
    feature-7 hunks (lines 9, 19, 146-163, 280-299; see `progress/review_cli_recent.md`). The fifth is line 243:
    `"Minimal notes CLI."` -> `"TEST23"`. There is nothing else. Line count is 306, the same as at the feature-7
    review.
  - `obj/Debug/net10.0/ref/NotesCli.dll` is still from 11:44 (the feature-7 build), so the public API surface did
    not change.
  - Diffing `CliTests.cs` against its baseline shows additions only (`224a225,352` and `259a439,440` for feature 7,
    `252a381,431` for feature 8). No existing test line was removed or changed.
- **The new tests fail without the change (mutation 1).** I made a scratch copy with line 243 reverted to
  `"Minimal notes CLI."` and ran `dotnet test`: `Failed: 4, Passed: 43, Total: 47`. The 4 failures are the two
  `--help`/`-h` cases, no command and unknown command.
- **The absent check works on its own (mutation 2).** In a scratch copy I kept `TEST23` and put
  `"Minimal notes CLI."` in the `add` help text (`Cli.cs:13`). The same 4 tests fail, each with
  `Assert.DoesNotContain() Failure: Sub-string found`.
- **Old vs new output (Level 3).** I ran the built `NotesCli.dll` (new) and the reverted scratch build (old) with
  `--help`, `-h`, no command and `fly`, using a scratch `NOTES_FILE`:
  - Exit codes are identical: 0, 0, 2, 2.
  - `--help` / `-h` write only to stdout. No command / `fly` write only to stderr.
  - In all 4 cases, `diff old new` shows exactly one change: line 3, `Minimal notes CLI.` -> `TEST23`. Each output is
    12 bytes shorter (18 - 6). So the usage line, the command list (signature and help columns included) and the
    `notes: error: ...` lines are unchanged.
  - No notes file was created. I deleted the scratch folders afterwards.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\init.ps1`: `Passed! - Failed: 0, Passed: 47,
  Skipped: 0, Total: 47`, `[OK] Environment ready`, **exit code 0**. The only `[WARN]` is that feature 8 is
  `in_progress`, which is expected and clears with this approval.

## Checkpoints

- C1: [x]
  - [x] 4 base files exist (init step 2)
  - [x] 3 docs exist (init step 2)
  - [x] `./init.sh` (init.ps1) ends with exit code 0 (my run above)
- C2: [x]
  - [x] At most one feature `in_progress` (only 8; init step 3)
  - [x] Every `done` feature has tests that pass (47/47 green; feature 8 adds 5 test cases)
  - [x] `progress/current.md` describes the active session (feature 8, 2026-11-04 11:50)
- C3: [x]
  - [x] `src/NotesCli/` only has `Cli.cs`, `Note.cs`, `NoteStorage.cs`, `Program.cs` and the csproj (init step 4)
  - [x] `NotesCli.csproj` has no `<PackageReference>` (init step 4; the file is byte-identical to the baseline)
  - [x] No `Console.*` outside `Program.cs` (grep: only `Program.cs:4-5`). No debug output, and no TODO/FIXME/HACK
        in `src/` or `tests/`
- C4: [x]
  - [x] There is a test class per source file: `CliTests`, `NoteTests`, `NoteStorageTests`, `ProgramEndToEndTests`
  - [x] Tests use `TempDirectory` and no filesystem mocks. The new tests go through `Run` with the `_tmp` path and
        never touch disk
  - [x] `dotnet test NotesCli.slnx` runs 47 tests and all are green
- C5: [x]
  - [x] No suspicious untracked files: no `*.tmp`, `.notes*.json` or `notes.json` outside `bin/` / `obj/`.
        `bin/`, `obj/` and `.vs/` are covered by `.gitignore`
  - [x] `progress/history.md` has an entry for the last closed session (2026-11-04, feature 7). The leader appends
        this session's entry after this approval (`.claude/agents/leader.md:30-31`)
  - [x] Feature 8 moves to `done` with this approval

## Required changes

None for feature 8.

## Observations for the leader (non-blocking)

1. **Known debt that is out of scope, as instructed.** `src/NotesCli/Cli.cs:224` is 123 columns and `Cli.cs:245`
   uses `+ Environment.NewLine`. Both are the same as in the 2026-11-03 baseline.
2. **More pre-existing debt on the touched line, also out of scope.** `Cli.cs:243` still builds the list with a
   collection initializer, `new List<string> { ... }`. `docs/conventions.md:18` asks for collection expressions.
   The baseline has the same construct, and the human asked to change only the description text, so this is not
   held against feature 8. It fits in the same cleanup feature as item 1.
3. **Test hardening (optional).** `Run_ListsEveryCommandAfterDescription_WhenHelpRequested` checks command names
   and order only, not the signature and help columns. If someone edited a help string in `Cli.Commands`
   (`Cli.cs:13-19`), no test would catch it. For this feature I closed that gap with the byte-for-byte old/new
   comparison above. A future cleanup could pin the full rows.
4. **Pre-existing behaviour, unchanged.** With no command or an unknown command, stderr has a blank line before
   `notes: error: ...`: `GeneralUsage()` ends with a newline and `Cli.cs:45` adds another with `WriteLine`. The
   output is identical before and after this feature.
5. **Session close.** Append the session summary to `progress/history.md` and reset `progress/current.md`
   (`.claude/agents/leader.md:30-31`).
