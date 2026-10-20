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

---

# 🤖 Chief Complaint v2 – AI Infrastructure & Architecture Report

As we spin up the second iteration of *Chief Complaint*, we’ve completely overhauled the development workflow. Instead of using a generic AI assistant, we've customized the Antigravity IDE with a suite of highly specialized, domain-specific AI "Personas," strict global rules, and automated QA workflows.

This ensures that as the game scales, the codebase remains modular, the economy remains balanced, and the UI stays decoupled from the core logic. Here is a breakdown of our automated development team:

## 1. The Core Development Team (AI Personas)

We interact with these personas by typing their slash commands (e.g., `/cc-card-engine-specialist`) into the IDE. Each persona is strictly scoped to prevent them from stepping on each other's toes or writing spaghetti code.

- **The Architect (`cc-card-engine-specialist`)**
  - **Role:** The backend mastermind. Owns the core `MonoBehaviour` managers (GameManager, DeckManager, BoardManager) and the `ScriptableObject` C# schemas.
  - **Mandate:** Strict adherence to SOLID principles. It is forbidden from writing UI or audio code. It manages the shift loop and broadcasts events when state changes occur.
- **The Data & Economy Balancer (`cc-medical-data-scraper`)**
  - **Role:** The clinical expert. Produces card and patient data (reviewed as CSV/JSON before it is applied to assets) and handles the mathematical Action Point economy.
  - **Mandate:** Ensures medical authenticity (e.g., proper clinical tags so a broken femur doesn't draw a cardiac symptom). It dictates the data content, but relies on the Architect to build the C# fields for it.
- **The Frontend Engineer (`cc-ui-ux-specialist`)**
  - **Role:** Owns the Canvas, layout structures, and visual "juice" (DOTween animations, card hover states).
  - **Mandate:** Acts as a pure consumer. It builds "dumb views" that listen to the Architect's backend events to trigger animations, ensuring strict Model-View-Controller separation.
- **The Juice Engineer (`cc-audio-specialist`)**
  - **Role:** Manages the Unity `AudioMixer`, sound effect timing, and Particle Systems (VFX).
  - **Mandate:** Wires up the audio feedback (e.g., a crisp paper slide when drawing a card) to sync perfectly with the UI animations without desyncing the core game loop.
- **The QA Tester (`cc-unity-qa-tester`)**
  - **Role:** The safety net. Writes deterministic NUnit tests (EditMode & PlayMode) for the core backend.
  - **Mandate:** Validates state changes, economy edge cases, and boundary limits. It does not test UI or Audio, focusing entirely on engine correctness.

## 2. Immutable Project Rules

To prevent the AI (or us) from accidentally breaking the physical board layout, we implemented a global rule file (`ui-asset-standards.md`) that the IDE enforces on every interaction:

- **Strict Sizing:** Patient/Action cards and Board Slots are mathematically locked to exactly **3.2 x 4.2 world units**.
- **Resolution & PPU:** Standard Definition is 320x420 (100 PPU); High Definition is 640x840 (200 PPU).
- **No Scaling Hacks:** Object scale must always remain `(1, 1, 1)`. The physical bounds of sprites are controlled strictly via pixel resolution, PPU settings, and 9-slicing.

## 3. Automated Code Review Workflows

Instead of reviewing the entire Unity project at once (which overwhelms the AI context window), we built targeted slash commands that act as our automated Lead Developer:

- **`/audit-managers`:** Scans the core logic for memory leaks, tight coupling, and event-driven architecture health.
- **`/audit-ui`:** Scans the UI layer to ensure no frontend scripts are accidentally modifying backend game state (enforcing the MVC pattern).
- **`/audit-data`:** Scans the ScriptableObjects to ensure they contain *only* static configuration and no mutable runtime state (preventing asset corruption during playtests).
- **`/project-health-report`:** An exhaustive, overnight-capable sweep of the entire project. It generates a formatted report listing "What is solid," "P0 Bugs (Fix First)," and "P1 Architectural Debt."

*With this infrastructure, we can rapidly scaffold new cards, trust the test suite, and ensure the UI and backend never tangle. Let's build a great hospital deck-builder!*
