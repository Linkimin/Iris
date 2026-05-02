---
name: iris-brainstorm
description: Pre-spec collaborative scoping dialogue. Use when exploring a non-trivial idea before committing to a specification. One question at a time, approach comparison, architecture-aware.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: brainstorming
  output_type: brainstorm_output
---

# Iris Brainstorm Skill

## Purpose

Use this skill to run a structured pre-spec collaborative dialogue in Iris.

Brainstorming is an available stage before `spec`. It explores the user's intent through structured dialogue — one question at a time, approach comparison, incremental validation — and produces enough clarity for specification writing. It never produces code, never edits files, and never updates memory.

The brainstorming stage prevents the `spec` stage from receiving ambiguous or premature input. Instead of the agent inventing assumptions mid-spec or bundling clarifying questions, brainstorming separates exploration from specification.

This skill is adapted from the Superpowers `brainstorming` concept but is Iris-specific: it respects Iris Clean/Hexagonal architecture, Iris stage separation, and the prohibition on speculative files.

## When to Use

**Activate this skill when the user expresses a non-trivial intent that has no spec yet.** Trigger phrases include:

- "let's think about..."
- "I want to add..."
- "let's brainstorm..."
- "explore the idea of..."
- "how should we..."
- "what approach would work for..."
- "let's figure out..."
- any ambiguous scope request where the correct next stage is not obvious

**Do NOT use brainstorming when:**

- the user has a clear, specific request that can go directly to `spec` (e.g., "spec a login page");
- the user explicitly asks for a spec, design, plan, or implementation;
- the user asks a factual question (use `/status` or direct answer);
- the user reports a bug or failure (use `iris-debug` or standard debugging);
- the user asks for verification (use `/verify`);
- the user asks for review (use `/review`).

Brainstorming is optional. If the user says "skip to spec", honor that. Never force brainstorming on a user who is ready for specification.

## Required Context

Before asking the user anything, inspect:

1. `AGENTS.md` — understand the project operating model.
2. Relevant `.opencode/rules/*.md` — especially `iris-architecture.md` and `no-shortcuts.md` for boundary rules.
3. `.agent/overview.md` — understand current project phase and status.
4. Relevant `.agent/mem_library/**` — understand existing design decisions that may constrain the approach.

If context files are missing, proceed with available context and note the gap. Do not let missing context block the dialogue.

## Dialogue Rules

Brainstorming follows a strict structure. Every response from the agent must respect these rules.

### One Question at a Time

**Rule (FR-003):** Ask exactly one clarifying question per response. Do not bundle multiple questions. Wait for the user's answer before proceeding.

Prefer multiple-choice questions when the options are enumerable — it is faster than open-ended questions. But do not force multiple choice when the question genuinely needs an open-ended answer.

**Example (good):**

> Before I propose approaches: should this work through existing Iris skills, or could it require a new adapter?

**Example (bad):**

> What should the scope be? What are the non-goals? Which layers are affected? What about testing?

### Approach Comparison

**Rule (FR-004):** After sufficient clarification, propose 2–3 distinct approaches with explicit trade-offs. At least one approach must be minimal/iterative (smallest vertical slice that proves the concept). Recommend one approach with reasoning, but let the user decide.

Every approach must include:

- What it looks like (a few sentences).
- Which Iris layers/projects it affects.
- Trade-offs: pros and cons.
- Risk level and estimated effort.

Do not propose approaches that violate Iris architecture. See "Architecture-Aware Dialogue" section below.

### Incremental Validation

**Rule (FR-005):** After each section of scoping (problem summary, scope, non-goals, approach), ask whether it looks right so far before proceeding. The user can revise direction at any point.

### Scope Decomposition Detection

**Rule (FR-006):** If the user's intent describes multiple independent subsystems, flag this immediately and propose decomposition before refining details.

**Example:**

> This sounds like 3 independent pieces: <list them briefly>. Let's focus on one first. Which is your priority?

Match the Superpowers practice but use Iris terminology.

## Brainstorm Output Format

**Rule (FR-007):** When the user confirms the direction, produce a concise Brainstorm Output in this exact structure:

```markdown
## Brainstorm Output: <Topic>

### Problem Summary
<One paragraph describing the problem or opportunity.>

### Agreed Scope
- In scope: <itemized list>
- Out of scope: <itemized list>
- Non-goals: <itemized list>

### Selected Approach
<Brief description of the chosen approach and rationale for selection over alternatives.>

### Architecture Notes
<Which Iris layers are affected, which boundaries must be preserved, which forbidden shortcuts apply (reference Architecture-Aware Dialogue rules).>

### Open Questions
<Questions not resolved during brainstorming, if any. If none, state "None — ready to proceed.">

### Recommended Next Stage
<Typically `spec`. May be `design` if the topic is already well-scoped and only structure/contracts need definition.>
```

