---
name: cc-new-feature
description: "Chief Complaint team workflow for building a new mechanic or system (for example a synergy, a new patient trigger, a new screen or a shift rule) across roles: design and plan, core C# first, then content, UI, audio and tests in parallel, then integration, balance and an in-Editor check. Use when the user asks to add or build a feature or mechanic. The main session leads as cc-card-engine-specialist and delegates to the cc- subagents."
argument-hint: "<feature description>"
disable-model-invocation: true
---

# New Feature Workflow

You are the lead (cc-card-engine-specialist, see `CLAUDE.md`). Build one feature across the team. Use Australian English. Keep the task list updated with steps named by what they do, not by who does them.

**One mechanic at a time.** A large multi-mechanic update was shelved earlier because it was clunky and buggy. If the request bundles several mechanics, propose building the smallest playable one first and stop there for a playtest.

## Step 0: Design (you, with the user)

1. Read the code the feature touches (managers, views, effects, the shift flow) and Grep for existing events and helpers to reuse.
2. Write a short design: the rule in one or two sentences, how the player triggers and sees it, the numbers, edge cases (empty board, Game Over, draft phase, Escape), and how it interacts with existing mechanics (Team Assessment, surgeries, Red Tape, escalation).
3. Ask the user (AskUserQuestion) about the design choices that are genuinely theirs, such as the rule, numbers or feel. Recommend an option.
4. Present the plan (the steps below, with owners and files) and **wait for the user's go-ahead** before writing code. A feature changes several systems at once, so it needs their OK first.

## Step 1: Hand-off (you)

Create `docs/handoffs/<yyyy-mm-dd>-<feature>.md` from `docs/handoffs/README.md`, recording the design, decisions and steps with owners and files. Check that no two parallel steps touch the same files.

## Step 2: Core first (you)

Make the schema, events and logic changes yourself: new SO fields or enum values (append enum values; never reorder them), `System.Action` events for UI and audio to hook, manager logic and any new `CardEffectSO`. Keep logic testable (pure helpers, injectable randomness, state getters). Recompile and check `console get`. Mirror rule changes in `Tools/BalanceSim`. Note the new fields, events and APIs in the hand-off, since the other roles build on them.

## Step 3: Fan out (in parallel, one message)

In each brief, name the `unity:` skills the role should load (see the table in `CLAUDE.md`), e.g. `unity:ui-ugui` for new screens or `unity:unity-cli` for tests.

Brief each role that the feature needs; skip the others and say so. Each brief has the hand-off path, the APIs and events from step 2, the files it owns for this task, and what done looks like.

- **cc-medical-data-scraper:** asset content and numbers (new or changed cards, patients, symptoms, tags), through `unity command`.
- **cc-ui-ux-specialist:** what the player sees: prompts, highlights, HUD, popups (`GameFeedback`), tooltips. It subscribes to events and never drives game state.
- **cc-audio-specialist:** sounds or VFX on the new events, only if the feature has a moment worth marking. If it needs new audio, add the cue to `docs/audio/sound-brief.md` and give the user Suno prompts with `/cc-suno-music` (loops), `/cc-suno-sounds` (SFX, stings) or `/cc-suno-speech` (voice lines). The files are imported later with `import`, so the feature must work silently without them.
- **cc-unity-qa-tester:** EditMode tests for the rule and its edge cases, PlayMode tests where scene state matters, and a simulator test if the rule was mirrored. Run them and report totals.

If a role reports it needs something from another role, such as a new event or field, do that yourself, then resume that role with SendMessage.

## Step 4: Integrate and check (you)

1. Read every diff. Look for overlapping edits, hand-edited scene or prefab YAML (not allowed while the Editor is connected) and missing `.meta` files.
2. Recompile, run all EditMode tests, and run PlayMode tests if any were added.
3. Balance: if the feature touches AP, Reputation, Funding, stability or escalation, run `dotnet run --project Tools/BalanceSim -c Release -- report --runs 2000` before and after, and report the change in win rates. For a big shift, suggest `/cc-balance-review`.
4. Play-mode check: drive the feature with `unity command script eval` (as in earlier play-mode checks) on the main path and at least one edge case. Capture the game view, then exit play mode.
5. Ask the user before saving the scene if it may hold their own unsaved edits.
6. Add a **Result** section to the hand-off.

## Step 5: Report

Tell the user: what the feature does now, the files changed by role, test totals, balance numbers, screenshots, anything not verified, and suggested follow-ups (kept small, one mechanic at a time). Do not commit unless asked.
