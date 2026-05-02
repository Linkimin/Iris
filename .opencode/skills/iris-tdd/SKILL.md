---
name: iris-tdd
description: Test-Driven Development methodology for Iris. Red-Green-Refactor cycle adapted to .NET/C#/xUnit. Enforces test-first discipline during implementation. No production code without a failing test first.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: implementation
  output_type: tdd_cycle_report
---

# Iris TDD Skill

## Purpose

Use this skill to enforce Test-Driven Development during `/implement`. TDD is not a separate workflow stage — it is the methodology for writing production code inside implementation. When the `/implement` command loads this skill alongside `iris-engineering` and `implement`, every code change follows the Red-Green-Refactor cycle.

This skill adapts the Superpowers `test-driven-development` methodology to Iris, with .NET/C#/xUnit-specific commands, Iris layer-aware test placement, architecture constraint verification, and rationalization defense referencing real Iris failures (P1-001, T-04).

## Iron Law

**NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST.**

If you write production code before the test, delete it. Start over with RED. "Keep as reference" is not allowed. "Adapt existing code" is not allowed. Delete means delete.

The P1-001 timer cancellation regression was caused by exactly this violation: production code was refactored first, existing tests passed for incidental timing reasons, and the cancellation invariant was silently broken. A RED-first test would have caught it in 30 seconds.

## When to Use

**Always** during `/implement` when writing or changing production code:

- New features.
- Bug fixes.
- Refactoring with behavior change.
- Any `.cs` file change that affects behavior.

**Do NOT use TDD for:**

- Configuration files (`appsettings.json`, `.csproj` edits without behavior change).
- Documentation or Markdown changes.
- Project reference additions (no test needed for `<ProjectReference>`).
- Format-only changes (use `dotnet format`).
- Throwaway exploration code — but throw it away before starting real work.

## The Red-Green-Refactor Cycle

```
RED → Verify RED → GREEN → Verify GREEN → REFACTOR → Repeat
```

Each cycle produces one small verified increment. Every step uses .NET/xUnit/Iris-specific commands.

### RED — Write a Failing Test

1. **Choose the test project** — use the test placement table below.
2. **Write exactly one test** — one behavior, one `[Fact]` or `[Theory]`.
3. **Use a clear name:** `MethodName_Scenario_ExpectedBehavior`.
4. **Follow AAA:** Arrange (setup), Act (call the method), Assert (verify result).
5. **Use FluentAssertions:** `.Should().Be(...)`, `.Should().Throw<T>()`.
6. **Prefer real code over mocks** — use mocks/stubs only for external boundaries (adapters, providers).

```csharp
[Fact]
public async Task SendMessageAsync_EmptyInput_ReturnsValidationError()
{
    // Arrange
    var handler = new SendMessageHandler(/* fakes for ports */);
    var options = new SendMessageOptions { Input = "" };

    // Act
    var result = await handler.HandleAsync(options, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("empty");
}
```

### Verify RED — Watch It Fail

**MANDATORY. Never skip.**

```powershell
dotnet test <test-project> --filter "FullyQualifiedName~TestName" --no-build
```

Confirm:
- Test **fails** (assertion failure, not compile/runtime error).
- Failure message is the expected one (feature missing, not typo).
- Test does **NOT pass** on first run — if it passes, the test tests existing behavior; fix the test.
- Test does **NOT error** — if compile/runtime error, fix the test until it fails correctly.

### GREEN — Minimal Code

Write the simplest code to pass the test. No more.

- No over-engineering: no extra parameters, no YAGNI violations.
- No unrelated refactoring: stay exactly within the scope of the test.
- No other files changed unless necessary for the test to compile and pass.

### Verify GREEN — Watch It Pass

**MANDATORY.**

```powershell
# First: focused
dotnet test <test-project> --filter "FullyQualifiedName~TestName" --no-build

# Then: full suite
dotnet test .\Iris.slnx

# Then: build
dotnet build .\Iris.slnx

# Then: format
dotnet format .\Iris.slnx --verify-no-changes
```

Confirm:
- Target test passes.
- ALL other tests still pass (no regressions).
- Build: 0 errors, 0 warnings.
- Format: 0 violations.

**Test fails?** Fix code, not the test.
**Other tests fail?** Fix code — existing tests are the regression contract.
**Format violations?** Run `dotnet format`, re-verify.

### REFACTOR — Clean Up

After green only:

- Remove duplication.
- Improve names.
- Extract helpers.
- Keep tests green — rerun `dotnet test .\Iris.slnx` after each step.
- Do NOT add new behavior during refactor.

### Repeat

Next failing test for the next behavior. Each cycle is one verified increment.

## Test Placement by Layer

Tests must go in the correct test project for the layer being changed:

