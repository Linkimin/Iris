---
name: iris-architecture
description: Use when Iris work may affect layers, project references, dependency direction, dependency injection, adapters, hosts, shared abstractions, or forbidden shortcuts.
compatibility: opencode
metadata:
  project: Iris
  workflow_stage: architecture
  output_type: architecture_guidance
---

# Iris Architecture Skill

## Purpose

Use this skill to preserve Iris Clean / Hexagonal architecture.

It guides placement, ownership, dependency direction, and boundary review. Hard prohibitions live in:

- `.opencode/rules/iris-architecture.md`;
- `.opencode/rules/no-shortcuts.md`.

## When To Use

Use this skill when work touches:

- project references;
- dependency injection;
- domain entities or value objects;
- application handlers, ports, policies, or DTOs;
- persistence, model gateway, tools, voice, perception, or SI runtime adapters;
- Desktop, API, or Worker composition;
- shared primitives;
- architecture tests;
- cross-project contracts.

Do not use it for isolated typos or documentation-only edits with no boundary impact.

## Required Context

Inspect:

- `AGENTS.md`;
- `.agent/architecture.md`;
- `.agent/first-vertical-slice.md` when first-slice flow is affected;
- `.opencode/rules/iris-architecture.md`;
- `.opencode/rules/no-shortcuts.md`;
- changed `.csproj` files if project references changed;
- DI registration files if composition changed;
- changed source and tests.

## Dependency Direction

Expected direction:

```text
Shared <- Domain <- Application <- Adapters <- Hosts
```

In practical project references:

- `Iris.Domain` may depend on `Iris.Shared`.
- `Iris.Application` may depend on `Iris.Domain` and `Iris.Shared`.
- Adapters may depend inward on Application/Domain/Shared.
- Hosts compose Application plus adapters.

Application defines ports. Adapters implement ports.

## Placement Decision Table

| Responsibility | Correct owner | Wrong owner signs |
|---|---|---|
| Conversation/message invariants | Domain | depends on EF, HTTP, UI |
| Send message orchestration | Application | calls Ollama/DbContext directly |
| Prompt construction | Application | lives in Desktop or ModelGateway |
| Ollama HTTP request/response | ModelGateway | Application imports provider models |
| SQLite schema/repository | Persistence | UI/Application uses DbContext |
| UI state and bindings | Desktop | ViewModel owns prompt/persistence logic |
| Permission decision policy | Application | Tools approves product-level risk |
| Shell/file execution | Tools | Application starts processes directly |
| Screenshot/clipboard capture | Perception | Desktop P/Invokes Win32 directly |
| STT/TTS/audio devices | Voice | Chat handler records or plays audio |
| Shared Result/Clock/ID primitives | Shared | Shared knows Iris product concepts |

If a responsibility appears in two owners, design the seam before editing.

## Layer Ownership Checks

Check each affected file:

- Domain: pure concepts, invariants, value objects, entities.
- Application: use cases, orchestration, policies, ports, prompt/context assembly.
- Persistence: EF Core, SQLite, entities, mappings, repositories.
- ModelGateway: provider HTTP calls, request/response mapping, routing.
- Tools: technical tool execution and sandbox enforcement.
- Voice: audio, STT/TTS, devices, playback/recording.
- Perception: desktop context, screenshots, clipboard, Win32 isolation.
- Infrastructure: shared technical plumbing only.
- Hosts: UI/API/Worker composition and presentation.
- Shared: neutral primitives only.

If a file has more than one owner, stop and design the boundary.

## Boundary Smell Checklist

Treat these as warning signs:

- `using Microsoft.EntityFrameworkCore` outside Persistence or tests.
- `HttpClient` provider calls outside ModelGateway or approved gateway adapters.
- `Avalonia` references outside Desktop.
- `IrisDbContext` referenced by Desktop/API/Worker/Application.
- `Ollama` concrete types referenced by Desktop/Application.
- Application project reference to an adapter project.
- Adapter-to-adapter project reference.
- Shared type names containing Iris product concepts such as Persona, Memory, Conversation behavior, or ToolPolicy.
- Host constructors containing workflow decisions instead of composition.

## Forbidden Shortcut Checks

Always check for:

- Desktop directly calling database or model providers;
- Application referencing concrete adapters;
- Domain referencing infrastructure;
- Tools deciding product permission policy;
- Voice owning chat orchestration;
- Perception extracting memories;
- Shared gaining Iris product behavior;
- host projects depending on each other.

Use `.opencode/rules/no-shortcuts.md` for the authoritative list.

## First Vertical Slice Checks

For first-slice chat work, verify this intended flow:

```text
ChatView
→ ChatViewModel
→ IrisApplicationFacade
→ SendMessageHandler
→ Application abstractions
→ Persistence / ModelGateway adapters
```

Allowed:

- Desktop composes adapters in DI.
- Desktop ViewModel calls facade.
- Facade calls Application handler.
- Handler uses Application abstractions.
- Adapters implement those abstractions.

