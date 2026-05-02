# Manual Smoke Procedure: Phase 5.5 Avatar v1 (M-01 – M-07)

**Date authored:** 2026-05-01
**Build target:** post-Desktop-SQLite-path-stabilization (branch `feat/avatar-v1-and-opencode-v2` or successor)

## References

- Avatar spec (M-01–M-07 expected outcomes): `docs/specs/2026-04-30-phase-5-5-avatar-v1.spec.md` §11.
- Path stabilization spec: `docs/specs/2026-05-01-manual-smoke-and-sqlite-path-stability.spec.md`.
- Path stabilization design: `docs/designs/2026-05-01-manual-smoke-and-sqlite-path-stability.design.md`.
- Path stabilization plan: `docs/plans/2026-05-01-manual-smoke-and-sqlite-path-stability.plan.md`.

## Audience

Human operator with a Windows desktop session, local Ollama installed, and write access to `%APPDATA%\Iris\`. The operator drives this script start-to-finish and records observations.

`builder` agent must not execute these scenarios autonomously.

---

## §1 Global Preconditions

Complete every item below **before** running scenarios M-01 – M-07.

### 1.1 Source state

- [ ] Working tree is clean: `git status --short` shows no uncommitted changes (or only the smoke recording document edits).
- [ ] Branch is up to date with the post-stabilization implementation. Record the commit SHA used:
  - `git rev-parse HEAD` → `__________________________`

### 1.2 Build & automated test gate

- [ ] `dotnet build .\Iris.slnx` → 0 errors, 0 warnings.
- [ ] `dotnet test .\Iris.slnx --no-restore --no-build` → all tests pass (expected ~141 after path stabilization).
- [ ] `dotnet format .\Iris.slnx --verify-no-changes` → exit code 0.

### 1.3 Configuration baseline (default per-user path)

For M-01 – M-04 the default per-user database path must be in effect:

- [ ] `src/Iris.Desktop/appsettings.local.json` is **absent**, OR present but does **not** contain `Database:ConnectionString`.
- [ ] No environment variable overrides `Database:ConnectionString`.

### 1.4 Stale database cleanup

- [ ] Delete any leftover `iris.db` files from prior CWD-launch behavior:
  - [ ] Repository root: `iris.db` / `iris.db-shm` / `iris.db-wal` removed if present.
  - [ ] `src/Iris.Desktop/bin/Debug/net10.0/iris.db*` removed if present.
  - [ ] `%APPDATA%\Iris\iris.db*` removed (so M-01 observes a fresh creation).

### 1.5 Ollama state

- For M-01 – M-03: **Ollama must be running** with the model named in `appsettings.json` (`llama3.1` by default) available locally.
  - [ ] `ollama list` shows the configured chat model.
  - [ ] `curl http://localhost:11434/api/tags` returns 200 OK.
- For M-04: Ollama must be **stopped** (or unreachable on the configured port).
- M-05 – M-07 are independent of Ollama state.

### 1.6 Recording template (fill in inline below each scenario)

Per scenario use this short block:

```text
M-XX result: Pass | Fail | Anomaly
Observed: <one or two sentences>
Anomaly notes (if any): <reproduction, screenshots, log lines>
```

---

## §2 Scenarios

### M-01 — Запуск Desktop, Ollama работает (Avatar Idle on launch)

**Expected outcome (verbatim from Phase 5.5 spec §11):**
> Аватар виден, `Idle`.

**Preconditions delta from §1:** Ollama running; `appsettings.local.json` absent or without `Database:ConnectionString`.

**Steps:**

1. Run `dotnet run --project .\src\Iris.Desktop\Iris.Desktop.csproj --no-build` (or launch the built EXE).
2. Wait for the main window to display.
3. Visually confirm:
   - The avatar control is visible in the configured corner (default: bottom-right).
   - The avatar shows the **Idle** image (`idle.png`).
4. In a separate explorer/PowerShell window, confirm `%APPDATA%\Iris\iris.db` exists.
5. Close the Iris window (graceful close).

**Pass criteria:**

- Avatar appears on launch, in Idle state, in the configured corner.
- A new `iris.db` file is observed at `%APPDATA%\Iris\iris.db` (proves path stabilization is active).

**Result:**

```text
M-01 result: ___________
Observed: ____________________________________________________
Anomaly notes (if any): _______________________________________
```

---

### M-02 — Отправить сообщение (Thinking during send)

**Expected outcome (verbatim from spec §11):**
> Аватар переходит в `Thinking` на время отправки.

**Preconditions delta from §1:** Ollama running. Continue from the same launched window as M-01, or relaunch fresh.

