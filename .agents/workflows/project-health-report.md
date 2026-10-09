---
name: project-health-report
description: "Perform a comprehensive health check of the entire project's architecture, performance, and test coverage."
---

# Project Health Report

I want a comprehensive health report of the Chief Complaint project. 

Please evaluate the following areas by exploring the codebase:
1. **Code Architecture:** Adherence to SOLID principles and clean separation of concerns between Managers, UI, and Data.
2. **Performance:** Usage of object pooling, reduction of GC allocations, and efficient loops.
3. **Test Coverage:** Presence and quality of NUnit EditMode and PlayMode tests.

When finished, generate a markdown artifact using the following exact structure:

```markdown
# Project Health Report
**Scope:** [Brief summary of folders/scripts scanned]
**Status:** Read-only review completed.

## What is already solid
- [Bullet points praising good architectural decisions, decoupling, etc.]

## P0 - Bugs / correctness risks (fix first)
1. **[Issue Name]**
   - **File:** `[Filename]`
   - **Problem:** [Description of the leak, tight coupling, or error]
   - **Fix:** [Actionable refactoring suggestion]

## P1 - Architectural Debt & Performance
1. **[Issue Name]**
   - [Description and Fix]
```

If necessary, spawn a 'research' subagent to help explore the codebase in the background.
