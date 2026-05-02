
# AGENTS.md

## Project Operating Rules

This repository is an engineering project, not a sandbox. Preserve architecture, contracts, tests, documentation, and project intent.

## Workflow

For non-trivial work, use:

1. Brainstorm (optional)
2. Spec
3. Design
4. Plan
5. Implement
6. Verify
7. Review
8. Audit

Do not jump directly into implementation unless the change is trivial and local.

For failures (bugs, test failures, build failures), use Debug before planning a fix.

## Rule Files

Read relevant rule files before acting:

- `.opencode/rules/00-core-workflow.md`
- `.opencode/rules/10-architecture-boundaries.md`
- `.opencode/rules/20-dotnet-style.md`
- `.opencode/rules/30-testing-verification.md`
- `.opencode/rules/40-agent-memory.md`
- `.opencode/rules/50-security-safety.md`
- `.opencode/rules/60-audit.md`

Use only rule files relevant to the task. Do not flood context unnecessarily.

## Skills

Use workflow skills from:

- `.opencode/skills/iris-brainstorm/SKILL.md`
- `.opencode/skills/iris-debug/SKILL.md`
- `.opencode/skills/iris-tdd/SKILL.md`
- `.opencode/skills/spec/SKILL.md`
- `.opencode/skills/design/SKILL.md`
- `.opencode/skills/plan/SKILL.md`
- `.opencode/skills/implement/SKILL.md`
- `.opencode/skills/verify/SKILL.md`
- `.opencode/skills/audit/SKILL.md`
- `.opencode/skills/agent-memory/SKILL.md`
- `.opencode/skills/architecture-boundary-review/SKILL.md`

Use save skills only when the user explicitly asks to save an artifact.

## Agents

Use the correct role:

- `planner` — specs, designs, plans, analysis
- `builder` — approved implementation, verification, saving artifacts
- `reviewer` — read-only focused review
- `auditor` — formal final audit

## No Architecture Drift

Do not independently:

- change project boundaries;
- introduce new projects;
- collapse layers;
- bypass the Application layer;
- move business logic into UI, persistence, tools, voice, or hosts;
- add concrete infrastructure dependencies to Domain or Application;
- change public contracts without explicit reason.

## Reconnaissance

Before creating a file, check whether a suitable file, folder, abstraction, placeholder, or convention already exists.

Prefer extending existing structure over inventing parallel structure.

## Verification

After code changes, run the narrowest useful verification first.

For .NET projects, prefer:

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
````

Do not claim success unless verification was actually run. If skipped, state why.

## Agent Memory

When `.agents/` exists, maintain relevant files:

* `.agents/PROJECT_LOG.md`
* `.agents/overview.md`
* `.agents/local_notes.md`
* `.agents/mem_library/**`

Before asking about project decisions, inspect relevant memory files.

## Security

Do not read, print, store, or modify secrets, credentials, tokens, private keys, production configs, or real customer data.

Never run destructive commands without explicit approval.

Never run:

* `git push`
* `git clean`
* `git reset --hard`
* `rm -rf`
* `docker system prune`
* destructive PowerShell removal commands

## External Documentation

When working with external libraries, frameworks, APIs, or unfamiliar syntax, use Context7 or official documentation before guessing.

## Final Response

Always summarize:

* what changed;
* what was verified;
* what was not verified;
* remaining risks or next steps.

```

