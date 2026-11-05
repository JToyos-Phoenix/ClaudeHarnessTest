# harness-csharp: Notes CLI

A C# (.NET 10) port of [betta-tech/ejemplo-harness-subagentes](https://github.com/betta-tech/ejemplo-harness-subagentes),
the example project that demonstrates **Harness Engineering** principles on top of a
minimal notes CLI.

> The application code is deliberately simple. What matters in this repo is not **what**
> it does, but **how** it is structured so an AI agent can work on it autonomously and
> verifiably.

**New to the idea?** Read [guide/01-harness-engineering-guide.md](guide/01-harness-engineering-guide.md).
**Want to know how the Python version became this one?** Read
[guide/02-python-to-csharp-conversion-plan.md](guide/02-python-to-csharp-conversion-plan.md).

## How the harness is organized

| Pillar | Where it lives in this repo |
|--------|-----------------------------|
| **1. The repository IS the system** | `AGENTS.md`, `CLAUDE.md`, `init.ps1`, `feature_list.json`, `progress/`, `docs/` |
| **2. Multi-agent orchestration**    | `.claude/agents/leader.md`, `implementer.md`, `reviewer.md`, `scripts/demo-orchestration.cs` |
| **3. Verification and improvement** | `CHECKPOINTS.md`, `tests/`, hooks in `scripts/hooks/` (+ `.claude/settings.json`) |

## Getting started

Requirements: .NET SDK 10+, Windows PowerShell 5.1 or PowerShell 7+.

```bash
./init.sh
```

or, from PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\init.ps1
```

If everything is green, open `AGENTS.md` and follow it from there.

## Using the app (humans)

```bash
dotnet run --project src/NotesCli -- add "buy bread" --body "and milk"
dotnet run --project src/NotesCli -- list
dotnet run --project src/NotesCli -- show 1
dotnet run --project src/NotesCli -- search bread
dotnet run --project src/NotesCli -- recent --limit 3
dotnet run --project src/NotesCli -- edit 1 --title "buy bread and eggs"
dotnet run --project src/NotesCli -- delete 1
```

Notes are stored in `.notes.json` in the current folder, or wherever `NOTES_FILE` points.

## Try it yourself with Claude Code

If you open Claude Code at the repo root, you are already inside the harness:
`CLAUDE.md` forces the model to act as `leader` (it orchestrates, it does not edit code).

1. `./init.sh` must end green.
2. Open `feature_list.json` and leave at least one feature with `status: "pending"`.
   All current features (1-8) are `done`, so add a new one first.
3. Start Claude Code at the repo root: `claude`.
4. Ask it literally: **"implement the next pending feature"**.

What you will see in the chat:

- The **leader** announces the plan, launches an `implementer` and then a `reviewer`.
- **No code goes through the chat**, only references like
  `done -> progress/impl_cli_recent.md`. That is the anti broken-telephone rule.

Where each subagent leaves its trace (the persistent "visualization"):

| File                             | Written by  | Contains                                            |
|----------------------------------|-------------|-----------------------------------------------------|
| `progress/current.md`            | implementer / leader | Live plan of the session                   |
| `progress/impl_<feature>.md`     | implementer | Files touched + output of the tests                 |
| `progress/review_<feature>.md`   | reviewer    | Checklist against `docs/` and `CHECKPOINTS.md`      |
| `feature_list.json`              | implementer -> reviewer | `pending` -> `in_progress` (implementer) -> `done` (reviewer) |
| `progress/history.md`            | leader      | Append-only summary when the session closes         |

Open `progress/` in your editor while Claude works: each report appears as soon as the
subagent finishes.

### Hooks (active)

`.claude/settings.json` wires two hooks: tests run after every C# edit, and `init.ps1`
runs before Claude ends a turn. It also pre-approves `./init.sh` and
`dotnet build/test/run`. Claude Code loads it when a session starts, so open a new
session after changing it. `scripts/hooks/settings.template.json` is a reference copy;
to turn the hooks off, delete `.claude/settings.json` or set `"disableAllHooks": true`
in `.claude/settings.local.json`.

## Structure

```
.
├── AGENTS.md                # Map for agents (progressive disclosure)
├── CLAUDE.md                # Claude Code only: "you are the leader"
├── CHECKPOINTS.md           # Criteria for a "correct final state"
├── feature_list.json        # Scope: one feature at a time
├── init.ps1                 # Verification and initialization (the gate)
├── init.sh                  # Wrapper: runs init.ps1 from Bash
├── NotesCli.slnx            # Solution
├── Directory.Build.props    # net10.0, nullable, warnings as errors
├── progress/
│   ├── current.md           # Active session (live state)
│   ├── history.md           # Append-only log
│   └── explore_*.md         # Worker reports (from demo-orchestration.cs)
├── docs/
│   ├── architecture.md      # What "good work" means
│   ├── conventions.md       # Style, naming, errors
│   └── verification.md      # How to prove it works
├── .claude/
│   ├── settings.json        # Hooks + pre-approved dotnet commands
│   └── agents/              # leader, implementer, reviewer
├── scripts/
│   ├── demo-orchestration.cs   # Leader/Worker demo (dotnet run scripts/demo-orchestration.cs)
│   └── hooks/                  # post-edit.ps1, on-stop.ps1, settings.template.json (reference copy)
├── src/NotesCli/
│   ├── NoteStorage.cs       # Atomic JSON persistence
│   ├── Note.cs              # Domain model (record) + domain exceptions
│   ├── Cli.cs               # Argument parsing + commands
│   └── Program.cs           # Entry point (wires Console to Cli.Run)
├── tests/NotesCli.Tests/    # xUnit tests (47)
└── guide/                   # Learning material for humans (agents skip it)
```

## Lessons this project illustrates

- **Progressive disclosure** in `AGENTS.md`: the agent does not get every rule at once;
  it gets a map to look them up on demand.
- **One feature at a time**, enforced by `init.ps1` (rejects more than one `in_progress`).
- **State on disk, not in chat**: `progress/current.md` and `history.md` survive restarts
  and blown context windows.
- **Executable verification**: `init.ps1` runs the real build and tests, and checks the
  architecture rules; it does not trust what the agent says.
- **Leader-Worker-Reviewer pattern**: the leader does not implement, the implementer
  does not self-approve, the reviewer does not edit code.
- **Anti broken-telephone**: subagents write their results to files and only return a
  lightweight reference.
