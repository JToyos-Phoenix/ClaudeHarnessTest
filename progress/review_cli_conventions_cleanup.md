# Review: feature 9 `cli_conventions_cleanup`

**Verdict:** APPROVED

- **Reviewer:** reviewer agent (Claude Opus 5.5), launched by the leader
- **Date:** 2026-11-05
- **Inputs:** `progress/impl_cli_conventions_cleanup.md`, `feature_list.json` (feature 9), `docs/architecture.md`,
  `docs/conventions.md`, `docs/verification.md`, `CHECKPOINTS.md`, `git diff 8e30cd7` on `src/` and `tests/`
- **Git:** read only (`git diff`, `git show`, `git archive`, `git status`). Nothing staged, committed, stashed,
  checked out or pushed.

## Acceptance criteria

- [x] Every line in `src/NotesCli/Cli.cs` is at most 120 characters
      -> Evidence (no unit test can pin this): I counted it two ways. `awk length` on the LF file gives 315 lines
         and max 114 (line 287). .NET `File.ReadAllLines` gives 315 lines, 0 over 120, max 114. The file has
         0 CR bytes, 0 tabs, 0 non-ASCII bytes and no BOM, so bytes and characters agree. Old line 224 (123 cols) is
         now `Cli.cs:224-226`.
- [x] `GeneralUsage()` builds its lines with a collection expression instead of `new List<string> { ... }`
      -> Evidence (source inspection): `Cli.cs:245-253`, `string[] lines = [ ..., .. Commands.Select(...), ];`.
         There is no `new List` or `AddRange` in `Cli.cs:243-255`. Behaviour is covered by
         `CliTests.Run_ListsEveryCommandAfterDescription_WhenHelpRequested` and `AssertGeneralUsageHeader`
         (`CliTests.cs:434-439`).
- [x] `GeneralUsage()` uses no `+` string concatenation
      -> Evidence (source inspection): `grep '+'` on `Cli.cs:243-255` finds nothing. The return is
         `$"{string.Join(Environment.NewLine, lines)}{Environment.NewLine}"` (`Cli.cs:254`).
- [x] The general help output (`--help`, `-h`, no command, unknown command) is byte-for-byte identical to `8e30cd7`,
      and all exit codes are unchanged
      -> Evidence (my own out-of-process run, see "Evidence I produced myself"): I compared 15 cases x (stdout,
         stderr, exit code) and `diff -r` exited 0. In-repo tests that pin parts of this:
         `CliTests.Run_PrintsTest23DescriptionOnStdout_WhenHelpRequested("--help" / "-h")` (exit 0, empty stderr),
         `CliTests.Run_PrintsTest23DescriptionOnStderr_WhenNoCommand` (exit 2),
         `CliTests.Run_PrintsTest23DescriptionOnStderr_WhenCommandUnknown` (exit 2) and
         `CliTests.Run_ListsEveryCommandAfterDescription_WhenHelpRequested`. They do not pin CRLF or the trailing
         newline, because `Lines()` normalizes both (`CliTests.cs:451`). The byte comparison covers those.
- [x] `tests/NotesCli.Tests/CliTests.cs` pins every full row of the command list (signature and help columns)
      -> `CliTests.Run_ListsEveryCommandAfterDescription_WhenHelpRequested` (`CliTests.cs:415-432`):
         `Assert.Equal(expected, Lines(output)[5..])` with the 7 full padded rows. This pins the content, the order,
         the row count and the 40-column padding. I checked that it fails under 6 mutations and that the old version
         missed one (see below).
- [x] All existing tests still pass
      -> `init.ps1` and `./init.sh`: `Passed! - Failed: 0, Passed: 47, Skipped: 0, Total: 47`, exit 0. The new
         `CliTests.cs` also passes 47/47 against the `8e30cd7` sources.

## Files reviewed

