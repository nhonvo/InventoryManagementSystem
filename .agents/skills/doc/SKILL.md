---
description: Scaffold and synchronize documentation from templates and code audits.
type: workflow
status: active
version: 3.0
tags: [workflow, documentation, atomic-docs, sync, inventoryalert]
---

// turbo-all

# /doc — Documentation Sync

**Objective**: After implementing a feature, synchronize the `doc/` folder with the actual code state using a structured audit. Persist knowledge so future sessions start fast.

---

## Phase 0: Locate Existing Docs

1. Check existing docs in `doc/`.
2. Check existing wiki documentation in `src/ui/InventoryAlert.Wiki/docs/`.

---

## Phase 1: Documentation Scaffolding

Create or update the relevant `doc/` file based on what changed:

| Change Type | Doc to update |
|-------------|--------------|
| New entity or feature | `doc/<feature-id>_spec.md` (create) |
| New API endpoint | `doc/WALKTHROUGH.md` (add endpoint) |
| New enhancement | `doc/ENHANCEMENT_PLAN.md` |
| Tech debt tracked | `.agents/GEMINI.md` → Known Tech Debt table |
| Roadmap item | `doc/ROADMAP.md` |

**Doc file format** — every `.md` in `doc/` must have this header:

```yaml
---
description: <one-line summary>
type: spec | plan | reference | walkthrough
status: draft | active | deprecated
version: 1.0
tags: [relevant, keywords, here]
last_updated: YYYY-MM-DD
---
```

---

## Phase 2: Code Audit & Sync

1. **Verify the implementation matches the spec**.
2. **Confirm test coverage**:
   ```bash
   dotnet test InventoryManagementSystem --no-build --filter "FullyQualifiedName~<EntityName>"
   ```
3. **Update `GEMINI.md`** if:
   - A new service method was added → update the Key Service table
   - A new tech debt was identified → add to Known Tech Debt table
   - A new slash command was added → update Available Slash Commands

---

## Phase 3: Commit

1. **Commit** when implementation and docs are stable:
   ```bash
   git add .
   git commit -m "docs(<scope>): sync <feature> documentation"
   ```

---

**Parent Context**: `GEMINI.md`
**Next Action**: `/run-tests` → `/code-review` → push PR
**Keywords**: `doc`, `scaffold`, `sync`, `atomic-docs`, `inventoryalert`, `knowledge`
