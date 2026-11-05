# AGENTS.md: navigation map for AI agents

> This file is the **entry point** for any agent working in this repository
> (Claude Code, Copilot, Codex, Cursor...). It is NOT a rulebook: it is a **map**.
> Read only what you need, when you need it (progressive disclosure).

---

## 1. Before you start (mandatory)

1. Run `./init.sh` (or `powershell -NoProfile -ExecutionPolicy Bypass -File .\init.ps1`)
   and check that it ends without errors. If it fails, **stop** and fix the environment
   before touching code.
2. Read `progress/current.md` to see the state the last session left behind.
3. Read `feature_list.json` and pick **one** feature with status `pending`. Never work
   on more than one at a time.

## 2. Repository map

| File / folder                    | What it contains                                            | When to read it |
|----------------------------------|-------------------------------------------------------------|-----------------|
| `feature_list.json`              | Feature list with status (pending / in_progress / done / blocked) | Always, at the start |
| `progress/current.md`            | State of the current session                                | Always, at the start |
| `progress/history.md`            | Append-only log of previous sessions                        | If you need history |
| `docs/architecture.md`           | What "good work" means in this project                      | Before implementing |
| `docs/conventions.md`            | Style, naming, structure rules                              | Before writing code |
| `docs/verification.md`           | How to prove your work works                                | Before declaring a feature `done` |
| `CHECKPOINTS.md`                 | Objective criteria for a "correct final state"              | To self-evaluate |
| `.claude/agents/`                | Subagent definitions (leader, implementer, reviewer)        | If you orchestrate work |
| `scripts/demo-orchestration.cs`  | Demo of the Leader-Worker pattern writing results to disk   | To understand the anti broken-telephone rule |
| `scripts/hooks/`                 | Scripts the harness runs automatically (hooks)              | Rarely |
| `src/NotesCli/`                  | Application code                                            | To implement |
| `tests/NotesCli.Tests/`          | Automated tests (xUnit)                                     | To verify |
| `guide/`                         | Learning material for humans                                | **Do not read**: not needed for any task |

## 3. Hard rules (non-negotiable)

- **One feature at a time.** Do not mix changes from several features in one session.
- **Never declare a feature `done` without green tests.** Run `./init.sh` and make sure
  the test block passes at 100%.
- **Document what you do** in `progress/current.md` while you work, not at the end.
- **Leave the repository clean** before closing the session (see section 5).
- **If you do not know something, search `docs/`** before inventing it.

## 4. How to pick a feature

```
1. Open feature_list.json
2. Filter by status == "pending"
3. Take the one with the lowest "id"
4. Change its status to "in_progress" and save
5. Write in progress/current.md: feature, start time, short plan
```

## 5. Closing the session (lifecycle)

Before finishing:

1. Run `./init.sh`: all green.
2. The feature is marked `done` only after an `APPROVED` review
   (`progress/review_<feature>.md`).
3. Append the summary of `progress/current.md` to the end of `progress/history.md`.
4. Reset `progress/current.md` to the template.
5. No temporary files, no debug `Console.WriteLine`, no TODOs without context.

## 6. If you get stuck

- Re-read the relevant section of `docs/`.
- If a tool does not do what you expect, **do not invent a workaround**: document the
  blocker in `progress/current.md`, set the feature to `blocked`, and stop the session.
