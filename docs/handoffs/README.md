# Hand-offs

Shared notes for multi-role work. Subagents start with no memory of the conversation, so this is how one role passes context and results to the next.

## Naming

One file per task: `docs/handoffs/<yyyy-mm-dd>-<short-task-name>.md`, for example `2026-10-21-defibrillator-card.md`.

## Template

```markdown
# <Task name>

## Plan (lead: cc-card-engine-specialist)
- Goal:
- Decisions already made:
- Steps and owners:
  1. <role>: <what> (files: ...)
  2. ...
- Done when:

## cc-<role>
- Changed: <paths>
- Checked: <tests run, compile, play mode>
- Needs from others: <who, what>
- Not verified: <...>
```

Each role appends its own section; nobody rewrites another role's section. The lead adds a final **Result** section after checking everything in the Editor.

Hand-off files are committed with the work they describe, so the history explains why a change was made. Delete old ones when they stop being useful.
