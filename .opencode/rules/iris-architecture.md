# Iris Architecture Rules

## Related Skills

- `.opencode/skills/iris-engineering/SKILL.md`
- `.opencode/skills/iris-architecture/SKILL.md`

## Dependency Direction

Iris uses Clean / Hexagonal architecture.

Core direction:

```text
Shared <- Domain <- Application <- Adapters <- Hosts
```

Domain and Application define the system. Adapters implement external technology. Hosts compose and run the system.

## Core Projects

`Iris.Shared`:

- must stay product-neutral;
- may contain reusable primitives, guards, result types, IDs, clocks, diagnostics, and pagination;
- must not contain Iris product/domain/application/provider/UI behavior.

`Iris.Domain`:

- may depend on `Iris.Shared`;
- owns entities, value objects, invariants, and domain concepts;
- must not reference Application, EF Core, HTTP, UI, providers, files, processes, or infrastructure.

`Iris.Application`:

- may depend on `Iris.Domain` and `Iris.Shared`;
- owns use cases, policies, ports, orchestration, prompt/context assembly, and application DTOs;
- must not reference concrete adapters, EF Core, provider SDKs, Avalonia, API, Worker, or Desktop.

## Adapters

Adapters may depend inward on Application, Domain, and Shared:

- `Iris.Persistence`
- `Iris.ModelGateway`
- `Iris.Perception`
- `Iris.Tools`
- `Iris.Voice`
- `Iris.Infrastructure`
- `Iris.SiRuntimeGateway`

Adapters implement Application abstractions. They do not own product workflow or domain policy.

Adapters must not depend on each other unless explicitly approved by design.

## Hosts

Hosts are composition roots:

- `Iris.Desktop`
- `Iris.Api`
- `Iris.Worker`

Hosts may depend on Application and adapters. Hosts must not depend on each other.

Hosts must not own business logic, prompt logic, persistence logic, model-provider logic, permission policy, memory extraction, voice orchestration, or tool planning.

## Required Flow

Preferred flow:

```text
Host/UI/API/Worker
→ Application
→ Application abstractions
→ Adapter implementations
→ External system
```

For first chat slice:

```text
ChatViewModel
→ IrisApplicationFacade
→ SendMessageHandler
→ Application abstractions
→ ModelGateway/Persistence adapters
```

## Project Reference Rules

Forbidden:

- `Iris.Domain` -> Application/adapters/hosts.
- `Iris.Application` -> adapters/hosts.
- adapter -> host.
- host -> host.
- specialized adapter -> specialized adapter without approved design.
- production project -> test project.

If a new reference seems necessary, stop and design the boundary first.

## DI Rules

Allowed:

- Application registers Application services.
- Adapters register their own implementations.
- Hosts compose Application plus adapters.

Forbidden:

- Domain DI registration.
- Application registering concrete EF/model/tool/voice/perception implementations.
- UI manually constructing adapter internals for workflow logic.
- service locator patterns.