Forbidden:

- ViewModel calls `OllamaChatModelClient`.
- ViewModel calls `IrisDbContext`.
- Handler constructs concrete repositories.
- PromptBuilder calls provider clients.
- ModelGateway reads/writes conversation storage.

## Project Reference Checks

For `.csproj` or package changes, verify:

- no Domain reference to Application or adapters;
- no Application reference to adapters or hosts;
- no adapter-to-adapter reference unless explicitly approved;
- no host-to-host reference;
- no test-only dependency in production projects;
- package added to the owning project only.

Prefer `dotnet list <project> reference` for evidence.

## Evidence Commands

Use read-only evidence before issuing findings:

```powershell
dotnet list .\src\Iris.Domain\Iris.Domain.csproj reference
dotnet list .\src\Iris.Application\Iris.Application.csproj reference
dotnet list .\src\Iris.Desktop\Iris.Desktop.csproj reference
dotnet list .\src\Iris.Persistence\Iris.Persistence.csproj reference
dotnet list .\src\Iris.ModelGateway\Iris.ModelGateway.csproj reference
```

Use targeted search for forbidden coupling:

```powershell
Select-String -Path .\src\Iris.Desktop\**\*.cs -Pattern 'IrisDbContext','OllamaChatModelClient'
Select-String -Path .\src\Iris.Application\**\*.csproj -Pattern 'Iris.Persistence','Iris.ModelGateway'
```

If commands cannot run, state the evidence gap.

## DI Composition Checks

Good:

- adapter registers its own implementations;
- host composes Application and adapters;
- Application registers application services only;
- Domain has no DI registration.

Bad:

- Application registers EF/Ollama/concrete adapters;
- UI manually constructs adapter internals for workflow logic;
- service locator hides dependencies;
- infrastructure becomes a container for all adapters.

## Contract Checks

When public contracts change, verify:

- interface belongs in Application if implemented by adapters;
- DTOs do not expose EF entities or provider request/response shapes;
- errors are application-visible, not raw provider/database exceptions;
- cancellation tokens are preserved across async ports;
- options/config live in adapter/host configuration, not Domain.

If contract shape is unclear, design first.

## Architecture Review Output Expectations

Architecture review should report:

- context reviewed;
- expected dependency direction;
- layer ownership result;
- forbidden shortcut table;
- project reference findings;
- DI findings;
- P0/P1/P2 findings with evidence;
- final readiness decision.

Do not implement fixes during architecture review unless the user explicitly asks.

## Finding Examples

Good P1 finding:

```markdown
#### P1-001: Desktop ViewModel bypasses Application facade

- Evidence: `src/Iris.Desktop/ViewModels/ChatViewModel.cs` constructs `OllamaChatModelClient`.
- Impact: UI owns provider workflow and blocks future model routing/testing.
- Recommended fix: inject `IIrisApplicationFacade` and route send through Application handler.
```

Bad finding:

```markdown
Architecture feels mixed.
```

Why bad: no evidence, no impact, no minimal fix.

## Stop Conditions

Stop when:

- project rules conflict with the requested change;
- required context is missing;
- a shortcut appears necessary to make the feature work;
- ownership cannot be assigned to one layer;
- a public contract change lacks design approval.

## Pressure Scenarios

### Scenario 1: "Just let Desktop call SQLite for now"

Expected:

- reject shortcut;
- route through Application/persistence abstractions;
- record debt only if user explicitly approves a temporary exception.

### Scenario 2: "Put the common chat DTO in Shared"

Expected:

- check whether DTO knows Iris product concepts;
- keep product-specific contracts in Domain/Application/Desktop as appropriate;
- Shared only gets neutral primitives.

### Scenario 3: "ModelGateway needs conversation history"

Expected:

- ModelGateway receives provider-ready request from Application;
- Application owns prompt/history assembly;
- ModelGateway does not depend on Persistence.

### Scenario 4: "Tools can decide if command is dangerous"

Expected:

- Tools can enforce low-level sandbox guards;
- Application owns product permission policy and risk classification.

## Quality Checklist

- [ ] Architecture docs were inspected.
- [ ] Affected layer ownership is explicit.
- [ ] Dependency direction was checked.
- [ ] Project references were checked when relevant.
- [ ] DI was checked when relevant.
- [ ] Forbidden shortcuts were checked.
- [ ] Shared remained neutral.
- [ ] Findings include file or command evidence.

## Anti-Patterns

Avoid:
- approving shortcuts because they are temporary;
- adding broad abstractions before a real seam exists;
- moving behavior to Shared because it is convenient;
- treating adapters as product-policy owners;
- calling a change architecture-safe without checking references.

## Self-Test Checklist

- Did I identify the owner layer for every changed behavior?
- Did I check project references when boundaries were touched?
- Did I check DI when composition changed?
- Did I distinguish low-level safety from product policy?
- Did I cite evidence for every finding?
- Did I avoid inventing a new abstraction without a real seam?
