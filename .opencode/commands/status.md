---
description: Summarize current project status from git state and agent memory
agent: planner
---

# /status

Use the `iris-engineering` skill.

Summarize the current project status.

Do not implement.
Do not edit files.
Do not create files.
Do not update memory files.
Do not run verification commands unless explicitly requested.
Do not restate this command template.
Do not narrate reasoning.
Use only the factual context injected below.

## Repository Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1`

## Agent Memory Context

!`powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1`

## Output Format

# Project Status

## 1. Summary

<compact factual summary>

## 2. Git State

- Branch:
- Working tree:
- Changed files:
- Untracked files:

## 3. Current Work

- Active task:
- Active phase:
- Last completed:
- Next safe step:

## 4. Recent Changes

- ...

## 5. Open Issues / Risks

- ...

## 6. Last Known Verification

- Command:
- Result:
- Date:

## 7. Recommended Next Step

- ...

## Execution Note

No implementation was performed.
No files were modified.