**Steps:**

1. Type a short message into the chat input (e.g., `"Hello, Iris"`).
2. Click Send (or press Enter).
3. While the request is in flight, observe the avatar control.

**Pass criteria:**

- The avatar transitions from Idle to **Thinking** (`thinking.png`) for the duration of the send.

**Result:**

```text
M-02 result: ___________
Observed: ____________________________________________________
Anomaly notes (if any): _______________________________________
```

---

### M-03 — Дождаться ответа (Success → Idle after response)

**Expected outcome (verbatim from spec §11):**
> Аватар кратко показывает `Success`, затем возвращается в `Idle`.

**Preconditions delta from §1:** Continue from M-02; the assistant message is appended to the chat.

**Steps:**

1. After the assistant response appears in the chat list, observe the avatar.
2. Watch the transition over ~1–3 seconds (default `SuccessDisplayDurationSeconds = 2.0`).

**Pass criteria:**

- The avatar transitions from Thinking to **Success** (`success.png`) briefly, then returns to **Idle**.

**Result:**

```text
M-03 result: ___________
Observed: ____________________________________________________
Anomaly notes (if any): _______________________________________
```

---

### M-04 — Остановить Ollama, отправить сообщение (Error state)

**Expected outcome (verbatim from spec §11):**
> Аватар показывает `Thinking`, затем `Error`.

**Preconditions delta from §1:** Stop Ollama (`ollama stop` or kill the service). Confirm `curl http://localhost:11434/api/tags` no longer responds. Iris Desktop may stay open from M-01–M-03.

**Steps:**

1. With Ollama unreachable, type a message into the chat input.
2. Send it.
3. Observe the avatar transitions.

**Pass criteria:**

- Avatar shows **Thinking** during the attempt.
- After the model call fails, avatar shows **Error** (`error.png`).
- Chat surface displays a readable error message (no raw exception).

**Result:**

```text
M-04 result: ___________
Observed: ____________________________________________________
Anomaly notes (if any): _______________________________________
```

After this scenario, restart Ollama if continuing manual exploration.

---

### M-05 — `Desktop:Avatar:Enabled = false`, перезапустить (Avatar hidden)

**Expected outcome (verbatim from spec §11):**
> Аватар не отображается.

**Preconditions delta from §1:** Close Iris Desktop. Edit (or create) `src/Iris.Desktop/appsettings.local.json` with:

```json
{
  "Desktop": { "Avatar": { "Enabled": false } }
}
```

**Steps:**

1. Save the file.
2. Rebuild the Desktop project (the `appsettings.local.json` is copied with `PreserveNewest`):
   `dotnet build .\src\Iris.Desktop\Iris.Desktop.csproj`.
3. Launch Iris Desktop.

**Pass criteria:**

- The avatar control is **not visible** in any corner.
- The chat surface is fully usable.

**Result:**

```text
M-05 result: ___________
Observed: ____________________________________________________
Anomaly notes (if any): _______________________________________
```

After this scenario, set `Enabled` back to `true` (or delete the `appsettings.local.json` override) and rebuild before continuing.

---

### M-06 — Сменить `Size` на `Large` (Avatar area larger)

**Expected outcome (verbatim from spec §11):**
> Область аватара увеличивается.

**Preconditions delta from §1:** Close Iris Desktop. Edit `src/Iris.Desktop/appsettings.local.json` to:

```json
{
  "Desktop": { "Avatar": { "Enabled": true, "Size": "Large" } }
}
```

**Steps:**

1. Save the file and rebuild the Desktop project.
2. Launch Iris Desktop.
3. Visually compare the avatar size to the default Medium size observed in M-01.

**Pass criteria:**

- The avatar area is visibly larger than under default Medium settings.

**Result:**

```text
M-06 result: ___________
Observed: ____________________________________________________
Anomaly notes (if any): _______________________________________
```

---

### M-07 — Сменить `Position` на `TopLeft` (Avatar moves to top-left)

**Expected outcome (verbatim from spec §11):**
> Аватар перемещается в левый верхний угол.

**Preconditions delta from §1:** Close Iris Desktop. Edit `src/Iris.Desktop/appsettings.local.json` to:

```json
{
  "Desktop": { "Avatar": { "Enabled": true, "Size": "Medium", "Position": "TopLeft" } }
}
```

**Steps:**

1. Save the file and rebuild the Desktop project.
2. Launch Iris Desktop.
3. Visually confirm avatar position.

**Pass criteria:**

- The avatar is anchored in the **top-left** corner of the chat area, not bottom-right.

**Result:**

