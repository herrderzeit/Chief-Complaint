# BalanceSim: headless balance simulator

A plain .NET 8 console app that plays thousands of Chief Complaint runs with bots and reports win rate,
Reputation and Funding curves, death and cure rates per patient pool, and which purchases dominate.

It reads the game's real content straight from the Unity project on disk (the ScriptableObject assets under
`Assets/Data` and the manager settings serialized in `Assets/Scenes/MainBoard.unity`), so a run always uses
whatever the game currently ships with. No Unity install is needed. It lives outside `Assets/`, so Unity never
compiles it.

## Running it

```sh
# full report: every bot plus the single-purchase experiment -> Tools/BalanceSim/out/
dotnet run --project Tools/BalanceSim -c Release -- report --runs 5000

# one bot, summary to the console
dotnet run --project Tools/BalanceSim -c Release -- run --bot smart --runs 2000

# play-by-play of a single seeded run (good for checking a rule by eye)
dotnet run --project Tools/BalanceSim -c Release -- trace --bot smart --seed 3

# what the loader read from the project
dotnet run --project Tools/BalanceSim -c Release -- data

# try a tuning change without touching the scene
dotnet run --project Tools/BalanceSim -c Release -- report --set ambulanceCureThreshold=2 --set deathReputationPenalty=7

# tests (engine rules + loading the real project)
dotnet test Tools/BalanceSim.Tests
```

`report` writes `report.md` (tables), `curves.csv` (per-shift curves, one row per bot and shift) and
`summary.json` (everything). Run `i` uses seed `--seed + i`, so results are reproducible. 5,000 runs per bot
take a few seconds. Any field of `src/Data/GameConfig.cs` can be overridden with `--set name=value`.

## Bots

| Bot | Plays | Buys |
|---|---|---|
| `random` | random legal actions (ends the shift with 10% chance per step) | random |
| `nobuy` | greedy heuristic | nothing |
| `randombuy` | greedy heuristic | random affordable items |
| `smart` | greedy heuristic | a sensible shopping list (AP engine, diagnosis tier, treatment tier when the pool needs it, Rep when low) |
| `stall` | `smart`, but stops completing cures after 2 so the run stays on Walk-Ins | as `smart` |

The greedy player scores every legal action (save a dying patient, admit, treat, diagnose, AP and draw cards
first, trigger Team Assessment, stabilise, clear Red Tape with spare AP) and takes the best until nothing is
worth doing. The report also runs a **single-focus experiment**: the greedy player buys only one market card,
or only one upgrade chain, and nothing else. Comparing those win rates with the `nobuy` baseline shows which
purchase carries a run on its own.

## How it maps to the game code

`src/Engine/Game.cs` is the rules engine. Each section names the Unity class it mirrors:
`GameManager` (AP, Reputation, Funding, shift loop, win/lose), `BoardManager` and `PatientCardView`
(spawn, decay, diagnose, treat, surgery, discharge, triggers), `PatientDeckManager` (patient draw, symptom
generation, escalation), `DeckManager` (piles, Red Tape, cleanse), `DraggableCard` (card play rules),
`DepartmentFallbackButton` (paging), `StaffSynergyManager` (Team Assessment), `StaffRoomManager` and
`DeckUpgradeManager` (market and upgrades), and the `*EffectSO` classes (card effects).

When a rule changes in the Unity code, change it here too; the tests in `Tools/BalanceSim.Tests` pin the
behaviours that matter for balance. A new effect script needs a case in `ProjectLoader.LoadEffect` and in
`Game.CanApply` / `WouldSucceed` / `ApplyEffect`; until then the loader warns and the effect never applies
(and `Every_card_effect_is_understood` fails).

Simplifications, none of which change what a player can achieve:

- The scene's four department slots are modelled as one per specialty.
- A patient is moved into a department only to be treated straight away (no parking a patient there), and a
  Ward patient is never sent back to Triage.
- UI-only states (drag hover, tutorial, Escape menu) are not modelled; "choose one" prompts and the
  Team Assessment pick are decisions the bot makes.
