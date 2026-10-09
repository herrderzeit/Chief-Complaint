# 🏥 Chief Complaint (v2) - Codebase & Architecture Overview

Hey! Thanks for taking the time to review this. As you know, this is the second iteration of the project, rebuilt entirely from the ground up. In v1, the project ended up being scraped but was a very good learning experience. For this v2 rebuild, my primary goal was establishing a clean, scalable architecture using SOLID principles, strict Assembly Definitions, and a decoupled Data-to-View pipeline.

Here is a quick primer on how the game works and how the codebase is structured to support it.

## 🎮 The Core Game Loop

Chief Complaint is a hospital-themed roguelike deck-builder.

- **The Board:** Patients arrive in the Triage river. Every patient has a Stability stat (which decays at the end of every turn/shift) and 1-3 Symptoms attached to them.
- **The Economy:** The player has a base economy of 3 AP (Action Points) per shift.
- **The Hand:** The player draws a hand of Action Cards (Treatments, Diagnostics, Staff, or unplayable "Red Tape" curses).
- **The Goal:** The player spends AP to play Action cards, synergize tags (like `[Surgical]` or `[Emergency]`), treat the symptoms, and restore patient Stability. Curing patients earns Reputation and progresses the run's "Escalation Phase" (Walk-Ins → Ambulances → Trauma Flights). If a patient's Stability hits 0, it's a Code Blue.

## 🏗️ Architecture & What to Expect

The project is built in Unity 6 using C#. Here are the core patterns and systems you'll encounter:

### 1. Data-Driven Design (ScriptableObjects)
All static game data is decoupled from MonoBehaviour logic. I heavily utilize ScriptableObjects as the immutable source of truth.
- **PatientCardSO:** Defines vitals, triage level, and which specialty decks to draw symptoms from. (Note: We use `OnValidate` to strictly cap symptoms at 3 so it doesn't break the AP economy).
- **SymptomCardSO:** Defines 3 escalating severity tiers (Minor → Acute → Critical) for a single illness.
- **ActionCardSO:** Defines AP costs, synergy tags, and uses a Strategy Pattern for triggering card effects.

### 2. Decoupled Views (MVC-ish Pattern)
I've made a strict rule to separate state from presentation.
- **Logic:** `GameManager` handles the AP/Shift loop. `BoardManager` manages the physical slots and patient decay. `PatientDeckManager` handles symptom generation and difficulty escalation.
- **Views:** Scripts like `PatientCardView` and `SymptomSubCardView` simply take a ScriptableObject payload and render the UI.
- **Interactions:** User input is abstracted into isolated scripts like `DraggableCard` and `DraggablePatient`.

### 3. Modern Input & Assembly Definitions
- **Input:** We have fully migrated off the legacy `UnityEngine.Input` class to the modern event-driven Unity Input System.
- **AsmDefs:** The codebase is partitioned using Assembly Definitions (e.g., `GameScripts.asmdef`, `EditorScripts.asmdef`, `PlayModeTests.asmdef`). This prevents editor scripts from leaking into runtime code and keeps compilation times lightning fast.

## 👀 Where to start looking:

If you want to follow the data flow, I recommend starting at `PatientDeckManager.cs` to see how patients and symptoms are procedurally generated, then look at `BoardManager.cs` for the shift decay loop, and finally check `ActionCardSO.cs` to see how the player's draft valuation and AP costs are structured.

Let me know if you have any questions on the structural flow!
