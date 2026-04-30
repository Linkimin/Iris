---
description: Review architecture boundaries, dependency direction, layer ownership, project references, and forbidden shortcuts
agent: reviewer
---

# /architecture-review

Use the `iris-engineering` skill.
Use the `iris-architecture` skill.
Use the `iris-review` skill.
Use the `architecture-boundary-review` skill.

Review architecture boundaries for:

$ARGUMENTS

If the review target is empty, review the current working tree diff, staged diff, and project structure.

## Hard Rules

Do not implement fixes.  
Do not edit files.  
Do not create files.  
Do not modify tests.  
Do not modify documentation.  
Do not update memory files.  
Do not run destructive commands.  
Do not run formatting commands.  
Do not run migrations.  
Do not update packages.  

Inspect evidence before making a readiness decision.  
Report missing evidence explicitly.  
Focus only on architecture boundaries and structural drift.

If factual context is missing, empty, or clearly points to the wrong directory, report an evidence gap instead of inventing project state.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-architecture/SKILL.md,.opencode/skills/iris-review/SKILL.md,.opencode/skills/architecture-boundary-review/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Architecture Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/architecture-context.ps1`

## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`

## Staged Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --cached --no-ext-diff"`

## Required Context

Before reviewing, consider:

- `AGENTS.md`;
- `.opencode/skills/architecture-boundary-review/SKILL.md`;
- relevant `.opencode/rules/*.md`;
- approved specification if present;
- approved architecture design if present;
- approved implementation plan if present;
- current git status;
- current git diff;
- project/solution structure;
- project reference files;
- dependency injection registration files;
- changed source files;
- changed test files;
- architecture tests if present;
- `.agent/overview.md` or `.agents/overview.md` if present;
- `.agent/PROJECT_LOG.md` or `.agents/PROJECT_LOG.md` if present;
- `.agent/mem_library/**` or `.agents/mem_library/**` if relevant.

## Review Scope

Check:

- dependency direction;
- layer ownership;
- project references;
- package placement;
- dependency injection boundaries;
- abstraction ownership;
- contract direction;
- adapter/host separation;
- domain purity;
- application adapter-independence;
- shared neutrality;
- forbidden shortcuts;
- architecture tests and regression protection.

## Expected Iris Dependency Direction

Use this expected direction unless repository-local architecture documents say otherwise:

```text
Iris.Shared
  ↑
Iris.Domain
  ↑
Iris.Application
  ↑
Adapters: Iris.Persistence / Iris.ModelGateway / Iris.Perception / Iris.Tools / Iris.Voice / Iris.Infrastructure / Iris.SiRuntimeGateway
  ↑
Hosts: Iris.Desktop / Iris.Api / Iris.Worker
```

Allowed general rules:

- Domain may depend on Shared only.
- Application may depend on Domain and Shared only.
- Persistence implements Application persistence abstractions and may depend on Application, Domain, Shared, and EF/SQLite packages.
- ModelGateway implements Application model abstractions and may depend on Application, Domain, Shared, and HTTP/client packages.
- Perception implements Application perception abstractions and may depend on Application, Domain, Shared, and OS/desktop integration packages.
- Tools implements Application tool abstractions and must not own permission policy decisions.
- Voice implements Application voice abstractions and must not own chat orchestration.
- Infrastructure contains cross-cutting adapter implementations and must not become a business-logic dumping ground.
- Desktop/API/Worker are hosts and should orchestrate through Application use cases/facades, not direct adapters.
- Shared must remain neutral primitives only.

## Forbidden Shortcuts

Flag these if present:

- UI directly calls database;
- UI directly calls model provider;
- UI directly calls file system for domain behavior;
- Domain references infrastructure;
- Domain references Application;
- Application references Persistence implementation;
- Application references ModelGateway implementation;
- Application references Perception implementation;
- Application directly constructs adapter classes;
- Tools decide permissions without Application policy;
- Voice owns chat orchestration;
- Perception owns memory extraction;
- Worker bypasses Application use cases;
- API exposes persistence entities directly;
- Shared contains product/domain-specific logic;
- Python/runtime gateway writes directly to Iris SQLite source of truth unless explicitly designed through Application-owned contracts.

## Severity Rules

Use:

- P0 — must fix before merge.
- P1 — should fix before merge.
- P2 — acceptable backlog.
- Note — observation only.

Do not inflate or soften severity without evidence.

## Output Format

# Architecture Boundary Review: <Task Name>

## 1. Summary

### Review Status

Passed / Passed with P2 notes / Blocked by P1 issues / Blocked by P0 issues / Partial / Evidence insufficient

### Architecture Readiness

Ready / Ready after P2 backlog / Not ready / Cannot determine

### High-Level Result

<brief outcome>

## 2. Context Reviewed

- Architecture rules:
- Spec/design/plan:
- Git status:
- Git diff:
- Project references:
- DI registration:
- Source files:
- Test files:
- Architecture tests:

## 3. Dependency Direction Review

Expected direction:

```text
<project-specific dependency direction>
```

### Findings

- ...

## 4. Layer Ownership Review

| Layer / Module | Expected Ownership | Observed Concern |
|---|---|---|
| Domain | ... | ... |
| Application | ... | ... |
| Adapters | ... | ... |
| Hosts | ... | ... |
| Shared | ... | ... |

## 5. Forbidden Shortcut Review

| Shortcut | Status | Evidence |
|---|---|---|
| UI → Database | Pass / Fail / N/A | ... |
| UI → Model Provider | Pass / Fail / N/A | ... |
| Domain → Infrastructure | Pass / Fail / N/A | ... |
| Domain → Application | Pass / Fail / N/A | ... |
| Application → Concrete Adapter | Pass / Fail / N/A | ... |
| Host Owns Business Logic | Pass / Fail / N/A | ... |
| Shared Contains Product Logic | Pass / Fail / N/A | ... |

## 6. Project Reference Review

### Findings

- ...

## 7. Dependency Injection Review

### Findings

- ...

## 8. Contract and Abstraction Review

### Findings

- ...

## 9. Test Boundary Review

### Findings

- ...

## 10. Consolidated Findings

### P0 — Must Fix

No P0 issues.

### P1 — Should Fix

No P1 issues.

### P2 — Backlog

No P2 issues.

### Notes

- ...

## 11. Suggested Fix Order

No fixes required.

## 12. Final Decision

Approved / Approved with P2 backlog / Changes requested / Blocked / Cannot determine from available evidence

## Execution Note

No fixes were implemented.
No files were modified.

## Gate Status

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ⬜ Not checked | This review may reference a spec but does not produce one |
| B — Design | ⬜ Not checked | This review may reference a design but does not produce one |
| C — Plan | ⬜ Not checked | This review may reference a plan but does not produce one |
| D — Verify | ⬜ Not checked | Run `/verify` separately |
| E — Architecture Review | ✅ Satisfied | This review |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |

## Finding Format

For every non-trivial finding, use:

#### <Severity>-<Number>: <Title>

- Evidence:
  - `<path>` / `<symbol>` / `<project reference>` / `<test>`
- Impact:
  - ...
- Recommended fix:
  - ...

If the review cannot be completed, respond with:

# Architecture Review Blocked

## Reason

<reason>

## What Was Checked

- ...

## Evidence Gap

- ...

## Safe Next Step

- ...