| File | Architecture | Conventions | Test |
|------|--------------|-------------|------|
| `src/NotesCli/Cli.cs` (`:224-226`, `:245-254` only) | OK. The change stays in `ParseArguments` / `GeneralUsage` in the UI layer. No new type, member, dependency or `Console` call | OK. Interpolation only. The collection expression has a trailing comma, like `Commands` at `:11-20`. The wrap at `:224-226` has one argument per line, like `FindCommand` (`:174-176`). Max line 114. LF endings, no tabs | `CliTests.Run_ListsEveryCommandAfterDescription_WhenHelpRequested`, `Run_PrintsTest23*`, plus the existing usage-error tests |
| `tests/NotesCli.Tests/CliTests.cs` (`:421-431` only) | OK. In-process `Cli.Run` with `StringWriter`s through the `Run` helper and the `TempDirectory` path (`:6-9`). No `Console`, no mocks | OK. Test name unchanged. Act / blank line / Assert. Collection expression for `expected`. Longest new line is 97 cols (`:429`) | n/a |
| `feature_list.json` | Only feature 9 `pending -> in_progress` against HEAD (`git diff`) | - | - |
| `progress/current.md` | Describes the active session (feature 9) | - | - |

Scope check against `8e30cd7`. A byte-level `diff` of the committed blob against the working file shows exactly two
hunks in `Cli.cs`: `224c224,226` and `243,245c245,254`. `CliTests.cs` has one hunk, `421,422c421,431`.
`git diff 8e30cd7 -- src tests` lists no other file. `git diff 8e30cd7 HEAD --stat` touches only `feature_list.json`.
Nothing else in `Cli.cs` changed. No public member was added, so `docs/verification.md` Level 1 has nothing new
to cover.

## Evidence I produced myself

All of this ran in the session scratchpad, outside the repo. I deleted it afterwards and left the unrelated
`commit-msg.txt` alone.

- **Baseline build.** `git -c core.autocrlf=false -c core.eol=lf archive 8e30cd7 Directory.Build.props NotesCli.slnx
  src tests | tar -x`. `cmp` confirmed the extracted `Cli.cs` matches `git show 8e30cd7:src/NotesCli/Cli.cs`. The
  "after" tree is a `cp` of the working-tree `src/` and `tests/`, also confirmed with `cmp`. I built both with
  `dotnet build ... -o <bin>`: 0 warnings, 0 errors each.
- **Byte comparison.** I ran each DLL with `NOTES_FILE` set to a scratch path that does not exist, from an empty
  scratch working directory, and captured stdout, stderr and exit code. Cases: `--help`, `-h`, no args, `fly`,
  `FLY extra --x`, `--help list`, `-h x`, `show`, `add`, `edit`, `search`, `delete` (the old line-224 path),
  `list --help`, `show abc`, `recent --limit 0`.
  `diff -r out-base out-after` exited 0. The general help cases:

  | Case | exit | stdout bytes | stderr bytes | sha256 (first 12) |
  |------|------|--------------|--------------|-------------------|
  | `--help` | 0 | 514 | 0 | out `6bfc0de5bad0` |
  | `-h` | 0 | 514 | 0 | out `6bfc0de5bad0` |
  | (none) | 2 | 0 | 577 | err `b032caa46189` |
  | `fly` | 2 | 0 | 613 | err `721dab2f60ae` |

  `cat -A` shows CRLF on every line. The help ends `) . \r \n`, which is exactly one trailing newline. The blank
  line before `notes: error:` is still there for no args and `fly`. No notes file was created.
- **The hardened test fails when a row changes.** I mutated a scratch copy of `Cli.cs`, never the repo, and ran only
  `Run_ListsEveryCommandAfterDescription_WhenHelpRequested`. It failed (1/1) under every mutation:
  A `"Search notes by word."` -> `"Search notes by keyword."`;
  B `"Add a note."` -> `"Add a note"`;
  C `{c.Signature,-40}` -> `-41`;
  D `[--{o} {O}]` -> `[--{o}={O}]` (signature column);
  E `(default {DefaultRecentLimit})` -> `(default: ...)`;
  F row indent of two spaces -> one space.
  The pre-feature version of the test (from `8e30cd7`) **passed** mutation A, so the hardening is real.
