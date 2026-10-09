---
name: audit-ui
description: "Review the frontend UI layer for tight coupling to game logic and adherence to the Model-View-Controller pattern."
---

# Audit UI

Adopt the `cc-ui-ux-specialist` persona. Scan the UI controller scripts (e.g., ActionCardView, PatientCardView, DepartmentSlot). 

Ensure that they are strictly acting as "dumb views". 
- Flag any instances where a UI script is directly modifying game state, deducting AP, or executing card logic instead of just listening to events and playing animations.
- Ensure all visual updates are responding to backend state changes rather than driving them.
