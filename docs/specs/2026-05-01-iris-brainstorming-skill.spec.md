# Specification: Skill iris-brainstorming

## 1. Problem Statement

Iris workflow starts at `spec` — a strict, formal engineering specification stage. There is no pre-spec collaborative phase where:
- the agent explores the user's intent through structured dialogue;
- multiple approaches are compared before committing to scope;
- the user validates direction before engineering formalism kicks in.

Without a brainstorming stage, the `spec` stage receives ambiguous or premature input. The agent must either ask clarifying questions in the middle of specification writing (blurring the stage boundary) or invent assumptions that the user never validated. This leads to rework, wasted effort on the wrong scope, and spec revisions after design is already underway.

The Superpowers `brainstorming` skill demonstrates that a structured pre-spec dialogue — one question at a time, approach comparison, incremental validation — prevents these failures. Iris needs its own version, adapted to its Clean/Hexagonal discipline, its stage separation rules, and its prohibition on speculative files.

## 2. Goal

Add an `iris-brainstorming` workflow skill that:
- lives in `.opencode/skills/iris-brainstorm/SKILL.md`;
- activates when the user expresses a non-trivial intent that has no spec yet;
- guides a structured collaborative dialogue that produces enough clarity for `spec`;
- never produces code, never edits files, never updates memory;
- produces a concise Brainstorm Output: problem summary, agreed scope boundary, explicit non-goals, selected approach rationale, open questions, and a recommended next workflow stage (typically `spec`);
- integrates into the Iris stage separation: brainstorming is a pre-spec gate, not a replacement for spec.

## 3. Scope

### In Scope

