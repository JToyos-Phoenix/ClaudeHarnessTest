# Conversion plan: from the Python harness to C# / .NET 10

> How [betta-tech/ejemplo-harness-subagentes](https://github.com/betta-tech/ejemplo-harness-subagentes)
> (Python 3.9+, stdlib only) was converted into this repository (C# 14 / .NET 10),
> what changed, why, and how the result was verified.
> For the concepts behind the harness itself, read `01-harness-engineering-guide.md` first.

---

## Contents

1. [Goal, scope and constraints](#1-goal-scope-and-constraints)
2. [Inventory of the source repository](#2-inventory-of-the-source-repository)
3. [Plan: phases](#3-plan-phases)
4. [File-by-file mapping](#4-file-by-file-mapping)
5. [Idiom mapping: Python to C#](#5-idiom-mapping-python-to-c)
6. [Design decisions](#6-design-decisions)
7. [Issues found in the original and how they were resolved](#7-issues-found-in-the-original-and-how-they-were-resolved)
8. [Test parity](#8-test-parity)
9. [Verification results](#9-verification-results)
10. [Deliberately not done](#10-deliberately-not-done)
11. [Next steps](#11-next-steps)
12. [Command cheat sheet](#12-command-cheat-sheet)

---

## 1. Goal, scope and constraints

**Goal:** a C# project with the same harness as the Python original (same files, same
roles, same protocol, same verification ideas), on top of a functionally identical
Notes CLI.

**Constraints that drove every decision:**

| # | Constraint | Why |
|---|------------|-----|
| C1 | **Behaviour parity** for features 1-6: same commands, output formats, exit codes | The harness's feature list and tests describe that behaviour |
| C2 | **Same JSON file format** (`.notes.json`) | Proves the port is faithful; both versions can share a file |
| C3 | **No external dependencies in the app** (original: "stdlib only") | It is an architecture rule the reviewer enforces; the port must obey it too |
| C4 | **Same harness semantics**: map, gate, one feature at a time, file-based memory, leader/implementer/reviewer | That is the point of the project |
| C5 | **Windows-first**, still runnable elsewhere | The target machine is Windows 11 with Windows PowerShell 5.1 and Git Bash (no `pwsh`, no Python needed) |
| C6 | **Feature 7 stays `pending`** | The repo must remain a working exercise for the harness |
| C7 | **English** | The original is in Spanish; the team works in English |

---

## 2. Inventory of the source repository

28 files, about 1,600 lines. Everything was read before porting.

| Category | Files | Notes |
|----------|-------|-------|
| App code | `src/storage.py`, `src/notes.py`, `src/cli.py`, `src/__init__.py` | 3 layers, about 190 lines |
| Tests | `tests/test_storage.py`, `test_notes.py`, `test_cli.py` | 22 tests, `unittest` |
| Harness entry | `AGENTS.md`, `CLAUDE.md`, `README.md` | |
| Harness rules | `docs/architecture.md`, `docs/conventions.md`, `docs/verification.md`, `CHECKPOINTS.md` | |
| Harness memory | `feature_list.json`, `progress/current.md`, `progress/history.md`, `progress/explore_*.md` (3), `impl_cli_edit.md`, `review_cli_edit.md` | |
| Roles | `.claude/agents/leader.md`, `implementer.md`, `reviewer.md` | |
| Automation | `init.sh`, `.claude/settings.json` (hooks + permissions) | |
| Misc | `.gitignore` | |
| Referenced but missing | `scripts/demo_orchestration.py` | Referenced from `AGENTS.md`, `CLAUDE.md` |

---

## 3. Plan: phases

| Phase | What | Exit criterion |
|-------|------|----------------|
| **0. Study** | Clone the repo, read all 28 files, list behaviours (outputs, exit codes, file format) and inconsistencies | Inventory + issue list (sections 2 and 7) |
| **1. Design** | Pick the target framework, project layout, test framework, script language; map every Python idiom to a C# one | Sections 5 and 6 |
| **2. Port the app** | Layer by layer **in feature order**: storage (#1) -> note model (#2) -> CLI (#3-#6), each with its tests, same as the feature list | `dotnet test` green |
| **3. Port the harness docs** | Translate and adapt `AGENTS.md`, `CLAUDE.md`, `docs/`, `CHECKPOINTS.md`, `feature_list.json`, `progress/` templates, agent roles | Every path/command points at C# artifacts |
| **4. Port the automation** | `init.sh` -> `init.ps1` (+ `init.sh` wrapper); hook commands -> PowerShell hook scripts; write the missing demo script | `./init.sh` green |
| **5. Verify** | Green path + deliberate failure paths for the gate and the hooks + manual smoke test | Section 9 matrix all as expected |
| **6. Document** | This guide and the harness guide | |

Porting the app in feature order mattered: it is exactly how the harness would build it
(one feature at a time, tests with each), so the port also validated that the feature
list's acceptance criteria are complete enough to rebuild the app.

---

## 4. File-by-file mapping

### Application and tests

| Python | C# | Notes |
|--------|----|-------|
| `src/__init__.py` | `src/NotesCli/NotesCli.csproj` | Package marker -> project file (no `<PackageReference>`) |
| `src/storage.py` | `src/NotesCli/NoteStorage.cs` | `load`/`save` -> `NoteStorage.Load`/`Save` |
| `src/notes.py` | `src/NotesCli/Note.cs` | dataclass + exceptions in one file, same as the Python module |
| `src/cli.py` (commands, parser, `main`) | `src/NotesCli/Cli.cs` | `cmd_*` -> `Cmd*`; argparse replaced by a small private parser |
| `if __name__ == "__main__": sys.exit(main())` | `src/NotesCli/Program.cs` | 2 lines: UTF-8 console + `return Cli.Run(args, Console.Out, Console.Error);` |
| *(none)* | `NotesCli.slnx`, `Directory.Build.props` | Solution + shared build rules (net10.0, nullable, warnings as errors) |
| `tests/test_storage.py` | `tests/NotesCli.Tests/NoteStorageTests.cs` | |
| `tests/test_notes.py` | `tests/NotesCli.Tests/NoteTests.cs` | |
| `tests/test_cli.py` | `tests/NotesCli.Tests/CliTests.cs` | |
| *(in docs only)* | `tests/NotesCli.Tests/ProgramEndToEndTests.cs` | Level 2 out-of-process test from `docs/verification.md`, now real |
| `tempfile.TemporaryDirectory()` | `tests/NotesCli.Tests/TempDirectory.cs` | Small `IDisposable` helper |
| *(none)* | `tests/NotesCli.Tests/NotesCli.Tests.csproj` | xUnit 2.9.3, test SDK 17.14.1 |

### Harness

| Python repo | This repo | Change |
|-------------|-----------|--------|
| `AGENTS.md` | `AGENTS.md` | Translated; C# paths; `init.ps1`; `guide/` listed as "do not read"; close-out rule points at the review file |
| `CLAUDE.md` | `CLAUDE.md` | Translated; PowerShell added to forbidden edit tools; reviewer closes features |
| `CHECKPOINTS.md` | `CHECKPOINTS.md` | C3/C4 rewritten for .NET (`<PackageReference>`, `TempDirectory`, `dotnet test`); items automated by `init.ps1` are tagged *(init)* |
| `docs/architecture.md` | `docs/architecture.md` | Same 5 principles + #6 "IO is injected"; JSON format section; exit codes 1/2 |
| `docs/conventions.md` | `docs/conventions.md` | PEP 8 -> C# conventions; naming table; xUnit test style |
| `docs/verification.md` | `docs/verification.md` | `dotnet test`; in-process and out-of-process examples; PowerShell + Bash smoke tests |
| `feature_list.json` | `feature_list.json` | Same 7 features, ids, names and statuses; acceptance criteria point at C# files; `notes <args>` shorthand defined |
| `progress/current.md` | `progress/current.md` | Translated template |
| `progress/history.md` | `progress/history.md` | Python history summarized as "inherited" + one entry for the port |
| `progress/explore_*.md` | `progress/explore_*.md` | **Regenerated** by running `scripts/demo-orchestration.cs` on the C# code |
| `progress/impl_cli_edit.md`, `review_cli_edit.md` | *(not carried over)* | They record a real Python session; copying them would be fake memory |
| `.claude/agents/*.md` | `.claude/agents/*.md` | Translated; protocol fixed (section 7); `PowerShell` tool added |
| `init.sh` (bash + inline Python) | `init.ps1` + `init.sh` wrapper | See decision D6 |
| `.claude/settings.json` | `.claude/settings.json` + `scripts/hooks/post-edit.ps1`, `on-stop.ps1` (`settings.template.json` is a reference copy) | See decision D8; installed with your approval |
| `scripts/demo_orchestration.py` *(missing)* | `scripts/demo-orchestration.cs` | Written from scratch, see section 7 |
| `.gitignore` | `.gitignore` | `__pycache__` -> `bin/`, `obj/`, `TestResults/`, `.vs/` |
| `README.md` | `README.md` | Translated, C# commands, links to `guide/` |

---

## 5. Idiom mapping: Python to C#

| Concept | Python original | C# port |
|---------|-----------------|---------|
| Immutable model | `@dataclass(frozen=True) class Note` | `public sealed record Note(int Id, string Title, string Body, string CreatedAt)` |
| "Modify" a note | `Note(id=..., title=new, ...)` | `note with { Title = newTitle }` |
| Factory | `@staticmethod Note.new(title, body, existing)` | `public static Note New(string title, string body, IEnumerable<Note> existing)` |
| Next id | `max((n["id"] for n in existing), default=0) + 1` | `existing.Select(n => n.Id).DefaultIfEmpty(0).Max() + 1` |
| Timestamp | `datetime.now(timezone.utc).isoformat(timespec="seconds")` | `DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)` (same `+00:00` output) |
| Serialization | `asdict(note)` + `json.dump(..., ensure_ascii=False, indent=2)` | `System.Text.Json` with `JsonNamingPolicy.SnakeCaseLower`, `WriteIndented`, `UnsafeRelaxedJsonEscaping`, `NewLine = "\n"` |
| Storage type | `list[dict]` | `List<Note>` (the record *is* the dict) |
| Atomic write | `tempfile.mkstemp(dir=...)` + `os.replace(tmp, path)` | temp file `.notes_<guid>.json` in the same folder + `File.Move(tmp, path, overwrite: true)` |
| Cleanup on failure | `os.unlink(tmp_path)` in `except` | `File.Delete(tempPath)` in `catch` (no-op if missing) + `throw;` |
| Default path | `os.environ.get("NOTES_FILE", ".notes.json")` at import | `NoteStorage.DefaultNotesPath` property, read per call |
| Domain errors | `class NoteError(Exception)`, `class NoteNotFound(NoteError)` | `NoteException`, `NoteNotFoundException` (C# naming) |
| Argument parsing | `argparse` subparsers | Private `Command` table + `ParseArguments` in `Cli.cs`: positionals, `--opt value`, `--opt=value`, `--`, `-h/--help` |
| Usage errors | argparse prints usage to stderr, exit 2 | Same: usage line + `notes: error: ...`, exit 2 |
| `type=int` | argparse converts or fails with exit 2 | `Invocation.GetInt` -> `UsageException` -> exit 2 |
| Case-insensitive search | `query.lower() in text.lower()` | `text.Contains(query, StringComparison.OrdinalIgnoreCase)` |
| Output | `print(...)` | injected `TextWriter stdout` |
| Errors | `print(..., file=sys.stderr)`; return 1 | injected `TextWriter stderr`; return 1 |
| Entry point | `sys.exit(main())` | top-level `return Cli.Run(args, Console.Out, Console.Error);` |
| Test framework | `unittest.TestCase` | xUnit: plain class + `[Fact]` |
| `setUp` / `tearDown` | methods | constructor / `IDisposable.Dispose` |
| Temp folder | `tempfile.TemporaryDirectory()` | `TempDirectory` (wraps `Directory.CreateTempSubdirectory`) |
| Redirect output | `redirect_stdout(io.StringIO())` | `new StringWriter()` passed to `Cli.Run` |
| Point CLI at temp file | `patch.object(storage, "DEFAULT_NOTES_PATH", path)` | pass `notesPath` to `Cli.Run` (no global state, see D5) |
| "Frozen" test | `with assertRaises(Exception): note.title = "x"` | Reflection test: no public property has a non-`init` setter; plus a `with` test. Assigning would not even compile. |
| Run tests | `python3 -m unittest discover -s tests -v` | `dotnet test NotesCli.slnx --nologo` |
| Style rules | PEP 8, line <= 100 | C# conventions, line <= 120; compiler with `TreatWarningsAsErrors` + nullable as the automatic part |
| "No dependencies" | `requirements.txt` empty or missing | no `<PackageReference>` in `NotesCli.csproj` (checked by `init.ps1`) |
| Version gate | Python >= 3.9 | .NET SDK >= 10 |

---

## 6. Design decisions

**D1: .NET 10 (`net10.0`).** Current LTS, installed on the target machine (SDK
10.0.403). Gives `JsonNamingPolicy.SnakeCaseLower`, `JsonSerializerOptions.NewLine`,
primary constructors, collection expressions and file-based apps (`dotnet run file.cs`)
for the demo script. Set once in `Directory.Build.props`.

**D2: zero NuGet packages in the app, xUnit in the tests.** The original rule is
"stdlib only". In .NET the equivalent is "Base Class Library only". `System.CommandLine`
and `Newtonsoft.Json` were ruled out for that reason. Tests need a framework (Python's
`unittest` is in the stdlib, there is no BCL equivalent), so the rule applies to
`src/NotesCli` only, and `init.ps1` checks exactly that project.

**D3: a hand-written parser that mimics argparse.** About 120 lines of private code in
`Cli.cs` (a `Command` table, `ParseArguments`, `UsageException`). It keeps the three-layer
architecture (no new file), the exit codes (1 domain, 2 usage), and argparse-style
messages (`the following arguments are required: id`, `invalid choice: 'x'`,
`argument id: invalid int value: 'x'`). Not supported on purpose: option abbreviations
(`--bo`) and single-dash short options, which no test or feature uses.

**D4: `CreatedAt` stays a `string`.** A `DateTimeOffset` would be more idiomatic, but
the string keeps the file byte-compatible with Python and round-trips exactly. ISO 8601
strings with the same `+00:00` offset sort correctly as text, so feature 7
(`recent`, order by `created_at`) still works with a simple `OrderByDescending`.

**D5: inject IO instead of patching globals.** Python tests patch a module constant.
The C# signature is `Cli.Run(argv, stdout, stderr, notesPath = null)`. No shared mutable
state means xUnit can run test classes in parallel safely, and it matches the
architecture rule "pass the path explicitly or use the default". Added to
`docs/architecture.md` as principle 6, and enforced by `init.ps1` (no `Console` outside
`Program.cs`).

**D6: `init.ps1` as the single gate, `init.sh` as a thin wrapper.** Bash + inline
Python does not fit a Windows/.NET machine without Python. PowerShell is on every Windows
box (5.1) and cross-platform (`pwsh`). `init.sh` stays so that `./init.sh`, the habit
from the video and the original repo, still works from Git Bash, macOS and Linux. It
only picks `pwsh` or `powershell.exe` and delegates. One source of truth for the logic.
The script is **ASCII-only** because Windows PowerShell 5.1 reads BOM-less files with the
ANSI code page (box-drawing characters would turn into mojibake).

**D7: the gate checks more than the original.** Every check that can be automated
moved from "the reviewer should notice" to "the script fails": no NuGet in the app, only
the allowed source files, no `Console` outside `Program.cs`, warnings on TODO/FIXME,
duplicated feature ids, and a warning that shows an `in_progress` feature left by the
previous session. This applies the video's point that the AI must *prove* things, to the
harness itself.

**D8: hooks rewritten as scripts with real feedback.** In the original, hook commands
print to stdout with exit code 0. Claude Code writes that stdout to its debug log, so
Claude never sees it. The port uses:

| Hook | Original | Port |
|------|----------|------|
| PostToolUse `Edit\|Write` | always ran all tests, printed the last 3 lines | `post-edit.ps1`: skips non-code files (docs/progress edits stay instant); on red, **exit 2 with the failing tests on stderr**, which Claude Code feeds back to Claude |
| Stop | ran `init.sh` to `/tmp/harness_init.log`, printed OK/FAILED | `on-stop.ps1`: on red, **exit 2** (Claude must fix or record the blocker); honours `stop_hook_active` to never loop; log in `%TEMP%` |
| Keys | `"$schema_comment"`, per-hook `"description"` | Removed (not valid settings); `"$schema"` added |
| Command | bash pipeline | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$CLAUDE_PROJECT_DIR/scripts/hooks/<x>.ps1"`: finds the script whatever the session's current folder, and does not depend on the machine's execution policy. `$CLAUDE_PROJECT_DIR` is expanded by Git Bash, which Claude Code uses for hooks on Windows when it is installed; on a machine without Git Bash, change the path back to the relative `scripts/hooks/<x>.ps1` |

**D9: protocol fixes.** See section 7. Main one: only the reviewer can mark a feature
`done`, and only with `APPROVED`.

**D10: `guide/` is outside the agent's map.** Learning material would only fill the
agent's context window, so `AGENTS.md` lists it as "do not read". This applies the
context-hygiene principle from the video.

**D11: memory is regenerated, not copied.** `explore_*.md` files were produced by
actually running the new demo script on the C# code. The Python implementer/reviewer
reports were not copied: memory must describe *this* repository. `history.md` records
the inheritance in one summarized entry.

---

## 7. Issues found in the original and how they were resolved

| # | Issue in the Python repo | Resolution in the port |
|---|--------------------------|------------------------|
| 1 | `scripts/demo_orchestration.py` is referenced by `AGENTS.md` and `CLAUDE.md` but is not in the repo | Wrote `scripts/demo-orchestration.cs`: 3 workers in parallel, each analyses one file, writes `progress/explore_<topic>.md`, returns `done -> <path>`; the leader prints only references |
| 2 | `reviewer.md` writes the verdict to `progress/review.md`; `README.md`, `leader.md` and the real session use `progress/review_<feature>.md` | Standardized on `progress/review_<feature>.md` everywhere |
| 3 | `implementer.md` step 7 says "call a reviewer", but the implementer has no `Agent` tool | The leader launches the reviewer (as `leader.md` already said); the implementer stops after writing its report |
| 4 | Who marks `done` is contradictory: `CLAUDE.md` says the implementer after review; in the real session the implementer marked it `done` *before* the review | Only the reviewer sets `done`, only on `APPROVED`. Implementer: `pending -> in_progress`. Leader: never. |
| 5 | The leader must write `progress/history.md` but `leader.md` lists no `Write`/`Edit` tools | Added `Write, Edit`, restricted by rule to `progress/` and docs |
| 6 | No loop for rejected reviews | Leader relaunches the implementer with the review file; after 2 rejections the feature goes `blocked` |
| 7 | Hook output never reached Claude (stdout at exit 0); invalid keys in `settings.json` | Section 6, D8 |
| 8 | Stop hook used `/tmp`, which does not exist natively on Windows | `%TEMP%` via `[IO.Path]::GetTempPath()` |
| 9 | `docs/architecture.md` says a corrupt file should raise a named exception, but the code does not | Kept parity (not implemented in either version); listed as a candidate feature in the harness guide |
| 10 | `docs/verification.md` describes a Level 2 subprocess test that no test implements | Implemented as `ProgramEndToEndTests` (2 tests running the real `NotesCli.dll` with `NOTES_FILE`) |

---

## 8. Test parity

All 22 Python tests have a C# counterpart. 10 tests were added for C#-specific
behaviour (the custom parser, the JSON contract, the real executable).

| Python test | C# test |
|-------------|---------|
| `test_load_returns_empty_when_file_missing` | `NoteStorageTests.Load_ReturnsEmpty_WhenFileMissing` |
| `test_save_then_load_roundtrip` | `NoteStorageTests.SaveThenLoad_RoundTrips` |
| `test_save_is_atomic` | `NoteStorageTests.Save_IsAtomic_AndLeavesNoTempFiles` |
| `test_to_dict_round_trip` | `NoteStorageTests.Save_WritesSnakeCaseKeys` (moved: serialization lives in storage now) |
| `test_new_assigns_id_one_when_no_existing` | `NoteTests.New_AssignsIdOne_WhenNoExistingNotes` |
| `test_new_increments_id` | `NoteTests.New_IncrementsIdFromHighestExisting` |
| `test_note_is_frozen` | `NoteTests.Note_HasNoPublicMutableProperties` |
| `test_add_creates_note_and_prints_id` | `CliTests.Add_CreatesNote_AndPrintsId` |
| `test_list_shows_existing_notes` | `CliTests.List_ShowsExistingNotes` |
| `test_list_empty_outputs_nothing` | `CliTests.List_WhenEmpty_OutputsNothing` |
| `test_show_prints_title_date_body` | `CliTests.Show_PrintsTitleDateAndBody` |
| `test_show_missing_id_returns_error` | `CliTests.Show_WithMissingId_ReturnsError` |
| `test_delete_removes_note_and_confirms` | `CliTests.Delete_RemovesNote_AndConfirms` |
| `test_delete_missing_id_returns_error` | `CliTests.Delete_WithMissingId_ReturnsError` |
| `test_search_finds_matching_notes` | `CliTests.Search_FindsMatchingNotes` |
| `test_search_no_match_returns_error` | `CliTests.Search_WithNoMatch_ReturnsError` |
| `test_search_is_case_insensitive` | `CliTests.Search_IsCaseInsensitive` |
| `test_edit_updates_only_title` | `CliTests.Edit_WithTitleOnly_UpdatesOnlyTitle` |
| `test_edit_updates_only_body` | `CliTests.Edit_WithBodyOnly_UpdatesOnlyBody` |
| `test_edit_updates_both_fields` | `CliTests.Edit_WithBothFlags_UpdatesBothFields` |
| `test_edit_missing_id_returns_error` | `CliTests.Edit_WithMissingId_ReturnsError` |
| `test_edit_without_flags_returns_error` | `CliTests.Edit_WithoutFlags_ReturnsError` |

New tests:

| C# test | Why |
|---------|-----|
| `NoteStorageTests.Load_ReadsFileWrittenByPythonVersion` | Proves constraint C2 (shared file format, including non-ASCII text) |
| `NoteTests.New_SetsCreatedAtAsIso8601Utc` | Pins the exact timestamp format feature 7 relies on |
| `NoteTests.With_CreatesNewInstance_AndLeavesOriginalUnchanged` | Documents the "modify = new instance" rule |
| `CliTests.Add_AcceptsInlineOptionValue` | Parser: `--body=value` (argparse supports it) |
| `CliTests.Show_WithNonNumericId_ReturnsUsageError` | Parser: `type=int` equivalent, exit 2 |
| `CliTests.Run_WithUnknownCommand_ReturnsUsageError` | Parser: invalid command, exit 2 |
| `CliTests.Run_WithoutArguments_ReturnsUsageError` | Parser: command is required, exit 2 |
| `CliTests.Run_WithHelp_PrintsUsageAndSucceeds` | Parser: `--help`, exit 0 |
| `ProgramEndToEndTests.Executable_UsesNotesFileEnvironmentVariable` | Level 2 verification: the real executable + `NOTES_FILE` |
| `ProgramEndToEndTests.Executable_ReturnsExitCodeOne_ForDomainErrors` | Real process exit code for domain errors |

Messages were translated (`no existe la nota con id=99` -> `note with id=99 does not
exist`, `borrada id=1` -> `deleted id=1`, `editada id=1` -> `edited id=1`, ...). The
tests assert the same things as the Python ones (`id=1` in stdout, the id in stderr, empty
stdout on error), so the translation did not weaken them.

---

## 9. Verification results

Run on Windows 11, .NET SDK 10.0.403, Windows PowerShell 5.1, Git Bash.

**Green path:** `./init.sh` -> all 6 sections `[OK]`, `Passed! - Failed: 0, Passed: 32`,
exit 0, about 5 s.

**Failure paths** (run on a scratch copy of the repo, so nothing in this repo was
modified):

| Case | Expected | Observed |
|------|----------|----------|
| A. Two features `in_progress` | `init.ps1` exit 1 | `[FAIL] There are 2 features in_progress (max 1)`, exit 1 |
| B. One feature `in_progress` | Pass with a warning | `[WARN] Feature 7 'cli_recent' is in_progress: resume it`, exit 0 |
| C. NuGet package + extra `Helper.cs` with `Console.WriteLine` and a TODO | exit 1, every issue named | 3 `[FAIL]` (PackageReference, unexpected file, Console use with file:line) + 1 `[WARN]` (TODO), exit 1 |
| D. A failing test | exit 1 | `[FAIL] Build or tests are broken`, exit 1 |
| E. `post-edit.ps1` after editing a `.md` | skip, exit 0 | exit 0, nothing run |
| F. `post-edit.ps1` after editing a `.cs` with a red test | exit 2 + failure on stderr | exit 2, stderr names `NoteTests.New_IncrementsIdFromHighestExisting` and the assert |
| G. `on-stop.ps1`, red, first time | exit 2 + instructions | exit 2, "init.ps1 FAILED ... record the state ... set the feature to blocked" |
| H. `on-stop.ps1`, red, `stop_hook_active: true` | exit 0, no loop | exit 0 + `{"systemMessage": "..."}` |
| I. Both hooks when green | exit 0 | exit 0, exit 0 |

**Manual smoke test** with `NOTES_FILE` pointing at a temp file: `--help`, `add` (with
non-ASCII text), `list`, `show`, `edit`, case-insensitive `search`, `delete` of a missing
id (exit 1), `edit x` (exit 2 + usage). The resulting JSON has snake_case keys, 2-space
indentation and raw UTF-8, the same as Python's output.

**Demo script:** `dotnet run scripts/demo-orchestration.cs` produced the three
`progress/explore_*.md` files and printed only references. The first run takes about
40 s because it compiles the script; later runs are cached.

---

## 10. Deliberately not done

| Item | Why | How to do it |
|------|-----|--------------|
| Feature 7 `cli_recent` | Kept `pending` so the harness can be exercised (constraint C6) | Harness guide, section 10 |
| `git init` / commits | The folder is not a repository yet, and committing is the owner's call | `git init`, then commit; `.gitignore` is ready |
| CI pipeline | Not in the original | One job running `pwsh ./init.ps1` (see harness guide, section 11) |

---

## 11. Next steps

1. `git init` and commit, so `progress/` and `feature_list.json` changes are versioned
   (the harness relies on versioned history).
2. In a **new** Claude Code session (so the hooks in `.claude/settings.json` load), run
   feature 7: *"implement the next pending feature"*. Compare
   the resulting `progress/impl_cli_recent.md` and `review_cli_recent.md` with the
   Python repo's `impl_cli_edit.md` / `review_cli_edit.md`.
3. Add a second exercise to `feature_list.json` (for example, "corrupt notes file ->
   named exception, exit 1, no stack trace"), which closes issue #9.
4. When an agent misbehaves, fix the harness, not just the output (pillar 3).

---

## 12. Command cheat sheet

```bash
./init.sh                                            # the gate (Bash / Git Bash)
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\init.ps1   # the gate (PowerShell)
```

```bash
dotnet test NotesCli.slnx --nologo                   # tests only
```

```bash
dotnet run --project src/NotesCli -- add "title" --body "text"
```

```bash
dotnet run --project src/NotesCli -- list
```

```bash
dotnet run scripts/demo-orchestration.cs             # leader/worker demo
```
