# Specification: Skill iris-tdd

## 1. Problem Statement

Iris workflow has `spec`, `design`, `plan`, and `implement` stages. The `implement` skill describes what to edit and how to verify, but it does not enforce a testing discipline. The agent can write production code first and then add tests — or skip tests entirely for "trivial" changes.

This creates a pattern identical to the one the Superpowers `test-driven-development` skill was designed to prevent:

- **Test-after code:** Tests pass immediately on first run, proving nothing. The agent may test what it built, not what was required. Edge cases are forgotten. The P1-001 regression was a direct consequence — the refactor passed existing tests for incidental reasons, while the cancellation invariant was silently broken.
- **No test at all:** "Trivial" changes accumulate. The codebase accumulates untested paths that later break during refactoring. T-04 flaky headless test survived because tests were added after implementation and missed the Avalonia dispatcher dependency.
- **Rationalization cascade:** "It's simple", "I'll test later", "Already manually verified". Each rationalization is individually plausible; together they hollow out test coverage.

The Superpowers `test-driven-development` skill demonstrates that a strict Red-Green-Refactor cycle — test first, watch it fail, minimal code, refactor — prevents these failures. Iris needs its own version adapted to:

- **.NET / C# / xUnit:** `dotnet test --filter`, `[Fact]` / `[Theory]`, AAA pattern, FluentAssertions, Moq/NSubstitute.
- **Iris Clean/Hexagonal test layering:** Domain tests (pure), Application tests (fakes/stubs), Adapter tests (real implementations), Architecture tests (boundary rules), Integration tests (composed behavior).
- **Iris verification commands:** `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`.
- **Iris stage separation:** TDD operates during `/implement`, not as a separate stage. It is a methodology integrated into the implementation workflow.

## 2. Goal

Add an `iris-tdd` workflow skill that:
- lives in `.opencode/skills/iris-tdd/SKILL.md`;
- activates when the user runs `/implement` with an approved plan, or explicitly requests TDD;
- enforces the Red-Green-Refactor cycle adapted to .NET/C#/xUnit and Iris architecture:
  1. **RED** — write a failing test in the correct test project (Domain/Application/Adapter/Architecture/Integration);
  2. **Verify RED** — run `dotnet test --filter` and confirm the test fails for the expected reason;
  3. **GREEN** — write minimal production code to pass the test;
  4. **Verify GREEN** — run `dotnet test` and confirm the test and all existing tests pass;
  5. **REFACTOR** — clean up code while keeping all tests green;
- enforces the Iron Law: **no production code without a failing test first**;
- integrates into Iris layers: the test must be in the correct test project for the layer being changed;
- includes Iris-specific rationalization defense referencing real Iris failures (P1-001, T-04);
- produces a TDD Cycle Report: RED test, RED verification output, GREEN code summary, GREEN verification output, refactor changes;
- never creates production code before test code — the skill enforces order, even for "trivial" changes.

## 3. Scope

### In Scope

- A new `.opencode/skills/iris-tdd/SKILL.md` file.
- Red-Green-Refactor cycle adapted to .NET/xUnit Iris conventions:
  - RED: write one `[Fact]` or `[Theory]`, one behavior, clear name, AAA pattern.
  - Verify RED: `dotnet test --filter "FullyQualifiedName~TestName"`, confirm failure reason.
  - GREEN: minimal production code, no over-engineering, no unrelated changes.
  - Verify GREEN: `dotnet test` (full suite), `dotnet build`, `dotnet format --verify-no-changes`.
  - REFACTOR: remove duplication, improve names, extract helpers — keep tests green.
- Iron Law enforcement: no production code before a failing test.
- Iris-specific test placement rules: which test project for each source layer.
- Rationalization defense table with Iris-specific counter-examples (P1-001, T-04).
- Integration with `implement` skill: the implement skill references `iris-tdd` as its testing methodology.
- Integration into `AGENTS.md` skills list.
- Integration with gate checks: TDD does not change the gate system — it operates inside Gate D (verification) and is confirmed by `/verify`.

### Out of Scope

