---
name: audit-data
description: "Review ScriptableObject definitions to ensure they contain only static data and no mutable runtime state."
---

# Audit Data

Scan the ScriptableObject definition scripts (e.g., PatientCardSO, ActionCardSO, SymptomCardSO). 

Ensure that these classes contain ONLY static data and configuration. 
- Flag any fields that track runtime state (like current health, active cooldowns, or current AP cost) which would accidentally persist across play sessions or overwrite the asset on disk during PlayMode.
