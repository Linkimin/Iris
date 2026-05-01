# IrisEngineering v2 Phase 2 Rules Consolidation

## Goal

Make canonical v2 rules the only loaded OpenCode instruction layer.

## Current Problem

`opencode.jsonc` loads both canonical rules and legacy numbered rules. Even when legacy files are short, this still creates two apparent rule layers and invites future drift.

## Final Loading Decision

`opencode.jsonc` should load only:

1. `.opencode/rules/workflow.md`
2. `.opencode/rules/iris-architecture.md`
3. `.opencode/rules/no-shortcuts.md`
4. `.opencode/rules/memory.md`
5. `.opencode/rules/verification.md`
6. `.opencode/rules/dotnet.md`
7. `.opencode/rules/security.md`
8. `.opencode/rules/review-audit.md`

## Compatibility Decision

Keep numbered files as compatibility pointers because existing command templates and `AGENTS.md` may still reference them before Phase 3 command rewrite.

## Rule Ownership

| Rule | Owns | Must not own |
|---|---|---|
| `workflow.md` | stage boundaries, gates, command modes | architecture details, .NET details |
| `iris-architecture.md` | layer ownership, dependency direction | command workflow |
| `no-shortcuts.md` | absolute forbidden shortcuts | explanatory playbooks |
| `memory.md` | `.agent` memory file roles and write policy | product memory content |
| `verification.md` | verification evidence policy | implementation fixes |
| `dotnet.md` | .NET project/package/test conventions | general workflow |
| `security.md` | secrets, destructive commands, supply-chain safety | architecture ownership |
| `review-audit.md` | review/audit severity and readiness rules | implementation steps |

## Legacy Mapping

| Legacy file | Canonical target |
|---|---|
| `00-core-workflow.md` | `workflow.md` |
| `10-architecture-boundaries.md` | `iris-architecture.md`, `no-shortcuts.md` |
| `20-dotnet-style.md` | `dotnet.md` |
| `30-testing-verification.md` | `verification.md` |
| `40-agent-memory.md` | `memory.md` |
| `50-security-safety.md` | `security.md` |
| `60-audit.md` | `review-audit.md` |

## Out Of Scope

- Command rewrite.
- Shared PowerShell scripts.
- Skill deepening.
- Plugin changes.
- AGENTS.md rewrite.