- **The new test file still passes on the old code.** I ran the new `CliTests.cs` against the `8e30cd7` sources:
  47/47 green. The test pins existing behaviour and does not change it.
- **init.** `powershell -NoProfile -ExecutionPolicy Bypass -File .\init.ps1` and `./init.sh` both gave
  `[OK] Environment ready`, **exit 0**, and 47/47 tests. The only `[WARN]` is feature 9 `in_progress`, and this
  approval clears it.

## Checkpoints

- C1: [x]
  - [x] The 4 base files exist (init step 2)
  - [x] The 3 docs exist (init step 2)
  - [x] `./init.sh` ends with exit code 0 (my run above)
- C2: [x]
  - [x] At most one feature `in_progress` (only 9; init step 3)
  - [x] Every `done` feature has tests that pass (47/47 green)
  - [x] `progress/current.md` describes the active session (feature 9, started 2026-11-05 10:46)
- C3: [x]
  - [x] `src/NotesCli/` only has `Cli.cs`, `Note.cs`, `NoteStorage.cs`, `Program.cs` and the csproj (init step 4)
  - [x] `NotesCli.csproj` has no `<PackageReference>` (init step 4; the file is unchanged since `8e30cd7`)
  - [x] `Console.*` only appears in `Program.cs:4-5`. There is no debug output and no TODO/FIXME/HACK in `src/` or
        `tests/`
- C4: [x]
  - [x] There is a test class per source file: `CliTests`, `NoteTests`, `NoteStorageTests`, `ProgramEndToEndTests`
  - [x] Tests use `TempDirectory` (`CliTests.cs:6-9`) and no filesystem mocks
  - [x] `dotnet test NotesCli.slnx` runs 47 tests and all are green
- C5: [x]
  - [x] No suspicious untracked files. `git status --untracked-files=all` lists only the 4 modified files and
        `progress/impl_cli_conventions_cleanup.md`. There is no `*.tmp`, `.notes*.json` or `notes.json` outside
        `bin/` / `obj/`. `bin/`, `obj/` and `.vs/` are gitignored
  - [x] `progress/history.md` has an entry for the last closed session (2026-11-04, feature 8). The leader appends
        this session's entry after this approval (`.claude/agents/leader.md:30-31`)
  - [x] Feature 9 moves to `done` with this approval

## Required changes

None for feature 9.

## Observations for the leader (non-blocking, out of scope for feature 9)

1. **More collection-expression debt in `Cli.cs`, not named in feature 9's acceptance and not flagged by the
   feature 7/8 reviews.** `Cli.cs:182` is `var positionals = new List<string>();`, which could be
   `List<string> positionals = [];`. `Cli.cs:265` is `new[] { Name }`. Both are the same in `8e30cd7`, and
   `docs/conventions.md:18` asks for collection expressions. The implementer was right to leave them: the leader
   asked that nothing else in `Cli.cs` change. They are a candidate for a follow-up feature.
2. **Line length in test files (pre-existing, untouched):** `tests/NotesCli.Tests/ProgramEndToEndTests.cs:5`
   (121 cols) and `tests/NotesCli.Tests/TempDirectory.cs:3` (125 cols), both `/// <summary>` lines.
   `docs/conventions.md:11` sets 120 columns for all code.
3. **Test gaps (pre-existing).** No in-process test covers the `ParseArguments` "the following arguments are
   required: <name>" message (`Cli.cs:224-226`). No in-process test pins the trailing newline of `GeneralUsage()`
   or the blank line before `notes: error:`, because `CliTests.Lines()` normalizes both. I checked them out of
   process for this review. A future hardening feature could pin them in process.
4. **Session close.** Append the session summary to `progress/history.md` and reset `progress/current.md`
   (`.claude/agents/leader.md:30-31`).
