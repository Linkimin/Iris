# Iris Workflow Rules

## Stage Separation

For non-trivial work, keep stages separate:

```text
Spec -> Design -> Plan -> Implement -> Verify -> Review -> Audit -> Memory update
```

Do not implement during spec/design/plan.
Do not fix findings during review/audit unless the user explicitly asks.
Do not update memory outside `/update-memory` or an explicitly allowed workflow.

## Required Gates

| Gate | Name | Required when | Satisfied by |
|---|---|---|---|
| A | Spec exists | New features, behavior changes, architecture changes, persistence changes, provider changes, UI flows. Not required for typos or trivial local fixes. | `/spec` output or explicit user statement that task is trivial/local |
| B | Design exists | Architecture-affecting changes (new dependencies, public contracts, DI composition, persistence schema, adapter seams, host wiring, memory/tool/voice/perception behavior) | `/design` output or explicit approval that design is not needed |
| C | Plan exists | Any multi-file change | `/plan` output or explicit user authorization for small direct implementation |
| D | Verification completed | Before readiness claims | `/verify` output or explicit reason for skipped verification |
| E | Architecture review completed | Boundary changes (project references, DI, ports, adapters, hosts, Shared) | `/architecture-review` output |
| F | Audit completed | Before merge/readiness claim | `/audit` output |
| G | Memory updated | After meaningful completed work | `/update-memory` output or confirmed memory write during `/implement` |

`/implement` must stop if Gate C is missing.

## Dirty Tree Rule

Before editing, inspect git state.

Do not overwrite, revert, stage, or normalize unrelated user changes.

## File Creation Rule

Before creating a file, check existing files, placeholders, ownership, and phase scope.

No speculative files.
No duplicate responsibilities.