```text
M-07 result: ___________
Observed: ____________________________________________________
Anomaly notes (if any): _______________________________________
```

After this scenario, restore `appsettings.local.json` to its prior state (or delete it) before resuming normal development.

---

## §3 Recording the Run

After all seven scenarios are executed, append a single dated entry to the top of `.agent/PROJECT_LOG.md`. Use the template below as a starting point.

### 3.1 PROJECT_LOG.md template

```markdown
## 2026-05-01 — Phase 5.5 Manual Smoke M-01–M-07

### Build
- Branch: <branch>
- Commit SHA: <git rev-parse HEAD>
- Verification: `dotnet build .\Iris.slnx` (0/0), `dotnet test .\Iris.slnx` (~141/141), `dotnet format --verify-no-changes` (clean).

### Scenarios
- M-01: Pass | Fail | Anomaly — <observation>
- M-02: Pass | Fail | Anomaly — <observation>
- M-03: Pass | Fail | Anomaly — <observation>
- M-04: Pass | Fail | Anomaly — <observation>
- M-05: Pass | Fail | Anomaly — <observation>
- M-06: Pass | Fail | Anomaly — <observation>
- M-07: Pass | Fail | Anomaly — <observation>

### Path stabilization observation
- Observed iris.db location: <path>
- Stale `iris.db` files cleaned beforehand: yes / no.

### Anomalies
- <list any P0/P1/P2 anomalies, with reproduction steps; otherwise: "none">

### Next
- If all Pass (no P0/P1): mark `Desktop SQLite path uses relative working directory` debt resolved in `.agent/debt_tech_backlog.md`; remove M-01–M-07 from `.agent/overview.md` Known Blockers.
- If anomalies: file new `/spec` for the regression; do not close the debt yet.
```

### 3.2 log_notes.md anomaly template (only when needed)

If any scenario reports Fail or Anomaly, append a separate entry to `.agent/log_notes.md`:

```markdown
## 2026-05-01 — Phase 5.5 Smoke anomaly: <short title>

### Scenario
- M-XX

### Symptom
- <what was observed vs. expected>

### Reproduction
1. <steps>

### Suspected cause
- <hypothesis or "unknown — needs investigation">

### Status
- Open
```

### 3.3 Closing the SQLite-path debt

If M-01 confirmed the new `%APPDATA%\Iris\iris.db` path, append a `### Resolution` block to the existing entry in `.agent/debt_tech_backlog.md`:

```markdown
### Resolution

Resolved 2026-05-01 by Phase 5.5 manual smoke M-01. Default Desktop launch creates `iris.db` at `<ApplicationData>/Iris/iris.db`. Override mechanism via `appsettings.local.json` documented in `src/Iris.Desktop/appsettings.local.example.json`. Relative-path overrides fail fast at startup.
```

### 3.4 Updating overview.md

After all M-01–M-07 are recorded with operator-judged outcomes:

- Update **Current Working Status** to reflect post-smoke state.
- Update **Known Blockers** by removing M-01–M-07 (if all Pass).
- Update **Next Immediate Step** to `/architecture-review` or `/audit` as appropriate.

---

## §4 Failure-Mode Notes

- If `iris.db` is **not** created at `%APPDATA%\Iris\` during M-01, treat as **P0**: the path stabilization is broken. Stop the smoke run; file an anomaly; consider Phase 3 rollback.
- If a relative-path override (e.g. `"Database:ConnectionString": "iris.db"`) launches successfully instead of failing fast, treat as **P0**: the resolver is not enforcing FR-008. Stop; file an anomaly.
- If the avatar transitions hang, time out, or never leave a non-Idle state, treat as **P1** (suggests timer/dispatcher regression similar to historical P1-001). Record exact reproduction.
- If `appsettings.local.json` JSON is malformed, the startup exception is expected. Re-edit and retry.
- Each `appsettings.local.json` edit between M-05 / M-06 / M-07 requires a Desktop rebuild because the file is copied with `PreserveNewest`.
- If `dotnet run` cannot find Avalonia native dependencies after a clean checkout, run `dotnet restore` once.
- The `appsettings.local.example.json` file is documentation only; copy its content (not the file) into a new `appsettings.local.json`.

---

## §5 Out of Scope

The following are deliberately **not** part of this smoke procedure:

- Stress / load testing.
- Multi-window / multi-instance Iris launches.
- Cross-platform (Linux, macOS) verification.
- Avatar animations beyond the discrete state transitions listed.
- Voice or perception integration (deferred to later product phases).
- Performance regression measurement.

If any of these are needed, they require a separate spec.
