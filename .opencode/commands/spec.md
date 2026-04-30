---
description: Create a strict engineering specification
agent: planner
---

# /spec

Use the `iris-engineering` skill.
Use the `spec` skill.

Create a strict engineering specification for:

$ARGUMENTS

If the target is empty, stop and report that the specification topic is missing.

## Hard Rules

Do not implement.  
Do not edit files.  
Do not create files.  
Do not save the specification unless the user explicitly asks for `/save-spec`.  
Do not jump into architecture design.  
Do not jump into implementation planning.  
Do not produce a file edit checklist.  
Do not update memory files.  
Do not run destructive commands.

Inspect relevant project context before writing the specification.

Ask questions only if they block a safe specification.  
If information is missing but non-blocking, write explicit assumptions.

If factual context is missing, empty, or clearly points to the wrong directory, report an evidence gap instead of inventing project state.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/spec/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach (`$dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path `$dir) { Write-Output `$dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## .NET Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`

## Targeted Inspection Guidance

After reading the injected context above:

1. Identify the relevant docs, source folders, tests, and prior decisions for `$ARGUMENTS`.
2. Perform additional read-only inspection only where needed.
3. Prefer existing repository terminology and architecture boundaries.
4. Do not infer implementation details beyond what is needed for a specification.
5. Preserve Clean/Hexagonal boundaries unless the approved project context explicitly says otherwise.

For Iris, respect these known boundary defaults unless repository evidence overrides them:

- `Iris.Domain` owns pure domain concepts and must not reference infrastructure, EF Core, UI, HTTP, model providers, or application handlers.
- `Iris.Application` owns use cases, orchestration, ports, policies, and prompt/context assembly; it must not depend on concrete adapters.
- `Iris.Persistence`, `Iris.ModelGateway`, `Iris.Perception`, `Iris.Tools`, `Iris.Voice`, and `Iris.Infrastructure` are adapters/implementation projects.
- `Iris.Desktop`, `Iris.Api`, and `Iris.Worker` are hosts.
- `Iris.Shared` must stay neutral and must not become a product-specific dumping ground.
- UI must not directly call database, model providers, or domain-changing infrastructure.
- Tools do not decide permissions; Application policy does.
- Voice does not own chat orchestration.
- Perception does not own memory extraction.

## Specification Scope Rules

The specification must define:

- problem;
- goal;
- in-scope work;
- out-of-scope work;
- non-goals;
- affected areas;
- functional requirements;
- architecture constraints;
- contract requirements;
- data/state requirements;
- error handling and failure modes;
- testing requirements;
- documentation and memory requirements;
- acceptance criteria;
- blocking open questions.

The specification must not include:

- implementation sequence;
- detailed class-by-class plan;
- file edit checklist;
- speculative abstractions;
- unrelated cleanup;
- design decisions beyond hard constraints;
- code patches.

## Output Format

Use the exact output format from `.opencode/skills/spec/SKILL.md` when available.

If the skill file is missing, use this fallback format:

# Specification: <Task Name>

## 1. Problem

## 2. Goal

## 3. In Scope

## 4. Out of Scope

## 5. Non-Goals

## 6. Affected Areas

## 7. Functional Requirements

## 8. Architecture Constraints

## 9. Contract Requirements

## 10. Data and State Requirements

## 11. Error Handling and Failure Modes

## 12. Testing Requirements

## 13. Documentation and Memory Requirements

## 14. Acceptance Criteria

## 15. Assumptions

## 16. Blocking Questions

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

After producing the specification, append this section to the output:

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | This specification |
| B — Design | ⬜ Not yet run | Run `/design` when ready |
| C — Plan | ⬜ Not yet run | Run `/plan` when ready |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |

All gate status entries must use exactly these emoji:

- ✅ Satisfied — gate condition met
- ⬜ Not yet run — gate not yet executed
- ⚠️ Skipped — gate intentionally skipped with reason
- ❌ Blocked — gate required but cannot be satisfied
