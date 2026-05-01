# IrisEngineering v2 Phase 4 Command Level 4 Rewrite

## Goal

Rewrite the 14 OpenCode command templates so they use Iris skills, canonical rules, and Phase 3 shared scripts.

## Level 4 Definition

Level 4 command = workflow selector + focused skills + canonical loaded rules + shared script context.

## Source Inputs

- `.opencode/commands/*.md`
- `.opencode/scripts/*.ps1`
- `.opencode/rules/*.md`
- `.opencode/skills/**/*.md`
- `opencode.jsonc`

## Rewrite Decision

Commands should use official OpenCode shell injection syntax:

```markdown
!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`
```

Large inline PowerShell resolver blocks are forbidden in command templates after this phase.

## In Scope

- Rewrite `.opencode/commands/*.md`.
- Keep command behavior and output contracts.
- Add `iris-engineering` plus focused skill usage lines.
- Replace large context blocks with shared script calls.

## Out Of Scope

- Script behavior changes.
- Skill/rule rewrites.
- `opencode.jsonc` changes.
- Product code changes.
- `AGENTS.md` changes.

## Acceptance

- 14 commands still exist.
- No command contains `E:\Work\Iris`.
- No command contains copied `$repo = if (...)` resolver blocks.
- Commands call shared scripts for repository, memory, guidance, .NET, and architecture context.
- Commands keep read/write boundaries.
- Commands still preserve `$ARGUMENTS` where they used it before.
