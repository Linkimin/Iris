
# AGENTS.md — Iris / Айрис Coding Agent Rules

> This file is a strict instruction document for coding agents working on Iris / Айрис.
> The agent must read this file before making any change.
> These rules are mandatory.

---

## 1. Project Identity

Iris / Айрис is a local personal AI companion built on .NET 10.

The system is designed as a desktop-first local assistant with:

- personality;
- long-term memory;
- local model integration;
- desktop perception;
- voice;
- tools;
- SI runtime;
- companion modes;
- future extensibility without architectural rewrite.

The project uses Clean / Hexagonal modular architecture.

Core rule:

```text
Domain + Application define the system.
Adapters implement external technology.
Hosts compose and run the system.
````

---

## 2. Mandatory Agent Behavior

The agent must:

1. Think before acting.
2. Inspect existing files before creating new files.
3. Search project metadata before asking the user.
4. Ask the user when the answer is not in metadata.
5. Never invent architecture decisions.
6. Never simplify architecture without explicit approval.
7. Never hardcode behavior that belongs in configuration, policy, options, or domain/application logic.
8. Preserve SOLID and Clean Code.
9. Respect existing project boundaries.
10. Update agent metadata after meaningful work.

The agent must not behave like a blind code generator.

---

## 3. Required Tools / Capabilities

When available, the agent must use:

```text
superpowers
MCP context7
```

Use these tools to:

* inspect relevant project context;
* verify framework/library usage;
* avoid outdated assumptions;
* check API/library patterns;
* reduce hallucinated implementation details.

If tool access is unavailable, the agent must explicitly state that it could not use the expected tool and continue only with available project files and instructions.

---

## 4. Required Reading Order

Before changing code, the agent must read:

```text
AGENTS.md
.agent/overview.md
.agent/architecture.md
.agent/first-vertical-slice.md
```

For feature-specific work, also read relevant memory files:

```text
.agent/mem_library/00_project_identity.md
.agent/mem_library/01_product_vision.md
.agent/mem_library/02_user_experience.md
.agent/mem_library/03_iris_persona.md
.agent/mem_library/04_feature_map.md
.agent/mem_library/05_memory_system.md
.agent/mem_library/06_desktop_perception.md
.agent/mem_library/07_voice_system.md
.agent/mem_library/08_tools_and_permissions.md
.agent/mem_library/09_si_runtime.md
.agent/mem_library/10_modes.md
.agent/mem_library/11_security_and_privacy.md
.agent/mem_library/12_future_roadmap.md
```

The agent must not skip metadata reading for architecture-affecting changes.

---

## 5. Agent Metadata Responsibility

The `.agent/` folder is the agent’s operational memory.

Current files:

```text
.agent/
├── mem_library/
├── architecture.md
├── debt_tech_backlog.md
├── first-vertical-slice.md
├── log_notes.md
├── overview.md
└── PROJECT_LOG.md
```

## 5.1 `PROJECT_LOG.md`

Must be updated after every meaningful iteration.

Record:

* what was changed;
* why it was changed;
* which files were touched;
* what remains;
* build/test status if checked.

Use dated append-only entries.

Do not overwrite the whole file.

## 5.2 `overview.md`

Must be updated after every meaningful iteration.

Record:

* current phase;
* current implementation target;
* current working status;
* next immediate step;
* known blockers.

This file must stay short and current.

## 5.3 `log_notes.md`

Every found bug, failure, broken command, build problem, unexpected behavior, or investigation note must be recorded here.

Use this file for:

* build errors;
* runtime errors;
* Visual Studio/project issues;
* broken paths;
* failed commands;
* suspicious behavior;
* unresolved questions.

The user referred to this as `local_notes`; the actual project file is:

```text
.agent/log_notes.md
```

Use `log_notes.md`, not a new `local_notes.md`, unless the user explicitly renames it.

## 5.4 `debt_tech_backlog.md`

Any technical debt introduced or discovered must be recorded here.

Examples:

* temporary stub;
* missing validation;
* incomplete mapper;
* missing test;
* deferred cleanup;
* TODO that cannot be solved immediately.

Do not hide debt in vague inline comments.

## 5.5 `mem_library/`

Contains stable product meaning.

Do not use it as a bug tracker or task log.

Use it to understand intended behavior and long-term design.

---

## 6. Required Workflow

Work must follow this chain:

```text
Spec
→ checkpoints
→ Design
→ checkpoints
→ Plan
→ checkpoints
→ Implementation
→ Audit + Review
→ checkpoints
```

The agent must not jump directly into implementation for non-trivial work.

## 6.1 Spec

Defines:

* problem;
* scope;
* non-goals;
* affected layers;
* invariants;
* acceptance criteria;
* open questions.

## 6.2 Checkpoints

At each checkpoint, verify:

* scope is still correct;
* no architecture boundary is violated;
* no new hidden dependency is introduced;
* no duplicated responsibility appears;
* user intent is still preserved.

## 6.3 Design

Defines:

* file placement;
* project responsibilities;
* interfaces/contracts;
* data flow;
* failure handling;
* dependency boundaries.

## 6.4 Plan

Defines:

* ordered implementation steps;
* exact files to touch;
* tests to add/update;
* metadata updates;
* rollback/cleanup expectations.

## 6.5 Implementation

Must follow the approved design and plan.

No surprise files.
No unrelated refactors.
No architecture simplification.

## 6.6 Audit + Review

After implementation, perform review passes:

1. Spec compliance.
2. Test quality.
3. SOLID / architecture quality.
4. Clean Code / maintainability.

Record findings in metadata.

---

## 7. Questions and Uncertainty Policy

If a question appears, the agent must first search:

```text
AGENTS.md
.agent/overview.md
.agent/architecture.md
.agent/first-vertical-slice.md
.agent/mem_library/
.agent/PROJECT_LOG.md
.agent/debt_tech_backlog.md
.agent/log_notes.md
```

If the answer is found, follow the documented decision.

If the answer is not found, ask the user.

The agent must not invent missing architectural decisions.

Forbidden behavior:

```text
"I assumed..."
"I simplified..."
"I created a new pattern..."
"I moved this because it seemed cleaner..."
```

without metadata support or explicit user approval.

---

## 8. File Creation Policy

Before creating any file, the agent must:

1. Inspect the target project.
2. Check whether a placeholder already exists.
3. Check whether the intended responsibility already has a file.
4. Check architecture docs.
5. Decide whether the file belongs in the proposed layer.
6. Record reasoning if the file is architecture-significant.

This project already contains many named placeholders.

Therefore, creating new files should be rare.

Prefer using existing planned files.

## 8.1 Required Brainstorm Before File Creation

Before creating a file, answer:

```text
What responsibility does this file have?
Which project/layer owns that responsibility?
Does an existing file already represent this responsibility?
Will this duplicate another file?
Will this introduce a dependency boundary issue?
Is this file required for the current slice?
Can this wait?
```

If unsure, ask the user.

---

## 9. File Deletion Policy

Before deleting any file, the agent must:

1. Inspect references.
2. Check whether the file is a planned placeholder.
3. Check whether docs mention it.
4. Check whether it belongs to the skeleton.
5. Consider whether it should be kept empty for future structure.
6. Ask the user if deletion is not clearly safe.

## 9.1 Safe deletion examples

Usually safe:

```text
WeatherForecast.cs from template
unused template endpoint
accidental NewFolder1
duplicate broken generated path
```

## 9.2 Unsafe deletion examples

Do not delete without approval:

```text
Domain model placeholders
Application handler placeholders
adapter structure files
metadata files
architecture docs
test project skeletons
```

---

## 10. No Hardcode Rule

Hardcoding is forbidden when the value belongs to:

* configuration;
* options;
* policy;
* domain rule;
* application rule;
* user setting;
* provider setting;
* path setting;
* model setting;
* permission setting;
* timeout/retry setting.

Examples of forbidden hardcoding:

```text
Ollama model name inside SendMessageHandler
SQLite path inside repository
allowed tool paths inside tool implementation
shell allowlist inside random handler
persona prompt inside Desktop ViewModel
API key inside source code
Python runtime URL inside application logic
```

Acceptable only for:

* local constants with no product meaning;
* test data inside tests;
* private helper defaults where clearly harmless;
* temporary stubs recorded in debt backlog.

---

## 11. No Architecture Simplification Rule

The agent must not simplify architecture just to make implementation easier.

Forbidden:

```text
ViewModel calls Ollama directly.
ViewModel uses DbContext.
Application references Persistence.
Application references ModelGateway.
API endpoint calls DbContext directly.
API endpoint calls ToolExecutor directly.
Tools decide product-level permissions.
Voice starts chat pipeline directly.
Perception writes memory directly.
Infrastructure becomes a warehouse for everything.
Python runtime owns Iris memory or main backend logic.
```

Correct direction:

```text
Host/UI/API/Worker
→ Application
→ Application abstractions
→ Adapter implementations
```

---

## 12. SOLID Requirements

The agent must preserve SOLID.

## 12.1 Single Responsibility

Each file/class must have one clear reason to change.

Bad:

```text
SendMessageHandler builds UI state, calls Ollama HTTP, writes EF entities, updates persona, executes tools.
```

Good:

```text
SendMessageHandler orchestrates Application abstractions.
ModelGateway calls model provider.
Persistence stores entities.
Desktop renders UI.
```

## 12.2 Open/Closed

Add behavior through clear extension points and policies, not by modifying unrelated classes.

## 12.3 Liskov Substitution

Implement abstractions faithfully.

Adapter implementations must not surprise callers with unrelated side effects.

## 12.4 Interface Segregation

Do not create giant interfaces.

Prefer narrow abstractions:

```text
IChatModelClient
IEmbeddingClient
IScreenshotProvider
IToolExecutor
IAudioPlaybackService
```

## 12.5 Dependency Inversion

Application defines abstractions.
Adapters implement them.

Application must not depend on concrete adapters.

---

## 13. Clean Code Requirements

Code must be:

* readable;
* small enough;
* named precisely;
* explicit about failures;
* low in hidden side effects;
* easy to test;
* consistent with existing project naming.

Avoid:

* god classes;
* giant methods;
* boolean parameter soup;
* hidden global state;
* duplicated mapping logic;
* broad catch blocks that hide errors;
* random static helpers;
* vague names like `Manager`, `Helper`, `Processor` unless clearly justified.

---

## 14. Failure Pattern Policy

Before implementing logic, the agent must consider failure patterns.

For every meaningful operation, ask:

```text
What can fail?
How is failure represented?
Where is it logged?
What is shown to the user?
Does failure leak private data?
Does failure leave partial state?
Is cancellation supported?
Is timeout needed?
```

Common failure areas:

* model unavailable;
* database unavailable;
* invalid config;
* permission denied;
* sandbox violation;
* path invalid;
* process timeout;
* tool output too large;
* microphone unavailable;
* screenshot failed;
* Python runtime unavailable;
* HTTP timeout;
* JSON serialization failure.

No random crashes where controlled errors are expected.

---

## 15. Invariant Policy

The agent must preserve invariants.

Examples:

## Domain

* domain model has no EF/UI/HTTP attributes;
* value objects validate their own basic invariants;
* domain events do not call infrastructure.

## Application

* no adapter references;
* use-cases orchestrate abstractions;
* policies live in Application, not adapters.

## Persistence

* maps Domain ↔ Entity;
* owns EF;
* does not make product decisions.

## ModelGateway

* owns model provider calls;
* does not build prompts;
* does not store memory.

## Tools

* executes only after Application flow;
* sandbox always applies;
* shell not unrestricted by default.

## Voice

* no silent recording;
* voice does not decide chat meaning.

## Perception

* no silent screen/clipboard access by default;
* Win32 stays inside `Iris.Perception/Windows`.

## API

* local-only by default;
* endpoints call Application, not adapters directly.

## Worker

* hosts background runtime;
* does not implement Application tasks.

## Python Runtime

* sidecar only;
* does not own main Iris state.

---

## 16. Current Architecture Summary

Current major projects:

```text
Iris.Shared
Iris.Domain
Iris.Application
Iris.Persistence
Iris.ModelGateway
Iris.Perception
Iris.Tools
Iris.Voice
Iris.Infrastructure
Iris.SiRuntimeGateway
Iris.Desktop
Iris.Api
Iris.Worker
```

Current Python project:

```text
python/iris-ai-runtime/src/iris_si_runtime
```

Current test projects:

```text
Iris.Domain.Tests
Iris.Application.Tests
Iris.Infrastructure.Tests
Iris.Integration.Tests
```

Recommended additional test project:

```text
Iris.Architecture.Tests
```

---

## 17. First Implementation Priority

The immediate priority is:

```text
1. Make solution build.
2. Implement first vertical chat slice.
3. Persist conversations/messages.
4. Call Ollama through ModelGateway.
5. Display response in Desktop UI.
6. Add minimal tests.
7. Add architecture tests.
```

Do not start advanced features first:

```text
Memory recall
Tools
Perception
Voice
SI runtime
API operationalization
Worker operationalization
Modes
Agent mode
```

---

## 18. First Vertical Slice Boundary

Target flow:

```text
ChatView
→ ChatViewModel
→ IrisApplicationFacade
→ SendMessageHandler
→ PromptBuilder
→ OllamaChatModelClient
→ ConversationRepository / MessageRepository
→ SQLite
→ UI response
```

Forbidden shortcuts:

```text
ChatViewModel → OllamaChatModelClient
ChatViewModel → IrisDbContext
SendMessageHandler → IrisDbContext
SendMessageHandler → concrete Ollama client
PromptBuilder → Ollama
ModelGateway → Repository
Persistence → ModelGateway
```

---

## 19. Dependency Rules

## 19.1 Core

```text
Iris.Shared:
  no product-specific dependencies