- A new `.opencode/skills/iris-brainstorm/SKILL.md` file.
- Structured dialogue workflow: explore context → ask questions one at a time → propose 2-3 approaches → get user validation → document agreed outcome.
- Brainstorm Output format: problem summary, scope boundary, non-goals, selected approach, open questions, next stage recommendation.
- Integration into `iris-engineering` stage selection table: "Let's think / brainstorm / explore an idea" → `brainstorm`.
- Integration into `AGENTS.md` workflow: `Brainstorm -> Spec -> Design -> Plan -> Implement -> Verify -> Review -> Audit`.
- Integration into `AGENTS.md` skills list.
- Integration into gate logic: brainstorming is a pre-Gate-A activity (doesn't satisfy Gate A, but Gate A can reference brainstorm output as context).

### Out of Scope

- Implementation of any other gap skill (iris-debug, iris-tdd, iris-complete).
- Changing the `spec` skill itself.
- Changing the `design` skill itself.
- Changing `.opencode/rules/workflow.md` (brainstorm is a pre-gate activity; gates remain A-G).
- Code generation, file creation, or file editing by the brainstorming skill.
- Memory updates during brainstorming.
- Visual companion / browser-based mockups (the skill may mention that visual exploration can be done manually, but will not orchestrate it).
- Any material change to Iris source code, tests, or infrastructure.

### Non-Goals

- Replicating Superpowers `brainstorming` exactly. Iris-brainstorm is adapted to Iris conventions: no speculative design docs, no visual companion, no git worktree creation, no `docs/superpowers/specs/` paths.
- Making brainstorming mandatory. It is an available stage, not a hard gate. If the user has a clear spec-ready request, they can skip brainstorming and go directly to `spec`.
- Producing a formal design or plan during brainstorming.

## 4. Current State

### Existing Workflow

Current workflow in `AGENTS.md` and `iris-engineering/SKILL.md`:

```
Spec -> Design -> Plan -> Implement -> Verify -> Review -> Audit
```

There is no stage before `spec`.

### Existing Skill Conventions

Iris skills follow a consistent YAML frontmatter + Markdown structure:

```yaml
---
name: <skill-name>
description: <one-line purpose>
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: <stage>
  output_type: <type>
---
```

Skills are stored in `.opencode/skills/<skill-name>/SKILL.md`.

### Existing Iris-engineering Integration

`iris-engineering/SKILL.md` stage selection table maps user requests to stages:

```
"Let's think / decide / scope" → `/spec` or `/design`
```

This entry currently routes ambiguous exploration to `spec` or `design`. Adding a brainstorming stage would give this entry a natural first destination.

### Architecture Constraints Active

The brainstorming skill is an agent workflow skill — it affects `.opencode/` files only. It does not touch Iris source code, project references, DI, or runtime behavior. Architecture boundary rules (`.opencode/rules/iris-architecture.md`, `.opencode/rules/no-shortcuts.md`) do not apply to the brainstorming skill itself, but the brainstorming dialogue must respect them: it must not propose designs that violate Iris boundaries.

## 5. Affected Areas

| Area | Impact |
|------|--------|
| `.opencode/skills/iris-brainstorm/SKILL.md` | New file — primary artifact |
| `AGENTS.md` | Update workflow: `Brainstorm → Spec → Design → Plan → ...`. Update skills list. |
| `.opencode/skills/iris-engineering/SKILL.md` | Update stage selection table: add brainstorm row. Update workflow stages table. Update minimum output by stage. |
| `.agent/overview.md` | May reference brainstorming as active stage during workflow (no spec change needed now). |
| `C:/Users/User/.agents/skills/superpowers/brainstorming/SKILL.md` | No change. External reference only. |

Files explicitly NOT affected:
- `.opencode/rules/workflow.md` — gates remain A-G; brainstorming is pre-gate.
- `.opencode/skills/spec/SKILL.md` — no change to spec skill.
- `.opencode/skills/design/SKILL.md` — no change to design skill.
- Any Iris source code, tests, or infrastructure.

## 6. Functional Requirements

- **FR-001: Activation trigger.** The skill activates when the user expresses a non-trivial intent with no existing spec, using phrases like "let's think about", "I want to add", "let's brainstorm", "explore the idea of", or similar ambiguous scope requests. The agent invokes the skill BEFORE asking clarifying questions.

- **FR-002: Context exploration.** Before asking the user anything, the skill requires the agent to inspect: `AGENTS.md`, relevant `.opencode/rules/*.md` (architecture, no-shortcuts), `.agent/overview.md`, and relevant `.agent/mem_library/**`. The agent must understand what exists before proposing what to add.

- **FR-003: One question at a time.** The agent asks exactly one clarifying question per response. It does not bundle multiple questions. It waits for the user's answer before proceeding. Multiple-choice questions are preferred when options are enumerable.

- **FR-004: Approach comparison.** After sufficient clarification, the agent proposes 2-3 distinct approaches with explicit trade-offs. At least one approach must be minimal/iterative. The agent recommends one approach with reasoning. The agent must not propose approaches that violate Iris architecture (e.g., "put business logic in Desktop", "bypass Application layer").

- **FR-005: Incremental validation.** After each approach section (scope, non-goals, constraints), the agent asks whether it looks right so far. The user can revise direction before proceeding.

- **FR-006: Scope decomposition detection.** If the user's intent describes multiple independent subsystems, the agent flags this immediately and proposes decomposition before refining details. "This seems like N independent pieces. Let's focus on one first." This matches the Superpowers practice but uses Iris terminology.

- **FR-007: Brainstorm output format.** At the end of the dialogue (user confirms direction), the agent produces a concise Brainstorm Output with these sections:

  ```markdown
  ## Brainstorm Output: <Topic>

  ### Problem Summary
  One paragraph.

  ### Agreed Scope
  - In scope: ...
  - Out of scope: ...
  - Non-goals: ...

  ### Selected Approach
  Brief rationale.

  ### Architecture Notes
  Which Iris layers are affected, which boundaries must be preserved, which forbidden shortcuts apply.

  ### Open Questions
  Questions not resolved during brainstorming.

  ### Recommended Next Stage
  Typically `spec`. May be `design` if the topic is already well-scoped.
  ```

- **FR-008: Handoff to spec.** The Brainstorm Output serves as input to `spec`. The spec agent uses it as context (scope, non-goals, constraints) but starts its own independent analysis. Brainstorm output is NOT a substitute for spec.

- **FR-009: Read-only guarantee.** The brainstorming skill must never edit files, create files, run destructive commands, update memory, or produce implementation code. It operates entirely through dialogue and the final Brainstorm Output in the conversation.

- **FR-010: Saving the output.** If the user explicitly asks to save the brainstorm output, the agent may save it to a path the user specifies (e.g., `docs/specs/brainstorm-YYYY-MM-DD-topic.md`). No automatic saving. No path assumption without user confirmation.

## 7. Architecture Constraints

- **AC-001: Skill file location.** `iris-brainstorm/SKILL.md` lives in `.opencode/skills/`. It follows the existing Iris skill YAML frontmatter convention.

- **AC-002: No dependency on Superpowers.** The skill references Superpowers `brainstorming` only as a conceptual inspiration, not as a dependency. It does not import, load, or delegate to Superpowers skills at runtime.

- **AC-003: No gate manipulation.** Brainstorming is a pre-Gate-A activity. It does not change the gate system (A through G), the gate conditions, or the gate check procedure in `iris-engineering/SKILL.md`. Gate A remains: "spec exists OR user says task is trivial". Brainstorm output can be referenced as evidence during gate checks but is not itself a gate satisfier.

- **AC-004: Workflow stage separation preserved.** The brainstorming stage is read-only and distinct from spec/design/plan/implement. No stage blurring. The skill enforces its own stop conditions: if the user asks for code during brainstorming, the agent responds: "That belongs in implement. First, let's finish scoping. Ready to move to spec?"

- **AC-005: Iris architecture respected in dialogue.** When the agent proposes approaches during brainstorming, every approach must respect:
  - `Domain` owns pure concepts, not infrastructure.
  - `Application` owns orchestration, not concrete adapters.
  - Adapters implement ports; hosts compose.
  - No UI → database, no UI → providers, no Domain → EF Core.
  - Shared stays neutral.

- **AC-006: No speculative files in `.opencode/` or `docs/`.** Brainstorming does not create spec files, design files, or plan files. Brainstorm output stays in conversation until the user explicitly asks to save.

## 8. Contract Requirements

No Iris source-code contracts are affected. The only contract change is:

| Contract | Current behavior | Required behavior | Compatibility |
|----------|-----------------|-------------------|---------------|
| `iris-engineering` stage selection table | "Let's think / decide / scope" → `/spec` or `/design` | "Let's think / brainstorm / explore" → `/brainstorm` | Extended. `/spec` and `/design` remain valid destinations for more specific requests. |
| `AGENTS.md` workflow list | `Spec -> Design -> Plan -> ...` | `Brainstorm -> Spec -> Design -> Plan -> ...` | Extended. Existing stages unchanged. |
| `AGENTS.md` skills list | Lists 8 skills | Lists 9 skills (adds iris-brainstorming) | Extended. |

## 9. Data and State Requirements

No persisted data. No in-memory state beyond the conversation context. No database changes. No file system changes (except when the user explicitly asks to save the brainstorm output).

## 10. Error Handling and Failure Modes

| Failure mode | Required behavior |
|-------------|-------------------|
| User gives contradictory constraints | Agent flags the contradiction: "Earlier you said X, now you're saying Y. Which takes priority?" — one question at a time. |
| User's idea violates Iris architecture | Agent explains which boundary would be violated and why. Asks whether the user accepts that constraint or wants to reconsider the approach. |
| User tries to skip to implementation | Agent responds: "We haven't agreed on scope yet. Let's finish brainstorming, then move to spec. Ready to continue?" |
| Context files missing | If `.agent/overview.md` or `.agent/mem_library/**` are missing, agent proceeds with available context and notes the gap. |
| User abandons brainstorming mid-dialogue | No recovery needed. Brainstorming is a dialogue; if the user changes topic, the agent adapts. |

## 11. Testing Requirements

This is a workflow skill — it defines agent behavior during dialogue. Testing is manual/behavioral, not automated code tests.

- **T-001: Dialogue flow.** Run a manual session: "I want to add a debugging skill to Iris." Verify the agent: (a) loads iris-brainstorming, (b) inspects existing skills and rules, (c) asks one question at a time, (d) proposes 2-3 approaches, (e) produces a Brainstorm Output at the end.

- **T-002: Architecture violation prevention.** During a session, propose a design that violates Iris architecture (e.g., "put the debug logic directly in Desktop ViewModels"). Verify the agent flags the violation with a reference to the specific Iris rule.

- **T-003: Multi-subsystem detection.** During a session, say "I want to add debugging, TDD, and branch completion skills." Verify the agent flags this as multiple independent subsystems and proposes decomposition.

- **T-004: Handoff to spec.** After a completed brainstorming session with a Brainstorm Output, ask the agent to create a spec. Verify the spec agent uses the Brainstorm Output as context.

- **T-005: No file creation.** During a brainstorming session, check that no files were created in `.opencode/skills/` or `docs/`.

- **T-006: Stage selection integration.** Ask "let's brainstorm a new feature." Verify the agent selects the brainstorming stage (not spec, not design).

## 12. Documentation and Memory Requirements

After implementation:
- Update `.agent/PROJECT_LOG.md` with the completed iteration.
- Update `.agent/overview.md` if brainstorming becomes the current active work.

No other documentation changes required.

## 13. Acceptance Criteria

- [ ] `iris-brainstorm/SKILL.md` exists at `.opencode/skills/iris-brainstorm/SKILL.md` with valid YAML frontmatter.
- [ ] The skill loads correctly when referenced (no parse errors).
- [ ] `AGENTS.md` workflow includes `Brainstorm` before `Spec`.
- [ ] `AGENTS.md` skills list includes `iris-brainstorming`.
- [ ] `iris-engineering/SKILL.md` stage selection table includes a row for brainstorm with correct routing.
- [ ] `iris-engineering/SKILL.md` workflow stages table includes brainstorm row.
- [ ] When invoked, the skill produces exactly one question per response during dialogue.
- [ ] When invoked, the skill proposes 2-3 approaches with trade-offs.
- [ ] When invoked, the skill produces a Brainstorm Output on user confirmation.
- [ ] The skill never creates, edits, or deletes files during normal operation.
- [ ] The skill never proposes approaches that violate Iris architecture boundaries.
- [ ] Manual test T-001 passes (dialogue flow).
- [ ] Manual test T-002 passes (architecture violation prevention).
- [ ] Manual test T-003 passes (multi-subsystem detection).
- [ ] All existing Iris skills continue to load and function without regression.

## 14. Open Questions

No blocking open questions.
