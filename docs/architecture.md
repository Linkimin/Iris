# Iris — Architecture

## Overview

Iris (Айрис) is a local personal AI companion built on .NET 10 with a Clean / Hexagonal modular architecture.

**Core principle:** Domain + Application define the system. Adapters implement external technology. Hosts compose and run the system.

## Solution Layout

```
Iris/
├── src/             # 13 .NET projects
│   ├── Iris.Shared/           # Neutral reusable primitives
│   ├── Iris.Domain/           # Pure domain model
│   ├── Iris.Application/      # Use cases, orchestration, ports
│   ├── Iris.Persistence/      # EF Core / SQLite adapter
│   ├── Iris.ModelGateway/     # LLM provider adapter (Ollama, LM Studio)
│   ├── Iris.Perception/       # Desktop capture adapter
│   ├── Iris.Tools/            # Tool execution adapter
│   ├── Iris.Voice/            # Audio/STT/TTS adapter
│   ├── Iris.Infrastructure/   # Shared technical plumbing
│   ├── Iris.SiRuntimeGateway/ # Python SI runtime bridge
│   ├── Iris.Desktop/          # Avalonia desktop host
│   ├── Iris.Api/              # HTTP API host
│   └── Iris.Worker/           # Background worker host
├── tests/           # 5 test projects
│   ├── Iris.Domain.Tests/
│   ├── Iris.Application.Tests/
│   ├── Iris.Infrastructure.Tests/
│   ├── Iris.Integration.Tests/
│   └── Iris.Architecture.Tests/
├── python/          # Python SI runtime sidecar
├── docs/            # Public documentation
└── .agent/          # Agent working memory (INTERNAL ONLY)
```

## Dependency Direction

```
Shared ← Domain ← Application ← Adapters ← Hosts
```

### Core (Domain + Application)

| Project | May depend on | Must NOT depend on |
|---|---|---|
| `Iris.Shared` | Nothing product-specific | Iris product concepts |
| `Iris.Domain` | `Iris.Shared` | EF Core, HTTP, UI, infrastructure |
| `Iris.Application` | `Iris.Domain`, `Iris.Shared` | Concrete adapters, hosts |

### Adapters

All adapters implement Application abstractions. They may depend inward on Application / Domain / Shared.

Adapters must NOT depend on each other unless explicitly approved.

### Hosts

Hosts (Desktop, API, Worker) compose Application + adapters. They must NOT depend on each other.

## Forbidden Shortcuts

- ViewModel → Ollama / DbContext (must go through Application)
- API endpoint → DbContext (must go through Application)
- Application → Persistence / ModelGateway (use abstractions)
- Domain → EF Core / HTTP
- Tools → permission decisions (Application decides)
- Voice → chat orchestration (Application orchestrates)
- Perception → memory extraction (Application owns)
- Shared → product-specific behavior

## First Vertical Slice

```
ChatView → ChatViewModel → IrisApplicationFacade
  → SendMessageHandler → PromptBuilder
  → Ollama (via ModelGateway)
  → SQLite (via Persistence)
  → response back to UI
```

## Testing Strategy

| Test project | Scope |
|---|---|
| `Iris.Domain.Tests` | Pure domain behavior, no infrastructure |
| `Iris.Application.Tests` | Use cases with fakes/stubs |
| `Iris.Infrastructure.Tests` | Serialization, event bus, background tasks |
| `Iris.Integration.Tests` | Composed behavior (Persistence+SQLite, ModelGateway stubs) |
| `Iris.Architecture.Tests` | Dependency direction, forbidden references, namespace guards |

## Build & Test

```powershell
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx
dotnet format .\Iris.slnx --verify-no-changes
```

## Decision Policy

When unsure where code belongs:

1. Domain concept or invariant → `Iris.Domain`
2. Use case or policy → `Iris.Application`
3. Database / EF / SQLite → `Iris.Persistence`
4. Model provider HTTP logic → `Iris.ModelGateway`
5. Desktop capture / WinAPI → `Iris.Perception`
6. Tool execution / sandbox → `Iris.Tools`
7. Audio / STT / TTS → `Iris.Voice`
8. Shared technical plumbing → `Iris.Infrastructure`
9. Python SI runtime calls → `Iris.SiRuntimeGateway`
10. UI → `Iris.Desktop`
11. HTTP transport → `Iris.Api`
12. Background host → `Iris.Worker`

If code seems to belong everywhere, it probably belongs nowhere yet. Stop and design the boundary first.
