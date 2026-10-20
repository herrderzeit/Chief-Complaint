---
name: cc-new-card
description: "Chief Complaint team workflow for adding one new action card end to end: design check, any new effect code, the card asset, tests, draft-pool wiring and an in-Editor check. Use when the user asks to add, design or create a new action card. The main session leads as cc-card-engine-specialist and delegates to the cc- subagents."
argument-hint: "<card idea, e.g. 'Defibrillator: 2 AP Treatment that restores 6 Stability to a Code Blue patient'>"
disable-model-invocation: true
---

# New Card Workflow

You are the lead (cc-card-engine-specialist, see `CLAUDE.md`). Take one action card from idea to a tested, playable card in the draft market. Use Australian English. Keep the task list updated with steps named by what they do (for example "Create the card asset"), not by who does them.

## Step 0: Pin down the card (you)

From the user's idea, settle: name, `cardType`, `powerTier`, `apCost`, effect(s), target (play zone or patient), exhaust rules, rules text intent, and whether it is a draft card, an upgrade target or Red Tape.

- Grep `Assets/Data/Actions/` for the name and for cards that already do the same thing. If one is strictly better or worse at the same cost, tell the user before going on.
- Ask the user (AskUserQuestion) only about choices that change the design, such as cost, tier or target. Fill in the rest with sensible defaults and state them.
- Price check: the market cost comes from `ActionCardSO.CalculateDraftValue()`. Work it out and say what the card will cost.

## Step 1: Plan and hand-off (you)

Create `docs/handoffs/<yyyy-mm-dd>-<card-name>.md` from the template in `docs/handoffs/README.md`. Record the decisions from step 0 and the steps below with their owners.

## Step 2: Effect code, only if needed (you)

List `Assets/Scripts/Cards/Effects/` and check whether existing `CardEffectSO` subclasses (alone, or combined through `ChoiceEffectSO` / `EitherOrEffectSO` / `WardWideEffectSO`) can express the card. If not, write a new `CardEffectSO` subclass following the existing ones (strategy pattern, no manager branches), recompile with `unity command recompile`, and check `console` for errors. Mirror any new rule in `Tools/BalanceSim` so the simulator can play the card.

## Step 3: Card asset (cc-medical-data-scraper)

Delegate with the Agent tool (`subagent_type: cc-medical-data-scraper`). The brief must include the hand-off path, the settled design, the effect class names, the target folder (`Assets/Data/Actions/Draft/`, `Upgrade/` or `RedTape/`), and these requirements:
- Create the effect SO asset(s) and the `ActionCardSO` asset through `unity command` while the Editor is connected.
- Write rules text that states exactly what the effect does, plus clinically sensible flavour text using globally understood terms.
- Set `draftUpgradeCost` only if the card has a `directDraftUpgrade`.
- Append the asset paths and field values to the hand-off.

## Step 4: In parallel (one message, files don't overlap)

- **cc-unity-qa-tester:** EditMode tests for any new effect (applies, refuses illegal targets, edge cases), plus a data test that the new asset loads, has rules text and costs what `CalculateDraftValue()` says. Add a simulator test if a new rule was mirrored. Run them and report totals.
- **cc-ui-ux-specialist:** only if the card needs something new on screen (a new type icon, a new target highlight, a tooltip). Otherwise skip it and say so.

## Step 5: Wire up and check (you)

1. Add the card to `DraftManager.draftableCardsPool` in `MainBoard.unity` through `unity command` (or to its base card's `directDraftUpgrade` if it is an upgrade). Ask the user before saving the scene if it might hold their own unsaved edits.
2. Read every diff the subagents made. Recompile, run the EditMode tests, and fix or send back anything wrong.
3. Run the simulator: `dotnet run --project Tools/BalanceSim -c Release -- report --runs 2000` and compare the smart, random and nobuy win rates with the previous report. Note how often the new card is bought.
4. Play-mode check: enter play mode, put the card in hand, play it on a legal and an illegal target, and capture the game view. Exit play mode.
5. Add a **Result** section to the hand-off.

## Step 6: Report

Tell the user: what the card does and costs, the files added or changed, test totals, the simulator win-rate change, and the screenshot. Do not commit unless asked.
