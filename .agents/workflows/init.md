---
description: initialize project context and guide on search/docs
type: workflow
status: active
version: 2.0
tags: [workflow, init, context, onboarding]
---

// turbo-all

# /init — Project Initialization & Context Sync

**Objective**: Rapidly sync with `InventoryAlert.Api` current state and locate critical technical docs before starting any work session.

**Tech Stack**: .NET 10 · EF Core 10 · PostgreSQL · Finnhub API · xUnit + Moq + FluentAssertions

---

## Phase 0: Project Tree Inspection

1. **Map project tree**:

   ```powershell
   Get-ChildItem -Directory | Select-Object Name
   ```

---

## Phase 1: Technical Resource Map

Core knowledge locations for this project:

| What | Where |
|------|-------|
| Cold-start briefing | `GEMINI.md` |
| DDD layer rules | `.agents/skills/ddd-architecture/SKILL.md` |
| Testing patterns + Moq recipes | `.agents/skills/testing-patterns/SKILL.md` |
| Finnhub client + worker logic | `.agents/skills/finnhub-integration/SKILL.md` |
| Project coding rules | `.agents/rules/project-rules.md` |
| Unit test plan | `doc/unit_test_plan.md` |
| DDD refactor plan | `doc/DDD_REFACTOR_PLAN.md` |
| Roadmap | `doc/ROADMAP.md` |

---

**Parent Context**: `GEMINI.md`
**Next Action**: `/feature-flow` or `/plan`
**Keywords**: `init`, `context`, `onboarding`, `inventoryalert`