| Source Layer | Test Project | Example |
|---|---|---|
| `Iris.Domain` | `tests/Iris.Domain.Tests/` | Entity invariants, value objects |
| `Iris.Application` | `tests/Iris.Application.Tests/` | Use cases, handlers, policies, ports (with fakes/stubs) |
| `Iris.Persistence` | `tests/Iris.IntegrationTests/` | Repository integration, SQLite in-memory |
| `Iris.ModelGateway` | `tests/Iris.IntegrationTests/` | Provider client calls (may need real provider) |
| `Iris.Infrastructure` | `tests/Iris.Infrastructure.Tests/` | Utility/service tests |
| `Iris.Desktop` (ViewModels) | `tests/Iris.IntegrationTests/` | Headless xUnit ViewModel tests |
| Architecture rules | `tests/Iris.Architecture.Tests/` | Project reference checks, dependency direction |

If a test project doesn't exist for a layer, ask: "Which existing test project should this test go in?" Do NOT create new test projects.

## Architecture-Aware Testing

Every test must respect Iris boundaries:

- **Domain tests:** Never reference EF Core, HTTP, UI, providers, or Application.
- **Application tests:** Use fakes/stubs for all ports; never reference concrete adapter projects (`Iris.Persistence`, `Iris.ModelGateway`).
- **Adapter tests:** Test the adapter's own behavior (mapping, HTTP calls, DI registration). Do NOT test Application orchestration.
- **Architecture tests:** Verify project references and dependency direction.
- **Integration tests:** May cross layers; must not violate dependency direction.

## TDD Cycle Report

After each complete cycle, produce a brief report:

```markdown
## TDD Cycle Report: <Change>

### RED
- Test: `MethodName_Scenario_ExpectedBehavior`
- Project: `tests/Iris.Domain.Tests/`
- Expected failure: ...

### Verify RED
- Command: `dotnet test --filter "FullyQualifiedName~..." --no-build`
- Result: FAILED — <actual failure message>

### GREEN
- File: `src/Iris.Domain/.../Entity.cs`
- Change: <one sentence>

### Verify GREEN
- Focused test: PASSED
- Full suite: 126 passed, 0 failed
- Build: 0 errors, 0 warnings
- Format: 0 violations

### REFACTOR
- <none or list of cleanup changes>
```

## Rationalization Defense

When tempted to skip TDD, consult this table:

| Rationalization | Correct Response |
|---|---|
| "This change is too simple to test." | Simple code breaks. P1-001 was "just a timer refactor" — it broke cancellation. Test takes 30 seconds. |
| "I'll add tests after implementation." | Tests-after pass immediately — they prove nothing. T-04 survived because it was written after. You don't know if it tests the right thing. |
| "Already manually verified." | Manual verification has no record, can't re-run, misses edge cases. The timer cancellation bug was invisible until formally tested. |
| "The existing code has no tests." | You're improving it. Add characterization tests for existing behavior first, then modify. |
| "I need to explore the design first." | Fine. Throw away the exploration code. Start fresh with RED. |
| "Writing the test first will slow me down." | TDD is faster than debugging. P1-001 took 3 fix attempts and a full audit to catch what a failing test would have caught in 30 seconds. |
| "This is a bug fix — I know the cause." | The P1-001 bug was "understood" as a timer issue. The actual root cause was a dead CancellationTokenSource. The test-first cycle forces you to prove you understand the bug. |
| "TDD is dogmatic. I'm being pragmatic." | TDD IS pragmatic: finds bugs before commit, prevents regressions, documents behavior, enables safe refactoring. "Pragmatic" shortcuts = debugging in production = slower. |
| "Tests after achieve the same goals." | Tests-after = "what does this do?" Tests-first = "what should this do?" Tests-after are biased by implementation. |

## Integration with implement

TDD operates within the `/implement` workflow. For each plan step:

1. Plan step → RED (write failing test for the step's behavior)
2. Verify RED → GREEN (minimal production code)
3. Verify GREEN → REFACTOR → next plan step

The TDD cycle does NOT replace the plan. It enforces how each plan step is implemented. If a plan step covers multiple behaviors, run one TDD cycle per behavior.

## Stop Conditions

| Trigger | Action |
|---|---|
| Production code written before test | **Delete the code.** Start over with RED. No exceptions. |
| Test passes on first run | The test tests existing behavior. Fix the test — the feature is not yet implemented. |
| Test errors (compile/runtime) | Fix the test setup until it fails with the expected assertion failure. |
| GREEN code breaks other tests | Fix the production code, NOT the existing tests. They are the regression contract. |
| Format violations after GREEN | Run `dotnet format`, re-verify GREEN. |
| Agent tries to refactor before GREEN | Stop. REFACTOR only after all tests are green. |
| Agent adds behavior during REFACTOR | Stop. REFACTOR is cleanup only — no new functionality. |
| User says "skip TDD just this once" | Engage rationalization defense. If user insists, proceed but flag risk with P1-001 citation. |

## Quality Checklist

Before concluding a cycle, verify:

- [ ] Test was written before production code.
- [ ] RED verification was run and test failed for the expected reason.
- [ ] GREEN code is minimal — no over-engineering, no unrelated changes.
- [ ] GREEN verification passed: focused test, full suite, build, format.
- [ ] No other tests regressed.
- [ ] Architecture boundaries respected in both test and production code.
- [ ] TDD Cycle Report produced with all sections.
- [ ] REFACTOR did not add new behavior.

Can't check all boxes? You skipped TDD. Start over with RED.

---

✅ TDD Cycle Complete — Test-first discipline enforced.
