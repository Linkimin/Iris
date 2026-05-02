---
description: Pre-spec collaborative scoping dialogue — explore ideas before committing to a specification
agent: planner
---

# /brainstorm

Use the `iris-engineering` skill.
Use the `iris-brainstorm` skill.

Run a structured pre-spec brainstorming dialogue for:

$ARGUMENTS

If the topic is empty, stop and ask what the user wants to brainstorm about.

## Hard Rules

Do not implement.
Do not edit files.
Do not create files.
Do not save the brainstorm output unless the user explicitly asks.
Do not jump into specification writing.
Do not jump into architecture design.
Do not jump into implementation planning.
Do not update memory files.
Do not run destructive commands.
Do not bundle multiple questions in one response — ask one question at a time.
Do not skip the approach comparison step — always propose 2–3 approaches.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Project Guidance Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md,.opencode/skills/iris-brainstorm/SKILL.md`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Brainstorming Scope Rules

The brainstorming dialogue must follow the `iris-brainstorm` skill exactly:

1. **Inspect context first.** Before asking the user anything, understand the project state: current phase, architecture constraints, existing design decisions.

2. **One question at a time.** Ask exactly one clarifying question per response. Wait for the user's answer. Prefer multiple-choice questions when options are enumerable.

3. **Propose 2–3 approaches.** After sufficient clarification, propose 2–3 distinct approaches with explicit trade-offs. At least one must be minimal/iterative. Every approach must respect Iris architecture boundaries (see Architecture Constraints below).

4. **Incremental validation.** After each scoping section, confirm with the user before proceeding.

5. **Flag multi-subsystem scope.** If the topic spans multiple independent subsystems, flag it immediately and propose decomposition.

6. **Produce a Brainstorm Output.** When the user confirms direction, produce the output in the exact format from `.opencode/skills/iris-brainstorm/SKILL.md`:

```markdown
## Brainstorm Output: <Topic>

### Problem Summary
<One paragraph.>

### Agreed Scope
- In scope: ...
- Out of scope: ...
- Non-goals: ...

### Selected Approach
<Brief rationale.>

### Architecture Notes
<Which Iris layers are affected, which boundaries must be preserved.>

### Open Questions
<Unresolved questions or "None — ready to proceed.">

### Recommended Next Stage
<Typically `spec`.>
```

7. **Handoff to spec.** After producing the Brainstorm Output, recommend `/spec <topic>` as the next stage. The Brainstorm Output does NOT satisfy Gate A — it is context for spec, not a substitute.

## Architecture Constraints

Every approach proposed during brainstorming must respect:

- `Iris.Domain` — pure domain concepts, no infrastructure.
- `Iris.Application` — use cases and ports, no concrete adapters.
- Adapters (`Persistence`, `ModelGateway`, `Tools`, `Voice`, `Perception`, `Infrastructure`) — implement ports, don't own workflow.
- Hosts (`Desktop`, `Api`, `Worker`) — composition root only, no business logic.
- `Iris.Shared` — product-neutral primitives, no product behavior.

Forbidden shortcuts to flag:

- UI → database, UI → model providers.
- Domain → EF Core, Domain → Application.
- Application → concrete adapters.
- Tools owning permission decisions.
- Voice owning chat orchestration.
- Perception owning memory extraction.

## Output Rules

Keep the dialogue conversational and focused. Never produce implementation code, file paths, or edit instructions during brainstorming.

End every brainstorming session with:

```markdown
---

✅ Read-Only Guarantee: No files were modified during this brainstorming session.
```

## Final Response Requirements

After the brainstorming dialogue concludes and the Brainstorm Output is produced:

## Execution Note

No implementation was performed.
No files were modified.

## Next Stage

Run `/spec <topic>` with the Brainstorm Output as input.
