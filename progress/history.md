# History log (append-only)

> Every time a session closes, its summary is appended here.
> Do not edit previous entries. Only append at the end.

---

## 2026-04-20 to 2026-04-29: Python original (inherited)
- **Source:** betta-tech/ejemplo-harness-subagentes (Python 3.9+, stdlib only).
- **Summary:** harness bootstrap, then features 1-6 (storage_layer, note_model,
  cli_add_list, cli_show_delete, cli_search, cli_edit) built one at a time; feature 6 was
  the first one run through leader -> implementer -> reviewer. 22 tests green at the end.

## 2026-11-03: Port to C# (.NET 10)
- **Agent:** Claude Opus 5.5, single session at a human's request (no subagents: the
  port changed the harness itself, which is human territory per `CLAUDE.md`).
- **Plan:** port the app layer by layer keeping behaviour and the JSON file format
  identical; port every harness file; replace `init.sh` with `init.ps1` (+ `init.sh`
  wrapper); fix protocol inconsistencies found in the original.
- **Changes:** `src/NotesCli/` (NoteStorage, Note, Cli, Program), `tests/NotesCli.Tests/`
  (xUnit), `init.ps1`, `init.sh`, `AGENTS.md`, `CLAUDE.md`, `CHECKPOINTS.md`, `docs/`,
  `.claude/agents/`, `scripts/` (hooks + demo-orchestration.cs), `progress/`.
- **Verification:** `./init.sh` green, 32 tests pass (22 ported + 10 C#-specific).
- **Closure:** features 1-6 ported as `done`. Feature 7 (`cli_recent`) left `pending`
  on purpose so the harness can be exercised end to end.

## 2026-11-04: Feature 7 `cli_recent`
- **Agents:** leader (Claude Opus 5.5) -> implementer -> reviewer.
- **Plan:** tests first in `CliTests.cs` with notes seeded at explicit `CreatedAt` values
  (deterministic ordering); add `recent` to `Cli.Commands`, reuse `FormatSummary` for the
  `list` line format; sort by `CreatedAt` desc, tie-break by `Id` desc; `--limit <= 0` is a
  usage error (exit 2).
- **Changes:** `src/NotesCli/Cli.cs` (`DefaultRecentLimit`, `CmdRecent`,
  `Invocation.GetPositiveInt` / `Label`), `tests/NotesCli.Tests/CliTests.cs` (10 `Recent_*`
  cases + `Seed` helper).
- **Verification:** `./init.sh` green, 42 tests pass. Report: `progress/impl_cli_recent.md`.
- **Closure:** `APPROVED` in `progress/review_cli_recent.md`; feature 7 `done`.
- **Leftovers:** feature 8 was added to `feature_list.json` mid-session as a malformed copy of
  feature 7 (duplicate name `cli_recent`, copied acceptance); rewritten by the leader with the
  human's approval before starting it. Pre-existing convention debt in `Cli.cs`
  `ParseArguments` / `GeneralUsage` (lines over 120 cols, `+` concatenation) noted by the
  reviewer, out of scope.

## 2026-11-04: Feature 8 `cli_description`
- **Agents:** leader (Claude Opus 5.5) -> implementer -> reviewer.
- **Spec:** rewritten by the leader with the human's approval (was a malformed copy of
  feature 7). Human chose to change only the description line of the general help.
- **Plan:** tests first asserting `TEST23` present and `Minimal notes CLI.` absent for
  `--help` / `-h`, no command and unknown command; then change the single literal.
- **Changes:** `src/NotesCli/Cli.cs:243` (`"Minimal notes CLI."` -> `"TEST23"`, line now 116
  cols); `tests/NotesCli.Tests/CliTests.cs` (4 test methods / 5 cases + helper
  `AssertGeneralUsageHeader`).
- **Verification:** `./init.sh` green, 47 tests pass. Reviewer confirmed via mutation runs and
  an old/new byte diff that only the description line changed; exit codes identical.
  Report: `progress/impl_cli_description.md`.
- **Closure:** `APPROVED` in `progress/review_cli_description.md`; feature 8 `done`.
- **Leftovers (out of scope, candidate cleanup feature):** `Cli.cs:224` over 120 cols,
  `Cli.cs:245` `+ Environment.NewLine`, `Cli.cs:243` collection initializer instead of a
  collection expression; command-list test pins names/order only, not help columns.

## 2026-11-05: Feature 9 `cli_conventions_cleanup`
- **Agents:** leader (Claude Opus 5.5) -> implementer -> reviewer. Project now under git
  (baseline commit `8e30cd7`, feature 9 spec added in `34dc64d`).
- **Plan:** harden the command-list test first (green on unchanged code, then mutation-checked);
  refactor only what the acceptance names; prove help output byte-identical to `8e30cd7`.
- **Changes:** `src/NotesCli/Cli.cs` (line 224 wrapped to <= 120 cols; `GeneralUsage()` uses a
  collection expression with spread and interpolation instead of `new List` + `AddRange` + `+`);
  `tests/NotesCli.Tests/CliTests.cs` (`Run_ListsEveryCommandAfterDescription_WhenHelpRequested`
  now pins the 7 full padded rows). Report: `progress/impl_cli_conventions_cleanup.md`.
- **Verification:** `./init.sh` green, 47 tests pass. Reviewer rebuilt `8e30cd7` out of repo:
  15 CLI cases byte-identical (stdout, stderr, exit code); hardened test fails under 6 mutations.
- **Closure:** `APPROVED` in `progress/review_cli_conventions_cleanup.md`; feature 9 `done`.
- **Leftovers (out of scope, candidate follow-ups):** `Cli.cs:182` `new List<string>()` and
  `Cli.cs:265` `new[] { Name }` (collection expressions); `ProgramEndToEndTests.cs:5` and
  `TempDirectory.cs:3` over 120 cols; no in-process test for the "arguments are required"
  message, the help trailing newline, or the blank line before `notes: error:`.
