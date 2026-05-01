# IrisEngineering v2 Phase 3 Shared Scripts

## Goal

Create reusable read-only PowerShell context scripts for OpenCode command templates.

## Decision

Phase 3 creates scripts only. Command templates remain unchanged until the command thinning phase.

## Script Ownership

| Script | Owns | Must not own |
|---|---|---|
| `resolve-repo.ps1` | repository root detection and current-location normalization | git summaries, memory reading |
| `git-context.ps1` | git status, changed files, staged files, untracked files, stats, recent commits | full diff output |
| `agent-memory-context.ps1` | `.agent` preferred memory context with `.agents` fallback | memory writes |
| `project-guidance-context.ps1` | AGENTS, canonical rules, requested skills | repository status, build discovery |
| `dotnet-discovery.ps1` | solution/project/test/config discovery | running build/test |
| `architecture-context.ps1` | project references and boundary-sensitive discovery | architecture decisions |

## Command Migration Targets

Later command thinning should replace large inline shell blocks with:

```markdown
!powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1
!powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1
!powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md
!powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1
!powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/architecture-context.ps1
```

## Safety

Scripts are read-only. They do not write files, stage changes, push, restore, format, apply migrations, or update memory.

## Secrets

Scripts must not print secret-bearing files such as `.env`, `.env.*`, private keys, user secrets, or local appsettings overrides.

## Out Of Scope

- Rewriting commands.
- Changing rules.
- Changing skills.
- Changing agents or plugins.
- Changing `AGENTS.md`.
