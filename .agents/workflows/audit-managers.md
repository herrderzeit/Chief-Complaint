---
name: audit-managers
description: "Review all core manager scripts for SOLID principles, event-driven architecture, and memory leaks."
---

# Audit Managers

Please use your search and file-reading tools to scan the core manager scripts in this project (e.g., GameManager, DeckManager, BoardManager). 

Audit them for the following:
1. **Coupling:** Are they tightly coupled, or are they using `System.Action` events correctly?
2. **Performance:** Are there any GC allocation leaks in the main update or shift loops?
3. **Data Integrity:** Is any mutable runtime state accidentally leaking into the ScriptableObject data layer? 

Provide a prioritized list of refactoring suggestions based on your findings.
