# CHECKPOINTS: evaluating the final state

> In multi-agent systems you do not evaluate the path, you evaluate the destination.
> These are the objective checkpoints a judge (human or AI) can use to decide whether
> the project is healthy. Items marked *(init)* are checked automatically by `init.ps1`.

## C1: The harness is complete

- [ ] The 4 base files exist: `AGENTS.md`, `init.ps1`, `feature_list.json`,
      `progress/current.md`. *(init)*
- [ ] The 3 docs exist: `docs/architecture.md`, `docs/conventions.md`,
      `docs/verification.md`. *(init)*
- [ ] `./init.sh` ends with exit code 0.

## C2: The state is coherent

- [ ] At most one feature `in_progress` in `feature_list.json`. *(init)*
- [ ] Every `done` feature has associated tests that pass.
- [ ] `progress/current.md` is the empty template or describes the active session
      (no leftovers from previous sessions).

## C3: The code respects the architecture

- [ ] `src/NotesCli/` only contains the files listed in `docs/architecture.md`. *(init)*
- [ ] `src/NotesCli/NotesCli.csproj` has no `<PackageReference>`. *(init)*
- [ ] No `Console.*` outside `Program.cs`, no debug output, no TODOs without context. *(init)*

## C4: Verification is real

- [ ] `tests/NotesCli.Tests/` has at least one test class per file in `src/NotesCli/`
      (`Program.cs` is covered by `ProgramEndToEndTests`).
- [ ] Tests use `TempDirectory` (a real temporary folder), never filesystem mocks.
- [ ] `dotnet test NotesCli.slnx` runs > 0 tests and all are green. *(init)*

## C5: The session was closed properly

- [ ] No suspicious untracked files (`*.tmp`, `.notes_*.json`, `bin/` or `obj/` outside
      `.gitignore`).
- [ ] `progress/history.md` has an entry for the last session.
- [ ] The last feature worked on is in its correct state in `feature_list.json`.

---

**How to use this file:** the reviewer agent (`.claude/agents/reviewer.md`) walks every
checkbox, marks `[x]` or `[ ]`, and rejects the session close if any box in C1-C5 is empty.
