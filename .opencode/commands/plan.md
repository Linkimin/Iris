---
description: Create a phased implementation plan from an approved specification and architecture design
agent: planner
---

# /plan

Use the `iris-engineering` skill.
Use the `plan` skill.

Create a phased implementation plan for:

$ARGUMENTS

If the requested target is ambiguous, infer the most likely active task from the injected repository context, approved or draft specification/design files, and agent memory. Ask questions only if safe planning is blocked.

## Hard Rules

Do not implement.  
Do not edit files.  
Do not create files.  
Do not save the plan unless the user explicitly asks for `/save-plan`.  
Do not rewrite the specification.  
Do not redesign the architecture.  
Do not introduce requirements that are not in the specification/design.  
Do not change phase order from the approved design unless the user explicitly asks.  
Do not propose destructive commands.  
Do not modify source code, tests, configuration, documentation, or memory files.

The output is a plan only.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/plan/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Documentation Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -Command ". .opencode/scripts/resolve-repo.ps1 -Quiet | Out-Null; Write-Output '## Docs Directories'; foreach (`$dir in @('docs/specs','docs/designs','docs/plans','docs/implementation','docs/superpowers/plans')) { if (Test-Path `$dir) { Write-Output `$dir } }; Write-Output ''; Write-Output '## Recent Markdown Artifacts'; if (Test-Path 'docs') { Get-ChildItem 'docs' -Recurse -File -Include '*.md' | Where-Object { `$_.FullName -notmatch '\\\\bin\\\\|\\\\obj\\\\|\\\\.git\\\\|\\\\.worktrees\\\\|\\\\.opencode\\\\node_modules\\\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 60 FullName,LastWriteTime } else { Write-Output 'docs directory not found' }"`

## .NET Discovery Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1`

## Required Context To Consider

Before writing the plan, consider:

- `AGENTS.md`;
- `.opencode/skills/plan/SKILL.md`;
- relevant `.opencode/rules/*.md`;
- approved or draft specification;
- approved or draft architecture design;
- relevant source structure;
- relevant contracts and interfaces;
- relevant tests and test projects;
- build and verification configuration;
- current git status and changed files;
- `.agent/overview.md` or `.agents/overview.md` if present;
- `.agent/PROJECT_LOG.md` or `.agents/PROJECT_LOG.md` if present;
- `.agent/local_notes.md`, `.agent/log_notes.md`, `.agents/local_notes.md`, or `.agents/log_notes.md` if present;
- `.agent/mem_library/**` or `.agents/mem_library/**` if relevant.

If the approved/draft specification or design is not available, produce a blocked result instead of inventing requirements or architecture.

## Scope Rules

The plan must define:

- plan goal;
- inputs and assumptions;
- in-scope work;
- out-of-scope work;
- forbidden changes;
- implementation strategy;
- phased implementation sequence;
- files to inspect;
- files likely to edit;
- files that must not be touched;
- verification per phase;
- rollback guidance per phase;
- testing plan;
- documentation and memory plan;
- verification commands;
- risk register;
- implementation handoff notes;
- blocking open questions.

The plan must not include:

- actual code implementation;
- source code patches;
- broad unrelated refactors;
- changed requirements;
- changed architecture decisions;
- speculative features;
- destructive commands;
- fake precision unsupported by inspected project context.

## Planning Requirements

Use the exact output format from `.opencode/skills/plan/SKILL.md` when that file exists.

The plan must be implementable by a coding agent without requiring architectural improvisation.

Every phase must include:

- purpose;
- files to inspect before editing;
- files likely to edit;
- forbidden edits;
- expected outcome;
- verification commands;
- rollback guidance;
- acceptance checkpoint.

Every phase must preserve:

- dependency direction;
- layer ownership;
- SOLID;
- Clean Code;
- existing public contracts unless the specification explicitly changes them;
- existing tests unless the plan explicitly calls for adding or updating tests;
- project memory discipline.

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

After producing the plan, append this section to the output:

### Audit Gate Status

| Gate | Status | Evidence |
|---|---|---|
| A — Spec | ✅ Satisfied | `<path to spec>` |
| B — Design | ✅ Satisfied | `<path to design>` |
| C — Plan | ✅ Satisfied | This plan |
| D — Verify | ⬜ Not yet run | Run `/verify` after implementation |
| E — Architecture Review | ⬜ Not yet run | Run `/architecture-review` if boundary changes |
| F — Audit | ⬜ Not yet run | Run `/audit` before merge claim |
| G — Memory | ⬜ Not yet run | Run `/update-memory` after meaningful work |