Iris.Domain:
  may depend on Iris.Shared only

Iris.Application:
  may depend on Iris.Domain and Iris.Shared only
```

## 19.2 Adapters

Adapters may depend inward:

```text
Iris.Persistence       → Application / Domain / Shared
Iris.ModelGateway     → Application / Domain / Shared
Iris.Perception       → Application / Domain / Shared
Iris.Tools            → Application / Domain / Shared
Iris.Voice            → Application / Domain / Shared
Iris.Infrastructure   → Application / Domain / Shared
Iris.SiRuntimeGateway → Application / Shared
```

Adapters must not depend on each other unless explicitly approved.

## 19.3 Hosts

Hosts compose dependencies:

```text
Iris.Desktop
Iris.Api
Iris.Worker
```

Host projects must not depend on each other.

Forbidden:

```text
Iris.Desktop → Iris.Api
Iris.Desktop → Iris.Worker
Iris.Api → Iris.Desktop
Iris.Api → Iris.Worker
Iris.Worker → Iris.Desktop
Iris.Worker → Iris.Api
```

---

## 20. Project-Specific Rules

## 20.1 `Iris.Desktop`

Allowed:

* Avalonia UI;
* ViewModels;
* UI services;
* facade calls to Application.

Forbidden:

* direct LLM calls;
* direct DbContext;
* direct WinAPI;
* direct tool execution;
* prompt logic.

## 20.2 `Iris.Application`

Allowed:

* handlers;
* policies;
* prompt/context assembly;
* memory logic;
* tool planning;
* voice/perception scenarios;
* abstractions.

Forbidden:

* concrete adapters;
* EF Core;
* HTTP provider details;
* Avalonia.

## 20.3 `Iris.Domain`

Allowed:

* entities;
* value objects;
* domain rules.

Forbidden:

* EF;
* HTTP;
* UI;
* infrastructure.

## 20.4 `Iris.Persistence`

Allowed:

* EF Core;
* SQLite;
* repositories;
* mappings.

Forbidden:

* product decisions;
* prompt logic;
* model calls.

## 20.5 `Iris.ModelGateway`

Allowed:

* Ollama/LM Studio/OpenAI-compatible local adapter logic.

Forbidden:

* prompt ownership;
* persistence;
* memory decisions.

## 20.6 `Iris.Tools`

Allowed:

* technical tool execution;
* registry;
* sandbox;
* process/file/web tools.

Forbidden:

* product permission policy;
* risk classification ownership;
* memory writes.

## 20.7 `Iris.Voice`

Allowed:

* STT/TTS;
* audio recording/playback;
* devices.

Forbidden:

* chat orchestration;
* persona decisions;
* memory writes.

## 20.8 `Iris.Perception`

Allowed:

* active window;
* clipboard;
* screenshot;
* Win32 isolation.

Forbidden:

* memory extraction;
* model calls;
* tool execution.

## 20.9 `Iris.Infrastructure`

Allowed:

* serialization;
* event bus;
* background task runtime;
* app directories;
* config helpers.

Forbidden:

* database;
* LLM;
* tools;
* voice;
* perception;
* UI.

## 20.10 `Iris.SiRuntimeGateway`

Allowed:

* call Python SI runtime;
* map contracts;
* health/diagnostics.

Forbidden:

* SI reasoning implementation;
* memory ownership;
* unrelated adapter dependencies.

## 20.11 `Iris.Api`

Allowed:

* HTTP transport;
* contracts;
* endpoints;
* middleware;
* local security;
* host composition.

Forbidden:

* business logic;
* direct DbContext;
* direct ToolExecutor;
* direct Win32/audio operations.

## 20.12 `Iris.Worker`

Allowed:

* host runtime;
* hosted services;
* background service startup.

Forbidden:

* Application task implementation;
* direct business logic;
* Desktop/API dependency.

---

## 21. Python Runtime Rules

Python runtime path:

```text
python/iris-ai-runtime/src/iris_si_runtime
```

Python runtime is a sidecar, not the main backend.

Allowed:

* symbolic reasoning;
* hypothesis generation;
* inference trace;
* temporary working memory;
* structured reasoning API.

Forbidden:

* main chat pipeline ownership;
* Iris memory database ownership;
* tool execution;
* persona policy;
* desktop perception ownership;
* direct UI integration.

All Python imports must use package namespace:

```python
from iris_si_runtime.core.result import Result
```

Not:

```python
from core.result import Result
```

---

## 22. Testing Rules

Minimum test projects:

```text
Iris.Domain.Tests
Iris.Application.Tests
Iris.Infrastructure.Tests
Iris.Integration.Tests
```

Recommended:

```text
Iris.Architecture.Tests
```

## Domain tests

No EF, HTTP, UI, tools, voice, perception.

## Application tests

Use fakes/stubs for abstractions.
Do not reference concrete adapters.

## Infrastructure tests

Test infrastructure plumbing only.

## Integration tests

May compose multiple projects.

## Architecture tests

Must enforce dependency rules.

---

## 23. Metadata Update Rules

After every meaningful iteration:

1. Update `.agent/PROJECT_LOG.md`.
2. Update `.agent/overview.md`.
3. Add bugs/problems to `.agent/log_notes.md`.
4. Add technical debt to `.agent/debt_tech_backlog.md`.

Do not leave metadata stale.

## PROJECT_LOG entry format

Use:

```md
## YYYY-MM-DD — Short title

