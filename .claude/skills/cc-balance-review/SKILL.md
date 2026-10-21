---
name: cc-balance-review
description: "Chief Complaint balance review workflow. Use when the user asks whether the game mechanics, cards, economy or difficulty curve are balanced, or after adding or changing cards, patients, effects or economy numbers. Coordinates the cc-medical-data-scraper, cc-card-engine-specialist and cc-unity-qa-tester personas and produces one prioritised report. Read-only: it reports findings and changes nothing."
disable-model-invocation: true
---

# Balance Review Workflow

Runs a read-only balance audit by putting three specialist questions to the project's personas, then merges their answers into one report. **Do not edit any asset, script or test during this workflow.** Fixes are proposed in the report and applied only after the user approves them.

Use **Australian English spelling** throughout.

## Scope

If the user named an area (for example "Red Tape cards" or "Trauma Flight patients"), restrict every step to it. Otherwise review everything under `Assets/Data/` and the economy code in `Assets/Scripts/Core/`, `Board/` and `Cards/`.

## Step 0: Baseline (do this yourself, first)

1. Load `unity:unity-cli` for the current command syntax. Run `unity status`. If the Editor is "ready", prefer `unity command` for reading assets (`asset find`, `component get_fields`). If no Editor is reachable, read the `.asset` YAML directly and say so in the report.
2. Run the existing EditMode tests (`unity command test run --mode EditMode`). Record the totals. Any failing test is reported first, because it may mean a balance assumption is already broken.
3. Note the current branch and `git log --oneline -5` so the report states what version was reviewed.

## Step 1: Fan out to the personas (in parallel, read-only)

Spawn three subagents **in one message**, using the subagent type in the table. Tell each that this is a **read-only** audit: it may not edit, create or delete any project file, and any simulator output goes in the scratchpad. The rules agent is `general-purpose` and must first read the "You are the lead" and "Engineering rules" sections of `CLAUDE.md` and adopt that persona. Give each its question and the scope from above. Ask each to return findings in the format under "Finding format".

| Agent | Subagent type | Question |
|---|---|---|
| Economy and content | `cc-medical-data-scraper` | Are AP costs, `powerTier`, draft value (`ActionCardSO.CalculateDraftValue()`), reputation rewards, `startingStability` and `stabilityDecayPerTurn` coherent? Can cumulative stability decay outpace maximum AP output in early shifts? Is each escalation pool (WalkIn, Ambulance, TraumaFlight) the right difficulty for its triage range? Are there cards that are strictly better or worse than another at the same cost, duplicate names, or tier/cost outliers? Do patient `clinicalTags` and symptom `clinicalTags` pair logically? |
| Rules and systems | `general-purpose` (card-engine persona from `CLAUDE.md`) | Read the manager and effect code (`GameManager`, `DeckManager`, `BoardManager`, `PatientDeckManager`, `Cards/Effects/`). Do the rules create degenerate loops or dead ends: infinite draw or AP, free exhaust-free engines, Red Tape that cannot be cleansed, reputation that can go unrecoverable, a shift that cannot be won, cards whose `successChance` makes them never worth playing? Is anything un-winnable or trivially won because of how decay, draw and AP interact? |
| Test coverage | `cc-unity-qa-tester` | Which balance assumptions are already enforced by tests in `Assets/Tests/`, and which are not? Propose specific parameterised boundary tests (name, inputs, expected result) for the unprotected ones. Do not write them. |

Tell every agent: "Cite the asset path or `file:line` for every finding. Say 'not verified' where you could not check something. Do not speculate about numbers you did not read."

## Step 2: Cross-check (do this yourself)

- Use the Card Balance Viewer figures (`Chief Complaint/Windows/Card Balance Viewer`: average AP cost, total AP generation) as a sanity check against the economy agent's claims. If the Editor is connected, `unity command editor invoke_menu` can open it; otherwise note that it was not consulted.
- Discard or downgrade any finding that lacks a citation or contradicts the code.
- Where two agents disagree, read the source yourself and decide, stating which was right.

## Step 3: Report

Produce one report, in chat, with these sections in order:

1. **Verdict:** one or two sentences: balanced / mostly balanced / needs work, and why.
2. **Reviewed:** branch, commit, scope, test totals, whether the Editor was used.
3. **Findings:** a table ordered by severity (Blocker, Major, Minor). Columns: severity, area, finding, evidence (path or `file:line`), proposed fix, owner persona.
4. **Proposed tests:** from the QA agent, trimmed to the ones worth writing.
5. **Not verified:** anything that could not be checked and why.

If the user asks for the report to be kept, write it to `docs/balance-reviews/YYYY-MM-DD.md` (create the folder) rather than a scratch location.

End by asking which fixes, if any, to apply. When approved, route each to its owner: data and number changes to `cc-medical-data-scraper`, C# changes to the lead (main session), new tests to `cc-unity-qa-tester`.

## Finding format (give this to each agent)

```
- severity: Blocker | Major | Minor
  area: <economy | rules | coverage>
  finding: <one sentence>
  evidence: <asset path or file:line, plus the numbers read>
  fix: <specific proposed change>
```