- Changing `.opencode/rules/workflow.md` — TDD is a methodology within `/implement`, not a new gate.
- Changing the `implement` skill itself (it may reference `iris-tdd` in a future phase, but that is not part of this spec).
- Creating a new test project.
- Test data generation tools, snapshot testing frameworks, property-based testing.
- Performance/load testing, mutation testing, code coverage tools.
- Automated refactoring tools.

### Non-Goals

- Replicating Superpowers `test-driven-development` exactly. Iris-tdd adapts the cycle to .NET/C#/xUnit with Iris layer-specific test placement.
- Making TDD a separate workflow stage. It is a methodology inside `/implement`.
- Enforcing TDD for configuration files, project references, or Markdown documentation changes — those are explicitly excluded.

## 4. Current State

### Existing Test Infrastructure

Iris has 5 test projects organized by layer:

| Test Project | Tests for | Pattern |
|---|---|---|
| `tests/Iris.Domain.Tests/` | Domain entities, value objects, invariants | `[Fact]` / `[Theory]`, AAA |
| `tests/Iris.Application.Tests/` | Use cases, handlers, policies, ports | Fakes/stubs for abstractions |
| `tests/Iris.Architecture.Tests/` | Project references, dependency direction | Boundary rule tests |
| `tests/Iris.IntegrationTests/` | Composed behavior across layers | Desktop ViewModel tests, persistence integration |
| `tests/Iris.Infrastructure.Tests/` | Infrastructure/utility tests | Unit tests for infrastructure |

### Existing Test Conventions

- Framework: xUnit (`[Fact]`, `[Theory]`, `[InlineData]`).
- Assertions: FluentAssertions (`.Should().Be()`, `.Should().Throw<T>()`).
- Pattern: AAA (Arrange-Act-Assert).
- Naming: `MethodName_Scenario_ExpectedBehavior` (e.g., `SendMessageAsync_EmptyInput_ReturnsError`).
- Verification: `dotnet test .\Iris.slnx`, `dotnet test --filter "FullyQualifiedName~TestName"`.

### Existing Implement Skill (Current State)

The `implement` skill already says:
- "Tests must be added or updated where behavior changes."
- It does NOT enforce RED before GREEN.
- It does NOT require watching the test fail.
- It allows writing production code first, then tests.

### Known TDD Gaps Observed

- P1-001 (timer cancellation): Production code was refactored first. The existing test (T-07) passed for incidental reasons (timing), not because it tested the cancellation invariant. A RED-first test would have failed until the cancellation token was properly wired.
- T-04 (flaky headless): Test was written after implementation and missed the `Dispatcher.UIThread.Post` Avalonia dependency in the headless xUnit environment.

## 5. Affected Areas

| Area | Impact |
|---|---|
| `.opencode/skills/iris-tdd/SKILL.md` | New file — primary artifact |
| `AGENTS.md` | Update skills list: add `iris-tdd` |
| `.opencode/skills/implement/SKILL.md` | May reference `iris-tdd` as methodology (out of scope for this spec; future phase) |
| `.opencode/commands/implement.md` | May reference `iris-tdd` (out of scope for this spec; future phase) |

Files explicitly NOT affected:
- `.opencode/rules/workflow.md` — TDD is a methodology within `/implement`, not a new gate.
- Iris source code, tests, project references, configuration.
- Any `.csproj`, `.slnx`, `.cs` files.

## 6. Functional Requirements

- **FR-001: Activation context.** The skill activates when the agent is inside `/implement` and about to write production code. It also activates when the user explicitly requests TDD: "use TDD", "test-first", "write tests first". The `implement` skill or command delegates to `iris-tdd` for the test-code cycle.

- **FR-002: Iron Law.** The skill enforces: **NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST.** If the agent writes production code before the test, the code must be deleted and the cycle started fresh. "Keep as reference" is not allowed. "Adapt existing code" is not allowed. Delete means delete.

