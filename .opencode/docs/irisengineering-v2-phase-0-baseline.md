# IrisEngineering v2 Phase 0 Baseline

## Current Inventory

### Commands

| Command | Current role | Main risk |
|---|---|---|
| `/status` | Summarizes git and memory state | Hardcoded repo/context snippets |
| `/spec` | Produces specification | Duplicated context and legacy memory naming |
| `/design` | Produces design | Duplicated context and legacy memory naming |
| `/plan` | Produces implementation plan | Duplicated context and legacy memory naming |
| `/implement` | Implements approved plan | Duplicated context and must enforce gates later |
| `/verify` | Runs verification | Duplicated context and command policy should move to rules/scripts |
| `/review` | Read-only engineering review | Good output shape, duplicated context |
| `/architecture-review` | Boundary review | Good focus, duplicated context |
| `/audit` | Formal readiness audit | Duplicated context and should align with gates |
| `/update-memory` | Memory update workflow | Must use `.agent/log_notes.md` as canonical |
| `/save-spec` | Saves spec artifact | Must not update memory unless explicitly allowed |
| `/save-design` | Saves design artifact | Must not update memory unless explicitly allowed |
| `/save-plan` | Saves plan artifact | Must not update memory unless explicitly allowed |
| `/save-audit` | Saves audit artifact | Must not update memory unless explicitly allowed |

### Existing Skills

Existing action skills are useful and should not be deleted during Phase 0-1. The v2 Iris skills should become stable methodology above them.

Current action skills:

- `agent-memory`
- `architecture-boundary-review`
- `audit`
- `design`
- `implement`
- `plan`
- `save-audit`
- `save-design`
- `save-plan`
- `save-spec`
- `spec`
- `verify`

### Existing Rules

Existing numbered rules are useful but contain naming drift and overlap. Phase 1 should add canonical named rules and make numbered rules compatible.

Current numbered rules:

- `00-core-workflow.md`
- `10-architecture-boundaries.md`
- `20-dotnet-style.md`
- `30-testing-verification.md`
- `40-agent-memory.md`
- `50-security-safety.md`
- `60-audit.md`

### Existing Agents

Current OpenCode role files are present and are not part of Phase 0-1:

- `planner.md`
- `builder.md`
- `reviewer.md`
- `auditor.md`

### Existing Plugins

Plugins already provide useful guardrails. Phase 0-1 must not modify plugin behavior.

Current plugin files:

- `dotnet-verify-reminder.ts`
- `guardrails.ts`
- `session-summary.ts`

## Confirmed Problems

- Commands duplicate large PowerShell context blocks.
- Some skills/rules prefer `.agents/local_notes.md`.
- Iris actual memory convention is `.agent/log_notes.md`.
- Current skills are action-specific, but there is no central Iris methodology skill.
- Current rules are useful, but not yet organized as v2 canonical guardrails.
- `opencode.jsonc` loads only legacy numbered rules.

## Phase 1 Migration Decision

- Add canonical Iris skills.
- Add canonical Iris rules.
- Align old numbered rules with canonical rules.
- Register v2 rules in `opencode.jsonc`.
- Do not rewrite commands yet.
- Do not create scripts yet.

## Out Of Scope

- Command thinning.
- Shared PowerShell scripts.
- Plugin changes.
- Agent role changes.
- Repo-level architecture tests.
- CI changes.

## Phase 0 Evidence

The inventory was produced from local `.opencode` filesystem inspection on 2026-04-30.

The duplicated context and memory naming drift were confirmed by searching `.opencode/commands`, `.opencode/skills`, and `.opencode/rules` for:

- inline `powershell` command blocks;
- hardcoded `E:\Work\Iris`;
- `.agent` and `.agents`;
- `local_notes` and `log_notes`;
- build/test command references.

## Phase 0 Conclusion

Phase 0 confirms that the current OpenCode setup is a functional v1 foundation. Phase 1 should add a canonical v2 methodology layer without deleting or rewriting existing commands.