## Handoff Rules

**Rule (FR-008):** The Brainstorm Output serves as input to `spec`. The spec agent uses it as context (scope, non-goals, constraints) but starts its own independent analysis.

Brainstorm output is NOT a substitute for spec. It does not satisfy Gate A.

When handing off, respond with:

> Brainstorm complete. The next stage is `spec`. Paste the Brainstorm Output as input to `/spec <this topic>`.

If the user directly asks "now create a spec", proceed to the spec stage using the Brainstorm Output as context.

## Read-Only Guarantee

**Rule (FR-009):** This skill must never:

- edit files;
- create files;
- delete files;
- run destructive commands;
- update memory (`.agent/` files);
- produce implementation code;
- generate specs, designs, or plans.

It operates entirely through dialogue and the final Brainstorm Output in the conversation. The only exception is explicit saving (see Saving Policy).

## Saving Policy

**Rule (FR-010):** Do not automatically save the Brainstorm Output. If the user explicitly asks to save it, the agent may save it to a path the user specifies (e.g., `docs/specs/brainstorm-YYYY-MM-DD-topic.md`). Do not assume a path. Ask for confirmation.

## Architecture-Aware Dialogue

**Rule (AC-005):** Every approach proposed during brainstorming must respect Iris architecture boundaries. The agent must actively check:

| Layer | Owns | Must NOT contain |
|---|---|---|
| `Iris.Domain` | Entities, value objects, invariants, domain concepts | EF Core, HTTP, UI, providers, processes |
| `Iris.Application` | Use cases, ports, orchestration, prompt/context assembly | Concrete adapters, EF Core, provider SDKs, Avalonia |
| `Iris.Shared` | Reusable primitives, guards, result types, IDs, clocks | Iris product/domain behavior |
| Adapters (`Persistence`, `ModelGateway`, etc.) | Implement Application ports | Workflow decisions, chat orchestration (own only adapter concerns) |
| Hosts (`Desktop`, `Api`, `Worker`) | Composition root, DI wiring | Business logic, prompt logic, persistence logic |

**Forbidden shortcut patterns to explicitly flag:**

- "Put the logic in Desktop ViewModels" → business logic goes in Application/Domain.
- "Call the database directly from the UI" → go through Application → Persistence.
- "Add a reference from Domain to EF Core" → Domain must remain pure.
- "Have the tool adapter make permission decisions" → Application owns permission policy.
- "Use Shared as a dumping ground" → Shared stays product-neutral.

When an approach proposed by the user would violate a boundary, the agent must:

1. Identify the specific boundary and rule.
2. Explain why it matters (coupling, testability, long-term drift).
3. Propose an architecture-compliant alternative.
4. Ask whether the user accepts the constraint or wants to reconsider.

## Stop Conditions

Stop or redirect the brainstorming dialogue when:

- **User asks for code:** Respond with — "That belongs in `implement`. First, let's finish scoping. Ready to move to spec?"
- **User tries to skip to implementation:** Respond with — "We haven't agreed on scope yet. Let's finish brainstorming. Ready to move to `spec`?"
- **User gives contradictory constraints:** Flag the contradiction — "Earlier you said X, now you're saying Y. Which takes priority?" — and wait for clarification.
- **User's idea violates Iris architecture:** Explain which boundary, why it matters, and propose a compliant alternative.
- **User abandons the topic:** No special recovery. Adapt naturally to the new topic.

## Anti-Patterns

The brainstorming skill must never:

- create spec, design, or plan files during dialogue;
- produce code or implementation instructions;
- skip the approach comparison step (always propose 2–3);
- bundle multiple questions in one response;
- propose approaches that violate Iris architecture boundaries;
- automatically save output to `docs/` or `.opencode/`;
- update `.agent/` memory files;
- claim the output satisfies Gate A (it does not);
- replace `spec` — it is a pre-spec stage, never a substitute.

## Quality Checklist

Before concluding a brainstorming session, verify:

- [ ] Context files were inspected before dialogue started.
- [ ] Questions were asked one at a time.
- [ ] 2–3 approaches were proposed with trade-offs.
- [ ] Every proposed approach respects Iris architecture boundaries.
- [ ] Incremental validation was offered at key checkpoints.
- [ ] Multi-subsystem decomposition was flagged if applicable.
- [ ] A Brainstorm Output was produced with all required sections.
- [ ] No files were created, edited, or deleted.
- [ ] No memory was updated.
- [ ] The recommended next stage is explicit.
- [ ] The handoff to spec is clear.

(This checklist is for the agent's internal quality control. Show the "✅ Read-Only Guarantee: No files were modified" note at the end of the brainstorming session.)