- **FR-003: RED — Write a failing test.** The agent must:
  1. Identify the correct test project for the layer being changed (Domain → `Domain.Tests`, Application → `Application.Tests`, Adapter → `IntegrationTests` or adapter-specific tests, Architecture → `Architecture.Tests`).
  2. Write exactly one test — one behavior, one `[Fact]` or `[Theory]`.
  3. Use a clear name: `MethodName_Scenario_ExpectedBehavior`.
  4. Follow AAA: Arrange (set up test data), Act (call the method), Assert (verify result).
  5. Use FluentAssertions for readability.
  6. Prefer real code over mocks; use mocks/stubs only for external boundaries (adapters, providers).

- **FR-004: Verify RED — Watch it fail.** The agent must run:
  ```powershell
  dotnet test <test-project> --filter "FullyQualifiedName~<TestName>" --no-build
  ```
  And confirm:
  - Test fails (not errors with a compile/runtime crash).
  - Failure message is the expected one (feature missing, not typo).
  - Test does NOT pass on first run — if it passes, the test tests existing behavior; fix the test.
  - Test does NOT error — if it errors, fix the test until it fails correctly.

- **FR-005: GREEN — Minimal production code.** The agent must:
  1. Write the simplest code to pass the test — no more.
  2. No over-engineering: no extra parameters, no future-proofing, no YAGNI violations.
  3. No unrelated refactoring: stay exactly within the scope of the test.
  4. No other files changed: only the file needed to make the test pass.

- **FR-006: Verify GREEN — Watch it pass.** The agent must run:
  ```powershell
  dotnet test <test-project> --filter "FullyQualifiedName~<TestName>" --no-build
  ```
  Then, if green:
  ```powershell
  dotnet test .\Iris.slnx
  ```
  And confirm:
  - The target test passes.
  - ALL other tests still pass (no regressions).
  - Build is clean: `dotnet build .\Iris.slnx` with 0 errors, 0 warnings.
  - Format is clean: `dotnet format .\Iris.slnx --verify-no-changes` with 0 violations.

- **FR-007: REFACTOR — Clean up.** After green only:
  1. Remove duplication.
  2. Improve names.
  3. Extract helpers.
  4. Keep all tests green — rerun `dotnet test .\Iris.slnx` after each refactor step.
  5. Do NOT add new behavior during refactor.

- **FR-008: Repeat.** After refactor, the cycle repeats with the next failing test for the next behavior. Each cycle produces one small verified increment.

- **FR-009: Test Placement by Layer.** The skill must include a table mapping Iris source layers to their correct test projects:

  | Source Layer | Test Project |
  |---|---|
  | `Iris.Domain` | `tests/Iris.Domain.Tests/` |
  | `Iris.Application` | `tests/Iris.Application.Tests/` |
  | `Iris.Persistence` | `tests/Iris.IntegrationTests/` (or dedicated persistence tests) |
  | `Iris.ModelGateway` | `tests/Iris.IntegrationTests/` (or dedicated adapter tests) |
  | `Iris.Perception`, `Iris.Tools`, `Iris.Voice` | Appropriate adapter test project |
  | `Iris.Infrastructure` | `tests/Iris.Infrastructure.Tests/` |
  | `Iris.Desktop` (ViewModels) | `tests/Iris.IntegrationTests/` (headless xUnit) |
  | Architecture/boundary rules | `tests/Iris.Architecture.Tests/` |

- **FR-010: Architecture-Aware Testing.** The skill must ensure:
  - Domain tests never reference EF Core, HTTP, UI, or providers.
  - Application tests use fakes/stubs for all adapters; never reference concrete adapter projects.
  - Adapter tests test the adapter's own behavior (mapping, HTTP calls, DI registration); they do not test Application orchestration.
  - Architecture tests verify project references and dependency direction.

- **FR-011: TDD Cycle Report.** After each complete cycle, the agent produces a brief cycle report:

  ```markdown
  ## TDD Cycle Report: <Change>

  ### RED
  - Test: `<FullyQualifiedName>`
  - Expected failure: ...
  - Actual failure: ...

  ### GREEN
  - File changed: `<path>`
  - Change summary: <one sentence>

  ### VERIFY
  - Build: Passed / Failed
  - Tests: X passed, 0 failed
  - Format: 0 violations

  ### REFACTOR
  - Changes: <none or list>
  ```

