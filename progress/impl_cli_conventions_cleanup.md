# Implementation report: feature 9 - cli_conventions_cleanup

- **Agent:** implementer
- **Date:** 2026-11-05
- **Status left in `feature_list.json`:** `in_progress` (waiting for review; not marked `done`)
- **Git:** nothing staged, committed or pushed. `.git` was only read (`git show`, `git diff`, `git archive`).

## Files touched

| File | Change |
|------|--------|
| `src/NotesCli/Cli.cs` | (1) `ParseArguments`, old line 224: the `UsageException` call is wrapped over 3 lines. (2) `GeneralUsage()`: collection expression with spread instead of `new List<string> { ... }` + `AddRange`, and interpolation instead of `+ Environment.NewLine`. Nothing else in the file changed. |
| `tests/NotesCli.Tests/CliTests.cs` | `Run_ListsEveryCommandAfterDescription_WhenHelpRequested` hardened in place. It now asserts the 7 full command-list rows (signature column padded to 40 plus help column) instead of the first word of each row. No other test changed. |
| `feature_list.json` | Feature 9 status `pending` -> `in_progress`. |
| `progress/current.md` | Session plan and log. |

`git diff --stat`: 4 files, +60 / -15 (most of it is `progress/current.md`). `Cli.cs` +13 / -4, `CliTests.cs` +11 / -2.

### `Cli.cs` diff (whitespace-sensitive, complete)

```diff
@@ -221,7 +221,9 @@
             var missing = command.Positionals[positionals.Count..];
-            throw new UsageException(command.Usage, $"the following arguments are required: {string.Join(", ", missing)}");
+            throw new UsageException(
+                command.Usage,
+                $"the following arguments are required: {string.Join(", ", missing)}");
@@ -240,9 +242,16 @@
     private static string GeneralUsage()
     {
-        var lines = new List<string> { $"usage: {ProgramName} <command> [options]", "", "TEST23", "", "commands:" };
-        lines.AddRange(Commands.Select(c => $"  {c.Signature,-40} {c.Help}"));
-        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
+        string[] lines =
+        [
+            $"usage: {ProgramName} <command> [options]",
+            "",
+            "TEST23",
+            "",
+            "commands:",
+            .. Commands.Select(c => $"  {c.Signature,-40} {c.Help}"),
+        ];
+        return $"{string.Join(Environment.NewLine, lines)}{Environment.NewLine}";
     }
```

## Acceptance criteria -> evidence

| # | Criterion | Evidence |
|---|-----------|----------|
| 1 | Every line in `Cli.cs` <= 120 characters | PowerShell `ReadAllLines` character count: `lines=315 over120=0 max=114 nonAsciiLines=0`. A byte-based `awk` check agrees (longest is line 287, 114 cols). Old line 224 (123 cols) is now 3 lines of 37, 30 and 87 cols (lines 224-226). |
| 2 | `GeneralUsage()` uses a collection expression, not `new List<string> { ... }` | `string[] lines = [ ... ];` (see diff). The spread `.. Commands.Select(...)` folds the old `AddRange` into the same expression. |
| 3 | `GeneralUsage()` has no `+` concatenation | `return $"{string.Join(Environment.NewLine, lines)}{Environment.NewLine}";`. There is no `+` anywhere in the method. |
| 4 | Help output for `--help`, `-h`, no command and unknown command is byte-identical to `8e30cd7`; exit codes unchanged | Out-of-process comparison below: 21 files (stdout, stderr, exit code for 7 cases), all byte-identical. |
| 5 | `CliTests.cs` pins every full row of the command list | Hardened `Run_ListsEveryCommandAfterDescription_WhenHelpRequested`. It is green on both old and new code, and it fails under two mutations that the old version would have let through (see below). |
| 6 | All existing tests still pass | `init.ps1`: 47/47 passed, build clean (`TreatWarningsAsErrors`). |

## Design decisions

- **Hardened the existing test in place instead of adding a second one.** The full-row assertion is a strict
  superset of the old names-and-order check: the row strings contain the names in the same order, and
  `Assert.Equal` on the array also pins the row count. Keeping both would leave a redundant, weaker test. The
  name still describes the test, so I kept it, and the test count stays at 47. The test checks `--help` only.
  `-h`, no command and unknown command use the same `GeneralUsage()` string, and the byte comparison covers
  them.
- **Expected rows were copied from the real baseline output** (`8e30cd7` build, `cat -A`), not typed by hand,
  so the padding is exact.
