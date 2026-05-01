# Iris OpenCode Rules

This directory contains hard constraints for OpenCode agents working on Iris.

Skills explain how to work.
Commands choose a workflow.
Rules define what must not be violated.

## Canonical Rules

These files are loaded by `opencode.jsonc`:

1. `workflow.md`
2. `iris-architecture.md`
3. `no-shortcuts.md`
4. `memory.md`
5. `verification.md`
6. `dotnet.md`
7. `security.md`
8. `review-audit.md`

## Compatibility Rules

Numbered files are compatibility pointers for older command templates and `AGENTS.md` references.

They must not contain independent rules.

## Ownership

If a rule grows into a tutorial, move method guidance into an Iris skill.

If a rule duplicates another rule, keep the stricter canonical rule and replace the duplicate with a pointer.

If a rule belongs to a command-specific workflow, keep it in the command.