- **FR-012: Rationalization Defense.** The skill must include a rationalization table with Iris-specific counter-examples:

  | Rationalization | Correct Response |
  |---|---|
  | "This change is too simple to test." | Simple code breaks. P1-001 was "just a timer refactor" — it broke cancellation. Test takes 30 seconds. |
  | "I'll add tests after implementation." | Tests-after pass immediately — they prove nothing. T-04 survived because it was written after. You don't know if it tests the right thing. |
  | "Already manually verified." | Manual verification has no record, can't re-run, misses edge cases. The timer cancellation bug was invisible until the invariant was formally tested. |
  | "The existing code has no tests." | You're improving it. Add tests for existing code first (characterization tests), then modify. |
  | "I need to explore the design first." | Fine. Throw away the exploration code. Then start fresh with RED. |
  | "Writing the test first will slow me down." | TDD is faster than debugging. P1-001 took 3 fix attempts and a full audit to catch what a failing test would have caught in 30 seconds. |
  | "This is a bug fix — I know the cause." | The P1-001 bug was "understood" as a timer issue. The actual root cause was a dead CancellationTokenSource. The test-first cycle forces you to prove you understand the bug. |

- **FR-013: Stop conditions.** The skill must enforce stop conditions:
  - Production code written before test → delete code, start over with RED.
  - Test passes on first run → the test doesn't test new behavior. Fix the test.
  - Test errors (compile/runtime) → fix the test until it fails correctly, then proceed to GREEN.
  - Other tests break during GREEN → fix the production code, not the test. The existing tests are the contract.
  - Format violations during GREEN → fix formatting before continuing to REFACTOR.

- **FR-014: Integration with implement skill.** The `implement` skill may reference `iris-tdd` as its testing methodology. When both skills are active, the cycle order is: Plan step → RED → Verify RED → GREEN → Verify GREEN → REFACTOR → next Plan step. This spec does not change the `implement` skill itself — that is a follow-up integration.

## 7. Architecture Constraints

- **AC-001: Skill file location.** `iris-tdd/SKILL.md` lives in `.opencode/skills/`. Follows existing Iris skill YAML frontmatter convention.

- **AC-002: No dependency on Superpowers.** References Superpowers `test-driven-development` only as conceptual inspiration. No import, load, or delegation.

- **AC-003: Methodology within /implement.** TDD is NOT a separate workflow stage. It does not add a new row to the stage selection table. It operates inside `/implement` as the methodology for writing code.

- **AC-004: Does not change gates.** TDD does not add, remove, or modify audit gates (A-G). It enforces a testing discipline inside Gate D (verification). The RED/GREEN verification steps produce evidence that `/verify` can inspect.

- **AC-005: Layer test isolation.** Each test must be in the correct test project for its layer. Domain tests must not import Application. Application tests must not reference concrete adapters. This mirrors the Iris dependency direction rules.

- **AC-006: No new test projects.** The skill works with the existing 5 test projects. It must not propose creating new test projects unless a future design explicitly authorizes it.

- **AC-007: Read-only verification commands during TDD.** The skill may run `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`. It may edit source and test files as part of the cycle. It must not run destructive commands.

## 8. Contract Requirements

No Iris source-code contracts are affected. The only contract changes:

| Contract | Current behavior | Required behavior | Compatibility |
|---|---|---|---|
| `AGENTS.md` skills list | 9 skills (including brainstorm, debug) | 10 skills (adds `iris-tdd`) | Extended |
| `implement` skill methodology | No enforced test-first order | May reference `iris-tdd` as methodology (future) | Future extension |

## 9. Data and State Requirements

No persisted data. No database changes. The only state is the Red-Green-Refactor cycle state within a conversation — each cycle produces one TDD Cycle Report.

## 10. Error Handling and Failure Modes

