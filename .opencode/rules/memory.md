# Iris Memory Rules

## Related Skills

- `.opencode/skills/iris-engineering/SKILL.md`
- `.opencode/skills/iris-memory/SKILL.md`

## Canonical Paths

- Prefer `.agent`.
- Use `.agents` only if `.agent` does not exist.
- For Iris notes, prefer `log_notes.md`.
- Read `local_notes.md` only if it already exists.
- Do not create `local_notes.md` while `.agent/log_notes.md` exists.

## File Roles

`overview.md`:

- current phase;
- current implementation target;
- working status;
- next immediate step;
- known blockers.

`PROJECT_LOG.md`:

- completed meaningful iterations;
- changed files;
- validation status;
- remaining work.

`log_notes.md`:

- bugs;
- broken commands;
- build/test/runtime failures;
- suspicious behavior;
- unresolved investigation notes.

`debt_tech_backlog.md`:

- discovered or introduced technical debt;
- missing tests;
- deferred cleanup;
- temporary workarounds.

`mem_library/**`:

- stable product meaning;
- long-term architecture/product/UX/persona/security context.

Do not use `mem_library/**` as a task log.

## Write Policy

- `/update-memory` may update memory.
- Explicitly approved save workflows may update memory only when their skill says so.
- `/review`, `/architecture-review`, `/verify`, `/spec`, `/design`, `/plan`, and `/audit` are read-only for memory unless the user explicitly asks for memory update.

## Update Requirements

After meaningful completed implementation:

- append to `PROJECT_LOG.md`;
- update `overview.md` if active phase/status/next step changed;
- add failures or investigation notes to `log_notes.md`;
- add deferred debt to `debt_tech_backlog.md`.

Do not overwrite memory files wholesale.

## Safety

Do not record:

- secrets;
- raw prompts containing private content;
- API keys/tokens;
- production credentials;
- unrelated personal data.