### Changed
- ...

### Files
- ...

### Validation
- Build/test status.

### Next
- ...
```

## log_notes entry format

Use:

```md
## YYYY-MM-DD — Problem title

### Symptom
- ...

### Cause / Hypothesis
- ...

### Action
- ...

### Status
- Open / Resolved / Deferred
```

## debt_tech_backlog entry format

Use:

```md
## Debt: Short title

### Area
...

### Problem
...

### Risk
...

### Proposed fix
...

### Priority
Low / Medium / High
```

---

## 24. Checkpoint Requirements

At each workflow checkpoint, verify:

```text
Scope is still correct.
Affected projects are correct.
No dependency rule is violated.
No duplicate responsibility is introduced.
Existing placeholder files were checked.
Failure modes were considered.
Tests/validation are planned.
Metadata updates are planned.
```

If any answer is unclear, stop and ask the user.

---

## 25. Build and Validation

When possible, validate with:

```powershell
dotnet build
dotnet test
```

For Python runtime, when relevant:

```powershell
python -m iris_si_runtime
```

or project-specific commands from Python metadata.

Do not claim validation passed unless it was actually run.

If validation was not run, state it in `PROJECT_LOG.md`.

---

## 26. Forbidden Behavior

The agent must not:

* create files without checking existing placeholders;
* delete files without checking references and intent;
* simplify architecture;
* hardcode product behavior;
* bypass Application layer;
* ignore `.agent` metadata;
* skip PROJECT_LOG updates;
* skip overview updates;
* hide bugs;
* leave discovered debt undocumented;
* add broad abstractions without need;
* implement future features before first vertical slice;
* silently change project direction;
* make architectural decisions without metadata/user confirmation.

---

## 27. Required Mental Step Before Any Action

Before any file modification, the agent must explicitly reason internally through:

```text
What am I trying to change?
Which layer owns this responsibility?
Does a file already exist for this?
What dependency rules apply?
What can fail?
What invariant must be preserved?
What metadata must be updated?
Is this necessary for the current phase?
```

The agent must think before acting.

This is mandatory.

---

## 28. Current Immediate Task Direction

The project structure is complete enough.

The next correct direction is:

```text
Make the solution build.
Fix only build-blocking issues.
Then implement the first vertical chat slice.
```

Do not continue creating architecture folders or speculative files unless required.

---

## 29. Final Rule

When in doubt:

```text
Search AGENTS.md and .agent metadata first.
If still unclear, ask the user.
Do not improvise.
```

The user prefers strict architecture, explicit reasoning, and controlled implementation over fast but chaotic code generation.

```