- **`string[]` rather than `List<string>`** for `lines`. Nothing mutates it now that `AddRange` is gone, and
  `string[] x = [...]` matches the existing style (`Commands` in `Cli.cs`, `expected` arrays in `CliTests.cs`).
  `var` is not possible because collection expressions have no natural type.
- **Trailing newline kept with `$"{Join}{NewLine}"`, not a trailing `""` element.** This is the literal
  translation of the old `Join(...) + NewLine`, so the output cannot change. A trailing `""` in the array would
  also give the right bytes, but a reader could take it for a blank line in the help.
- **One element per line in the collection expression.** On one line it would be over 120 columns. Laid out
  this way, the array shows the 5 header lines of the help as they print.
- **Line 224 wrapped in the shape the file already uses.** One argument per line, like `FindCommand`
  (`Cli.cs:174-176`) and `Invocation.GetInt`/`GetPositiveInt`. Only whitespace changed: the string literal and
  the arguments are untouched.
- **Scope guard.** Nothing else in `Cli.cs` was reformatted. Out-of-scope debt I found is listed under
  "Observations" and was not fixed.
- **No new unit test for the `ParseArguments` "required" message (old line 224).** No existing in-process
  test reaches that path (`show`/`add`/`edit` without their positional). The acceptance list does not ask for
  one, and the change there is whitespace-only. I covered it in the out-of-process byte comparison instead
  (3 extra cases below). If the reviewer wants an in-process test there, it is a one-method addition.

## Test first: green before the refactor, and shown to be meaningful

1. Hardened test written while `Cli.cs` was still byte-identical to `8e30cd7` (checked with `cmp` against
   `git show 8e30cd7:src/NotesCli/Cli.cs`). Result: `Passed! - Failed: 0, Passed: 47, Total: 47`. This is
   expected because the test pins existing behaviour.
2. Mutation A (help column): `"Search notes by word."` -> `"Search notes by keyword."` in `Cli.cs:17`.
   ```
   Failed NotesCli.Tests.CliTests.Run_ListsEveryCommandAfterDescription_WhenHelpRequested [8 ms]
   Expected: [..., "  search <query>                           Search "..., ...]
   Actual:   [..., "  search <query>                           Search "..., ...]
                                                                  ^ (pos 4)
   Failed!  - Failed:     1, Passed:    46, Skipped:     0, Total:    47
   ```
3. Mutation B (signature column width): `{c.Signature,-40}` -> `{c.Signature,-41}` in `GeneralUsage()`.
   ```
   Failed NotesCli.Tests.CliTests.Run_ListsEveryCommandAfterDescription_WhenHelpRequested [10 ms]
   Failed!  - Failed:     1, Passed:    46, Skipped:     0, Total:    47
   ```
   The old names-only version of the test would have passed both mutations.
4. Each mutation was reverted with the inverse `sed`, and `cmp` then confirmed `Cli.cs` was byte-identical to
   the `8e30cd7` blob again. I did not use `git checkout`: with `core.autocrlf=true`, it would rewrite the
   LF working file as CRLF.

Then the refactor, one change at a time, with the suite run after each: `GeneralUsage()` gave 47/47, and then
line 224 gave 47/47.

## Byte-for-byte evidence against `8e30cd7`

**Baseline build** (in the session scratchpad, outside the repo; `.git` was only read):

```
git -c core.autocrlf=false -c core.eol=lf archive 8e30cd7 Directory.Build.props src/NotesCli | tar -x -C <scratch>/baseline-src
cmp <scratch>/baseline-src/src/NotesCli/Cli.cs <(git show 8e30cd7:src/NotesCli/Cli.cs)   # identical
dotnet build <scratch>/baseline-src/src/NotesCli/NotesCli.csproj -o <scratch>/baseline-bin  # 0 warnings, 0 errors
```

The first extraction without the `-c` flags came out CRLF (`core.autocrlf=true` plus `* text=auto` in
`.gitattributes`). I redid it so the baseline source matched the committed blob byte-for-byte.

**After build:** `dotnet build src/NotesCli/NotesCli.csproj` (0 warnings, 0 errors).

**Capture script** (deleted afterwards). It ran the same for both DLLs, with `NOTES_FILE` pointing to a
scratch path, and asserted that no notes file was created:

```bash
run_case() { local name="$1"; shift; dotnet "$dll" "$@" > "$out/$name.out" 2> "$out/$name.err"; echo "$?" > "$out/$name.code"; }
run_case help-long --help;        run_case help-short -h
run_case no-command;              run_case unknown-command fly
run_case show-missing-id show;    run_case add-missing-title add;    run_case edit-missing-id edit
```

