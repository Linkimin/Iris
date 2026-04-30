---
description: Create an architecture design from an approved specification
agent: planner
---

# /design

Use the `iris-engineering` skill.
Use the `iris-architecture` skill.
Use the `design` skill.

Create an architecture design for:

$ARGUMENTS

If the design target is empty, stop and ask for the specification or task name.

## Hard Rules

Do not implement.  
Do not edit files.  
Do not create files.  
Do not save the design unless the user explicitly asks for `/save-design`.  
Do not create an implementation plan.  
Do not rewrite the specification.  
Do not introduce requirements that are not in the specification.  
Do not silently resolve blocking specification questions.  
Do not modify documentation.  
Do not update memory files.  
Do not run destructive commands.

Inspect factual project context before writing the design.

Ask questions only if they block safe design. If reasonable assumptions are enough, state them explicitly and continue.

If repository context is missing, empty, or points to the wrong directory, report an evidence gap instead of inventing project state.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-architecture/SKILL.md,.opencode/skills/design/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach (`$dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path `$dir) { Write-Output `$dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## .NET Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`

## Architecture Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/architecture-context.ps1`

## Current Diff Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; git diff --no-ext-diff"`

## Design Scope

The design must define:

- technical design goal;
- specification traceability;
- current architecture context;
- proposed design summary;
- responsibility ownership;
- component design;
- contract design;
- data flow;
- data/state design;
- error handling design;
- configuration and dependency injection impact;
- security and permission considerations;
- testing design;
- options considered;
- risks and trade-offs;
- blocking open questions.

The design must not include:

- step-by-step implementation sequence;
- file edit checklist;
- exact implementation phases;
- speculative requirements;
- unrelated refactoring;
- implementation code beyond small illustrative contract snippets.

## Required Review Discipline

Before writing the design:

1. Identify the approved or draft specification that the design is based on.
2. Trace each major design decision back to a specification requirement or hard architecture constraint.
3. Check current project structure and dependency direction.
4. Check existing contracts and abstractions before proposing new ones.
5. Prefer extension of existing boundaries over new abstractions unless justified.
6. Mark unresolved specification gaps as blocking questions instead of silently designing around them.

## Output Format

Use the exact output format from `.opencode/skills/design/SKILL.md` if that file exists.

If the skill file is missing, use this fallback structure:

# Architecture Design: <Task Name>

## 1. Summary

## 2. Specification Traceability

## 3. Current Architecture Context

## 4. Proposed Design

## 5. Responsibility Ownership

## 6. Component Design

## 7. Contract Design

## 8. Data Flow

## 9. Data / State Design

## 10. Error Handling and Failure Modes

## 11. Configuration and Dependency Injection Impact

## 12. Security and Permission Considerations

## 13. Testing Design

## 14. Options Considered

## 15. Risks and Trade-Offs

## 16. Acceptance Mapping

## 17. Blocking Questions

## Final Response Requirements

End with:

## Execution Note

No implementation was performed.  
No files were modified.

## Assumptions

- ...

## Blocking Questions

No blocking questions.

If there are blocking questions, list them instead of writing `No blocking questions`.

## Gate Status

After producing the design, append this section to the output:

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | `<path to spec>` |
| B — Design | ✅ Satisfied | This design |
| C — Plan | ⬜ Not yet run | Run `/plan` when ready |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |

Use the same emoji legend as `/spec`.
