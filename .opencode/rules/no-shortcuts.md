# Iris No-Shortcuts Rules

## Related Skills

- `.opencode/skills/iris-engineering/SKILL.md`
- `.opencode/skills/iris-architecture/SKILL.md`

These shortcuts are forbidden unless the user explicitly approves a new architecture decision.

## Desktop

- Desktop must not call Ollama, LM Studio, or model-provider HTTP clients directly.
- Desktop must not use `IrisDbContext` or repositories directly for product workflow.
- Desktop must not build prompts.
- Desktop must not execute tools directly.
- Desktop must not call Win32 perception directly.

## Application

- Application must not reference `Iris.Persistence`.
- Application must not reference `Iris.ModelGateway`.
- Application must not reference `Iris.Perception`, `Iris.Tools`, `Iris.Voice`, or `Iris.Infrastructure`.
- Application must not construct concrete adapter implementations.
- Application must not contain UI/Avalonia code.

## Domain

- Domain must not reference EF Core.
- Domain must not reference Application.
- Domain must not contain HTTP, UI, file system, process, or provider code.

## Adapters

- Persistence must not call model providers.
- ModelGateway must not use repositories or own memory decisions.
- Tools must not own product permission decisions.
- Voice must not own chat orchestration.
- Perception must not extract memories or call models.
- Infrastructure must not become a warehouse for specialized adapters.

## Hosts

- Desktop must not depend on API or Worker.
- API must not depend on Desktop or Worker.
- Worker must not depend on Desktop or API.

## Shared

- Shared must not contain Iris product/domain behavior.
- Shared must not become a dumping ground for cross-layer convenience types.

## Python Runtime

- Python runtime is a sidecar.
- Python runtime must not own Iris main state, memory database, persona policy, tools, or UI integration.