**Result:** `cmp` on each file and `diff -r baseline-out after-out`, which exited with 0.

| Case | argv | exit (old = new) | stdout bytes | stderr bytes | identical |
|------|------|------------------|--------------|--------------|-----------|
| help-long | `--help` | 0 | 514 | 0 | yes (sha256 `6bfc0de5...b682`) |
| help-short | `-h` | 0 | 514 | 0 | yes (sha256 `6bfc0de5...b682`) |
| no-command | (none) | 2 | 0 | 577 | yes (sha256 `b032caa4...1c0d`) |
| unknown-command | `fly` | 2 | 0 | 613 | yes (sha256 `721dab2f...3154`) |
| show-missing-id | `show` | 2 | 0 | 80 | yes (sha256 `84a7a368...0d50`) |
| add-missing-title | `add` | 2 | 0 | 99 | yes (sha256 `9cb9c74a...9775`) |
| edit-missing-id | `edit` | 2 | 0 | 110 | yes (sha256 `69ad932d...1400`) |

The last three cases go through the wrapped old line 224.

Byte-level details that were preserved (`cat -A` of the new output; `^M$` = `\r\n`):

```
== --help stdout ==
usage: notes <command> [options]^M$
^M$
TEST23^M$
^M$
commands:^M$
  add <title> [--body BODY]                Add a note.^M$
  ...
  recent [--limit LIMIT]                   List the most recent notes (default 5).^M$
last 4 bytes: ) . \r \n          <- exactly one trailing newline

== no command, stderr tail ==
  recent [--limit LIMIT]                   List the most recent notes (default 5).^M$
^M$                              <- the existing blank line before the error, still there
notes: error: the following arguments are required: command^M$

== fly, stderr tail ==
^M$
notes: error: invalid choice: 'fly' (choose from add, list, show, delete, search, edit, recent)^M$

== show / add / edit with no positional (old line 224 path) ==
usage: notes show <id>^M$
notes: error: the following arguments are required: id^M$
usage: notes add <title> [--body BODY]^M$
notes: error: the following arguments are required: title^M$
usage: notes edit <id> [--title TITLE] [--body BODY]^M$
notes: error: the following arguments are required: id^M$
```

## Cleanup

- Deleted from the scratchpad: `baseline-src/` (including its `obj/`), `baseline-bin/`, `baseline-out/`,
  `after-out/` and `capture.sh`. The scratchpad also holds a `commit-msg.txt` that I did not create, so I left
  it alone.
- Repo: `git status --short --untracked-files=all` shows only the 4 modified files above (this report will be
  the 5th). There is no `.notes.json` and no stray file.

## Observations for the leader (out of scope, not fixed)

1. **`Cli.cs:265`, `Command.Signature`, uses `new[] { Name }`.** That is an array creation expression, not a
   collection expression (`docs/conventions.md:18`). It is the same in `8e30cd7`. Feature 9's acceptance
   names only `GeneralUsage()` for collection expressions, so I left it.
2. **Two test files have lines over 120 columns, both `/// <summary>` lines:**
   `tests/NotesCli.Tests/ProgramEndToEndTests.cs:5` (121 cols) and `tests/NotesCli.Tests/TempDirectory.cs:3`
   (125 cols). Feature 9's acceptance covers `Cli.cs` only.
3. **Test gaps (pre-existing).** No in-process test covers the `ParseArguments` missing-positional message.
   No unit test pins the exact trailing newline of `GeneralUsage()` or the blank line before `notes: error:`,
   because `CliTests.Lines()` normalizes line endings and trims trailing newlines. This feature covers both
   with the out-of-process byte comparison above. A future hardening feature could pin them in-process.

## Output of the last `init.ps1`

`powershell -NoProfile -ExecutionPolicy Bypass -File .\init.ps1` (exit code 0)

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
[OK]    feature_list.json is valid (9 features, 0 pending)
[WARN]  Feature 9 'cli_conventions_cleanup' is in_progress: resume it (see progress/current.md)

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

Passed!  - Failed:     0, Passed:    47, Skipped:     0, Total:    47, Duration: 501 ms - NotesCli.Tests.dll (net10.0)
[OK]    Build is clean (warnings are errors) and all tests pass

-- 6. Summary ------------------------------------------
[OK]    Environment ready. You can start working.
```

The `[WARN]` is expected: feature 9 stays `in_progress` until the reviewer approves it.
