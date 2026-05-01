# Iris Verification Rules

## Related Skills

- `.opencode/skills/iris-engineering/SKILL.md`
- `.opencode/skills/iris-verification/SKILL.md`

## Principle

Verification proves repository state. It must not silently change source, tests, snapshots, docs, or memory.

## Preferred Iris Commands

For full Iris verification, prefer:

```powershell
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx
dotnet format .\Iris.slnx --verify-no-changes
```

Use narrower commands first when the change is local.

If `.slnx` is unsupported, fall back to a supported solution/project command and report the fallback.

## Reporting

Never claim verification passed unless the command actually ran and passed.

Report:

- exact command;
- pass/fail/skipped result;
- relevant output summary;
- whether files changed;
- verification limits.

## Failure Reporting

If verification fails, report:

- exact command;
- failure summary;
- likely cause;
- whether it appears related to current diff;
- minimum safe next fix.

Do not call a failure pre-existing without evidence.

## Mutating Commands

Forbidden during verification unless explicitly requested:

- mutating formatters;
- snapshot/golden updates;
- migrations against non-disposable databases;
- deleting or weakening tests;
- source edits;
- memory updates.

## Manual Gaps

If a behavior requires manual confirmation, state the gap explicitly.

Examples:

- live Desktop click-through;
- Ollama available/unavailable UX;
- voice device behavior;
- desktop perception permissions.
