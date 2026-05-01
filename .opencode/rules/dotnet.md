# Iris .NET Rules

## Related Skills

- `.opencode/skills/iris-engineering/SKILL.md`
- `.opencode/skills/iris-verification/SKILL.md`

## Project Conventions

- Prefer existing project, namespace, folder, and DI patterns.
- Keep changes scoped to the owning project.
- Do not add projects, packages, references, or migrations unless the approved plan requires it.
- Do not move responsibilities across layers to make a task easier.

## Build And Test

Prefer:

```powershell
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx
dotnet format .\Iris.slnx --verify-no-changes
```

Use project-level commands for narrow local checks, then solution-level commands before readiness claims.

## Project References

Before changing references, check current references.

Forbidden in production projects:

- Domain -> Application/adapters/hosts.
- Application -> adapters/hosts.
- adapter -> host.
- host -> host.
- production project -> test project.

## Package Changes

Before adding a package:

- check whether an existing dependency already solves the problem;
- add the package only to the owning project;
- use central package management if present;
- document the reason in the plan or result.

Do not add packages casually.

## Tests By Layer

- Domain tests: pure domain behavior only.
- Application tests: use fakes/stubs for abstractions.
- Adapter tests: verify adapter behavior and mapping.
- Integration tests: composed behavior.
- Architecture tests: dependency and boundary rules.

Boundary-sensitive changes should have architecture coverage when the test project exists.

## Migrations

Do not edit or add migrations unless the approved plan explicitly requires persistence schema changes.

Do not apply migrations to non-disposable databases without explicit approval.