| Failure mode | Required behavior |
|---|---|
| Agent produces code before test | Iron Law enforcement: delete the code, start over with RED. No "keep as reference". |
| Test passes on first run | Test tests existing behavior — fix the test. The feature is not yet implemented. |
| Test errors (compile error, runtime crash in test code) | Fix the test setup until it fails with the expected assertion failure, not an error. |
| GREEN code breaks existing tests | Fix the production code, not the existing tests. The existing tests are the regression contract. |
| Format violations after GREEN | Run `dotnet format` to fix formatting, then re-verify GREEN. |
| Build failure after GREEN | Fix the build, then re-verify GREEN. |
| Agent tries to refactor before GREEN | Stop. REFACTOR only after all tests are green. |
| Agent adds behavior during REFACTOR | Stop. REFACTOR is cleanup only — no new functionality. |
| Test project not found for the target layer | Stop and ask: "Which test project should this test go in?" |
| User says "skip TDD just this once" | The rationalization defense table engages. If user insists, proceed but flag the risk with P1-001 citation. |

## 11. Testing Requirements

This is a workflow skill — testing is manual/behavioral.

- **T-001: Full TDD cycle.** Simulate: "Implement SendWithRetryAsync in Application." Agent: (a) writes failing test in `Application.Tests`, (b) runs `dotnet test --filter`, confirms RED, (c) writes minimal production code, (d) runs full `dotnet test`, confirms GREEN, (e) produces TDD Cycle Report.
- **T-002: Iron Law enforcement.** Agent writes a line of production code before any test. Agent must: delete the line, write the test first.
- **T-003: Test placement.** Agent is implementing a Domain entity change. Agent must place the test in `tests/Iris.Domain.Tests/`, not in `IntegrationTests`.
- **T-004: RED verification.** Test passes on first run. Agent must recognize this is wrong — the test tests existing behavior, not new behavior.
- **T-005: Regression guard.** Agent makes GREEN code that breaks another test. Agent must fix the production code, not the broken test.
- **T-006: Multiple cycles.** Implement a feature with 3 behaviors. Agent must run 3 full Red-Green-Refactor cycles, each with a TDD Cycle Report.
- **T-007: Rationalization defense.** User says "it's simple, skip TDD." Agent must engage the rationalization defense table.

## 12. Documentation and Memory Requirements

After implementation:
- Update `.agent/PROJECT_LOG.md` with the completed iteration.
- Update `.agent/overview.md` if TDD becomes the current active work.

## 13. Acceptance Criteria

- [ ] `iris-tdd/SKILL.md` exists at `.opencode/skills/iris-tdd/SKILL.md` with valid YAML frontmatter.
- [ ] The skill loads correctly when referenced (no parse errors).
- [ ] `AGENTS.md` skills list includes `iris-tdd`.
- [ ] The skill contains the Red-Green-Refactor cycle: RED (write failing test) → Verify RED → GREEN (minimal code) → Verify GREEN → REFACTOR (clean up).
- [ ] The skill enforces Iron Law: "NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST."
- [ ] The skill includes a test placement table: Iris layer → correct test project.
- [ ] The skill includes architecture-aware testing rules: Domain tests no EF Core, Application tests no concrete adapters, etc.
- [ ] The skill includes a rationalization defense table with Iris-specific counter-examples (P1-001, T-04).
- [ ] The skill includes .NET-specific commands: `dotnet test --filter "FullyQualifiedName~..."`, `dotnet build`, `dotnet format --verify-no-changes`.
- [ ] The skill produces a TDD Cycle Report after each cycle.
- [ ] The skill enforces stop conditions: code-before-test → delete, test passes first run → fix test, other tests break → fix code.
- [ ] Manual test T-001 passes (full TDD cycle with .NET commands).
- [ ] Manual test T-002 passes (Iron Law enforcement).
- [ ] Manual test T-003 passes (correct test placement by layer).
- [ ] All existing Iris skills continue to load and function without regression.
- [ ] `iris-engineering/SKILL.md` stage selection table is NOT changed (TDD is methodology, not a stage).

## 14. Open Questions

No blocking open questions.
