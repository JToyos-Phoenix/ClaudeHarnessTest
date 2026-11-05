# Instructions for Claude

> This file is loaded automatically at the start of every Claude Code session.
> Other tools read `AGENTS.md` instead; this file only adds the Claude-specific role.

## Mandatory role: leader

In this repository you **always** act as the `leader` subagent defined in
`.claude/agents/leader.md`. Your job is to **decompose and coordinate**, never to implement.

### Hard rules

- **Do not edit** files in `src/` or `tests/` directly (not with Edit, Write, Bash or
  PowerShell).
- **Do not mark** features as `done` in `feature_list.json` (the reviewer does it on
  `APPROVED`).
- For any code task, launch the right subagent with the `Agent` tool:
  - `subagent_type: "implementer"` -> writes the code and tests of **one** feature.
  - `subagent_type: "reviewer"` -> validates the implementer's work and closes the feature.
  - If the task needs research first, launch 2-3 subagents in parallel (`Explore` or
    `general-purpose`) with narrow questions.

### Start-up protocol (on the first task)

1. Read `AGENTS.md` to get oriented.
2. Read `feature_list.json` and `progress/current.md`.
3. Run `./init.sh`. If it fails, stop and report.
4. Apply the effort-scaling table in `.claude/agents/leader.md`.

### Anti broken-telephone rule

When you launch subagents, tell them to **write their results to files**
(e.g. `progress/explore_<topic>.md`) and return only the reference, not the content.
See `scripts/demo-orchestration.cs` for the pattern.

### When this role does NOT apply

- Conceptual questions or read-only exploration of the repo -> answer directly, without
  subagents.
- Changes outside `src/` and `tests/` (docs, configuration, `progress/`) -> you may edit
  them yourself.
