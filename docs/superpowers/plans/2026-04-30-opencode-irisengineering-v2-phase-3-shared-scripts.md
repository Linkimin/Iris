# IrisEngineering v2 Phase 3 Shared Scripts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add reusable `.opencode/scripts/*.ps1` context scripts so OpenCode commands can stop embedding large duplicated PowerShell blocks.

**Architecture:** Commands will remain workflow selectors; scripts will own factual repository/context discovery; skills will continue to own methodology; rules will remain hard constraints. Phase 3 creates and verifies the script foundation only, without rewriting the 14 commands yet.

**Tech Stack:** OpenCode markdown command shell injection, PowerShell 7/Windows PowerShell-compatible scripts, Git CLI, .NET CLI discovery, Iris `.agent` memory conventions, `opencode.jsonc` canonical rule loading.

---

## Scope

In scope:

- Create `.opencode/scripts`.
- Create shared read-only context scripts.
- Create a Phase 3 baseline document describing script ownership and command migration targets.
- Verify scripts from repository root and from a nested folder.
- Validate that scripts do not mutate source, memory, git state, or OpenCode command files.

Out of scope:

- Rewriting `.opencode/commands/*.md`.
- Changing `.opencode/skills/**/*.md`.
- Changing `.opencode/rules/*.md`.
- Changing `.opencode/agents` or `.opencode/plugins`.
- Changing `AGENTS.md`.
- Adding external PowerShell modules.
- Adding package dependencies.

## Existing Context

Phase 2 consolidated rule loading so `opencode.jsonc` loads canonical rules only:

1. `.opencode/rules/workflow.md`
2. `.opencode/rules/iris-architecture.md`
3. `.opencode/rules/no-shortcuts.md`
4. `.opencode/rules/memory.md`
5. `.opencode/rules/verification.md`
6. `.opencode/rules/dotnet.md`
7. `.opencode/rules/security.md`
8. `.opencode/rules/review-audit.md`

Current command files still contain large inline PowerShell blocks for:

- repository root resolution;
- git status/diff context;
- agent memory context;
- project guidance context;
- .NET solution/project discovery;
- architecture/dependency discovery.

Phase 3 extracts those repeated blocks into scripts, but leaves command templates untouched until Phase 4 command thinning.

## File Structure

Create:

- `.opencode/docs/irisengineering-v2-phase-3-shared-scripts.md` - factual script ownership map and migration notes.
- `.opencode/scripts/resolve-repo.ps1` - repository root resolver used by every other script.
- `.opencode/scripts/git-context.ps1` - git state, changed files, staged files, untracked files, stats, recent commits.
- `.opencode/scripts/agent-memory-context.ps1` - `.agent` preferred memory reader with `.agents` fallback.
- `.opencode/scripts/project-guidance-context.ps1` - AGENTS, canonical OpenCode rules, and requested skill context.
- `.opencode/scripts/dotnet-discovery.ps1` - solution/project/test/build config discovery and recommended commands.
- `.opencode/scripts/architecture-context.ps1` - project references, DI/host files, architecture tests, suspicious boundary references.

Modify:

- `.agent/PROJECT_LOG.md` - record Phase 3 plan creation.
- `.agent/overview.md` - update current OpenCode v2 next step.

Do not modify:

- `.opencode/commands/*.md`
- `.opencode/skills/**/*.md`
- `.opencode/rules/*.md`
- `.opencode/agents/**`
- `.opencode/plugins/**`
- `opencode.jsonc`
- `AGENTS.md`

## Script Contracts

All Phase 3 scripts must:

- be read-only;
- accept no mandatory parameters;
- resolve the repository root before reading project files;
- write human-readable markdown-ish sections to stdout;
- tolerate missing optional files;
- avoid throwing for expected missing optional context;
- avoid hard-coded absolute repository paths;
- avoid secrets and private config content;
- work when invoked from repository root;
- work when invoked from a nested folder;
- use only built-in PowerShell, Git, and .NET CLI commands.

All scripts must use stable section headings so commands can depend on them later.

## Failure Patterns To Prevent

- Wrong root: script runs from `.opencode/commands` or `src/*` and reads the wrong directory.
- Memory drift: script prefers `.agents` even though `.agent` exists.
- Secret leakage: script prints `.env`, local config, private keys, tokens, or full user secrets.
- Command mutation: script stages, formats, restores, deletes, pushes, or writes files.
- Context flood: script dumps all docs, all memory library files, or huge diffs by default.
- Silent failure: script suppresses an important missing repo/git/tool condition without a visible section.
- Platform brittleness: script depends on PowerShell modules not present on a default Windows machine.

---

### Task 1: Create Phase 3 Script Baseline

**Files:**

- Create: `.opencode/docs/irisengineering-v2-phase-3-shared-scripts.md`

- [ ] **Step 1: Inspect current script directory state**

Run:

```powershell
if (Test-Path .\.opencode\scripts) {
  Get-ChildItem .\.opencode\scripts -Force | Sort-Object Name | Select-Object Mode,Length,Name
} else {
  'NO .opencode/scripts'
}
```

Expected:

```text
NO .opencode/scripts
```

If `.opencode/scripts` already exists, inspect it and adapt this plan without deleting unrelated files.

- [ ] **Step 2: Inspect command context duplication**

Run:

```powershell
Get-ChildItem .\.opencode\commands -File |
  Select-String -Pattern 'powershell','git status','PROJECT_LOG','overview','dotnet','AGENTS','\.agent','\.agents' |
  Select-Object Path,LineNumber,Line
```

Expected:

- command files contain repeated inline PowerShell context blocks;
- this confirms scripts are needed before command thinning.

- [ ] **Step 3: Create baseline document**

Create `.opencode/docs/irisengineering-v2-phase-3-shared-scripts.md`:

```markdown
# IrisEngineering v2 Phase 3 Shared Scripts

## Goal

Create reusable read-only PowerShell context scripts for OpenCode command templates.

## Decision

Phase 3 creates scripts only. Command templates remain unchanged until the command thinning phase.

## Script Ownership

| Script | Owns | Must not own |
|---|---|---|
| `resolve-repo.ps1` | repository root detection and current-location normalization | git summaries, memory reading |
| `git-context.ps1` | git status, changed files, staged files, untracked files, stats, recent commits | full diff output |
| `agent-memory-context.ps1` | `.agent` preferred memory context with `.agents` fallback | memory writes |
| `project-guidance-context.ps1` | AGENTS, canonical rules, requested skills | repository status, build discovery |
| `dotnet-discovery.ps1` | solution/project/test/config discovery | running build/test |
| `architecture-context.ps1` | project references and boundary-sensitive discovery | architecture decisions |

## Command Migration Targets

Later command thinning should replace large inline shell blocks with:

```markdown
!powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/git-context.ps1
!powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/agent-memory-context.ps1
!powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md
!powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/dotnet-discovery.ps1
!powershell -NoProfile -ExecutionPolicy Bypass -File .opencode/scripts/architecture-context.ps1
```

## Safety

Scripts are read-only. They do not write files, stage changes, push, restore, format, apply migrations, or update memory.

## Secrets

Scripts must not print secret-bearing files such as `.env`, `.env.*`, private keys, user secrets, or local appsettings overrides.

## Out Of Scope

- Rewriting commands.
- Changing rules.
- Changing skills.
- Changing agents or plugins.
- Changing `AGENTS.md`.
```

- [ ] **Step 4: Verify baseline document**

Run:

```powershell
Test-Path .\.opencode\docs\irisengineering-v2-phase-3-shared-scripts.md
Get-Content .\.opencode\docs\irisengineering-v2-phase-3-shared-scripts.md -TotalCount 80
```

Expected:

- `Test-Path` returns `True`;
- the document states that commands remain unchanged during Phase 3.

### Task 2: Add Repository Root Resolver

**Files:**

- Create: `.opencode/scripts/resolve-repo.ps1`

- [ ] **Step 1: Create `.opencode/scripts`**

Run:

```powershell
New-Item -ItemType Directory -Force .\.opencode\scripts
```

Expected:

- `.opencode/scripts` exists.

- [ ] **Step 2: Create `resolve-repo.ps1`**

Create `.opencode/scripts/resolve-repo.ps1`:

```powershell
[CmdletBinding()]
param(
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

function Test-IrisGitRoot {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Test-Path -LiteralPath $Path -PathType Container) -and
        (Test-Path -LiteralPath (Join-Path $Path '.git'))
}

function Get-IrisRepositoryRoot {
    if ($env:OPENCODE_PROJECT_ROOT -and (Test-IrisGitRoot -Path $env:OPENCODE_PROJECT_ROOT)) {
        return (Resolve-Path -LiteralPath $env:OPENCODE_PROJECT_ROOT).Path
    }

    if ($PSScriptRoot) {
        $opencodeDir = Split-Path -Parent $PSScriptRoot
        $scriptRepo = Split-Path -Parent $opencodeDir
        if (Test-IrisGitRoot -Path $scriptRepo) {
            return (Resolve-Path -LiteralPath $scriptRepo).Path
        }
    }

    $gitRoot = $null
    try {
        $gitRoot = (& git rev-parse --show-toplevel 2>$null)
    } catch {
        $gitRoot = $null
    }

    if ($LASTEXITCODE -eq 0 -and $gitRoot -and (Test-Path -LiteralPath $gitRoot -PathType Container)) {
        return (Resolve-Path -LiteralPath $gitRoot).Path
    }

    if (Test-IrisGitRoot -Path (Get-Location).Path) {
        return (Resolve-Path -LiteralPath (Get-Location).Path).Path
    }

    return (Get-Location).Path
}

$repo = Get-IrisRepositoryRoot
Set-Location -LiteralPath $repo

if (-not $Quiet) {
    Write-Output ("Repository: " + (Get-Location).Path)
}

return (Get-Location).Path
```

- [ ] **Step 3: Verify resolver from repo root**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\resolve-repo.ps1
```

Expected output contains:

```text
Repository: E:\Work\Iris
E:\Work\Iris
```

The exact drive path may differ if the repository is moved; it must point at the git root.

- [ ] **Step 4: Verify resolver from nested folder**

Run:

```powershell
Push-Location .\src\Iris.Application
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\.opencode\scripts\resolve-repo.ps1
Pop-Location
```

Expected:

- output points to the same repository root;
- no files are created or modified.

### Task 3: Add Git Context Script

**Files:**

- Create: `.opencode/scripts/git-context.ps1`

- [ ] **Step 1: Create `git-context.ps1`**

Create `.opencode/scripts/git-context.ps1`:

```powershell
[CmdletBinding()]
param(
    [int]$RecentCommitCount = 5
)

$ErrorActionPreference = 'Continue'

$repo = . "$PSScriptRoot\resolve-repo.ps1" -Quiet

Write-Output ("Repository: " + $repo)
Write-Output ""

$insideGit = $false
try {
    & git rev-parse --is-inside-work-tree *> $null
    $insideGit = ($LASTEXITCODE -eq 0)
} catch {
    $insideGit = $false
}

if (-not $insideGit) {
    Write-Output "## Git Context"
    Write-Output "Git repository not found."
    exit 0
}

Write-Output "## Git Root"
& git rev-parse --show-toplevel
Write-Output ""

Write-Output "## Git Status"
& git status --short --branch
Write-Output ""

Write-Output "## Changed Files"
& git diff --name-status
Write-Output ""

Write-Output "## Staged Changed Files"
& git diff --cached --name-status
Write-Output ""

Write-Output "## Untracked Files"
& git ls-files --others --exclude-standard
Write-Output ""

Write-Output "## Diff Stat"
& git diff --stat
Write-Output ""

Write-Output "## Staged Diff Stat"
& git diff --cached --stat
Write-Output ""

Write-Output "## Recent Commits"
& git log --oneline -n $RecentCommitCount
```

- [ ] **Step 2: Run git context**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\git-context.ps1
```

Expected:

- output contains `## Git Status`;
- output contains `## Changed Files`;
- output contains `## Recent Commits`;
- output does not include full diff content.

- [ ] **Step 3: Verify git context from nested folder**

Run:

```powershell
Push-Location .\tests
powershell -NoProfile -ExecutionPolicy Bypass -File ..\.opencode\scripts\git-context.ps1
Pop-Location
```

Expected:

- output still reports the repository root;
- output does not fail because the working directory changed.

### Task 4: Add Agent Memory Context Script

**Files:**

- Create: `.opencode/scripts/agent-memory-context.ps1`

- [ ] **Step 1: Create `agent-memory-context.ps1`**

Create `.opencode/scripts/agent-memory-context.ps1`:

```powershell
[CmdletBinding()]
param(
    [int]$OverviewLines = 160,
    [int]$ProjectLogLines = 140,
    [int]$LocalNotesLines = 180,
    [int]$ArchitectureLines = 180,
    [int]$MemoryLibraryFileLimit = 80
)

$ErrorActionPreference = 'Continue'

$repo = . "$PSScriptRoot\resolve-repo.ps1" -Quiet

Write-Output ("Repository: " + $repo)
Write-Output ""

$agentDir = if (Test-Path -LiteralPath '.agent' -PathType Container) {
    '.agent'
} elseif (Test-Path -LiteralPath '.agents' -PathType Container) {
    '.agents'
} else {
    $null
}

if (-not $agentDir) {
    Write-Output "## Agent Memory"
    Write-Output "Agent memory directory not found: neither .agent nor .agents exists."
    exit 0
}

Write-Output ("Agent memory directory: " + $agentDir)
Write-Output ""

$overview = Join-Path $agentDir 'overview.md'
Write-Output "## Agent Overview"
if (Test-Path -LiteralPath $overview) {
    Get-Content -LiteralPath $overview -TotalCount $OverviewLines
} else {
    Write-Output "overview.md not found"
}
Write-Output ""

$projectLog = Join-Path $agentDir 'PROJECT_LOG.md'
Write-Output "## Project Log"
if (Test-Path -LiteralPath $projectLog) {
    Get-Content -LiteralPath $projectLog -TotalCount $ProjectLogLines
} else {
    Write-Output "PROJECT_LOG.md not found"
}
Write-Output ""

$localNotesCandidates = @(
    (Join-Path $agentDir 'local_notes.md'),
    (Join-Path $agentDir 'log_notes.md')
)

$localNotes = $localNotesCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
Write-Output "## Local Notes"
if ($localNotes) {
    Write-Output ("Source: " + $localNotes)
    Get-Content -LiteralPath $localNotes -TotalCount $LocalNotesLines
} else {
    Write-Output "local_notes.md / log_notes.md not found"
}
Write-Output ""

$architecture = Join-Path $agentDir 'architecture.md'
Write-Output "## Architecture Memory"
if (Test-Path -LiteralPath $architecture) {
    Get-Content -LiteralPath $architecture -TotalCount $ArchitectureLines
} else {
    Write-Output "architecture.md not found"
}
Write-Output ""

$memLibrary = Join-Path $agentDir 'mem_library'
Write-Output "## Memory Library Files"
if (Test-Path -LiteralPath $memLibrary -PathType Container) {
    Get-ChildItem -LiteralPath $memLibrary -File |
        Sort-Object Name |
        Select-Object -First $MemoryLibraryFileLimit -ExpandProperty Name
} else {
    Write-Output "mem_library not found"
}
```

- [ ] **Step 2: Run agent memory context**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\agent-memory-context.ps1
```

Expected:

- output says `Agent memory directory: .agent` when `.agent` exists;
- output contains `## Agent Overview`;
- output contains `## Project Log`;
- output contains `## Memory Library Files`;
- script does not write memory files.

- [ ] **Step 3: Verify memory script fallback text is explicit**

Run:

```powershell
Select-String -Path .\.opencode\scripts\agent-memory-context.ps1 -Pattern 'neither .agent nor .agents exists','local_notes.md / log_notes.md not found'
```

Expected:

- both messages are present in script source.

### Task 5: Add Project Guidance Context Script

**Files:**

- Create: `.opencode/scripts/project-guidance-context.ps1`

- [ ] **Step 1: Create `project-guidance-context.ps1`**

Create `.opencode/scripts/project-guidance-context.ps1`:

```powershell
[CmdletBinding()]
param(
    [string[]]$SkillPath = @(),
    [int]$AgentsLines = 240,
    [int]$RuleLines = 180,
    [int]$SkillLines = 220
)

$ErrorActionPreference = 'Continue'

$repo = . "$PSScriptRoot\resolve-repo.ps1" -Quiet

Write-Output ("Repository: " + $repo)
Write-Output ""

Write-Output "## AGENTS.md"
if (Test-Path -LiteralPath 'AGENTS.md') {
    Get-Content -LiteralPath 'AGENTS.md' -TotalCount $AgentsLines
} else {
    Write-Output "AGENTS.md not found"
}
Write-Output ""

Write-Output "## OpenCode Loaded Rules"
if (Test-Path -LiteralPath 'opencode.jsonc') {
    try {
        $config = Get-Content -LiteralPath 'opencode.jsonc' -Raw | ConvertFrom-Json
        $instructions = @($config.instructions)
        if ($instructions.Count -eq 0) {
            Write-Output "No instructions configured."
        }

        foreach ($instruction in $instructions) {
            Write-Output ("### " + $instruction)
            if (Test-Path -LiteralPath $instruction) {
                Get-Content -LiteralPath $instruction -TotalCount $RuleLines
            } else {
                Write-Output "Rule file not found."
            }
            Write-Output ""
        }
    } catch {
        Write-Output ("Failed to parse opencode.jsonc: " + $_.Exception.Message)
    }
} else {
    Write-Output "opencode.jsonc not found"
}

Write-Output ""
Write-Output "## Requested Skills"
if ($SkillPath.Count -eq 0) {
    Write-Output "No explicit skill paths requested."
    exit 0
}

foreach ($skill in $SkillPath) {
    Write-Output ("### " + $skill)
    if (Test-Path -LiteralPath $skill) {
        Get-Content -LiteralPath $skill -TotalCount $SkillLines
    } else {
        Write-Output "Skill file not found."
    }
    Write-Output ""
}
```

- [ ] **Step 2: Run project guidance with one skill**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md
```

Expected:

- output contains `## AGENTS.md`;
- output contains `## OpenCode Loaded Rules`;
- output contains `.opencode/rules/workflow.md`;
- output contains `## Requested Skills`;
- output contains `.opencode/skills/iris-engineering/SKILL.md`.

- [ ] **Step 3: Verify missing skill path is controlled**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\project-guidance-context.ps1 -SkillPath .opencode/skills/missing/SKILL.md
```

Expected:

- output contains `Skill file not found.`;
- command exits without crashing.

### Task 6: Add .NET Discovery Script

**Files:**

- Create: `.opencode/scripts/dotnet-discovery.ps1`

- [ ] **Step 1: Create `dotnet-discovery.ps1`**

Create `.opencode/scripts/dotnet-discovery.ps1`:

```powershell
[CmdletBinding()]
param(
    [int]$ProjectReferenceLimit = 120
)

$ErrorActionPreference = 'Continue'

$repo = . "$PSScriptRoot\resolve-repo.ps1" -Quiet

Write-Output ("Repository: " + $repo)
Write-Output ""

Write-Output "## .NET SDK"
try {
    & dotnet --version
} catch {
    Write-Output ("dotnet unavailable: " + $_.Exception.Message)
}
Write-Output ""

Write-Output "## Solution Files"
Get-ChildItem -Recurse -File -Include '*.sln','*.slnx' |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.opencode\\node_modules\\' } |
    Sort-Object FullName |
    Select-Object -ExpandProperty FullName
Write-Output ""

Write-Output "## Project Files"
Get-ChildItem -Recurse -File -Include '*.csproj','*.fsproj','*.vbproj' |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.opencode\\node_modules\\' } |
    Sort-Object FullName |
    Select-Object -ExpandProperty FullName
Write-Output ""

Write-Output "## Test Projects"
Get-ChildItem -Recurse -File -Include '*.csproj','*.fsproj','*.vbproj' |
    Where-Object {
        $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.opencode\\node_modules\\' -and
        $_.FullName -match '(Tests|Test|\.Tests)'
    } |
    Sort-Object FullName |
    Select-Object -ExpandProperty FullName
Write-Output ""

Write-Output "## Build Configuration Files"
foreach ($file in @('global.json','Directory.Build.props','Directory.Build.targets','Directory.Packages.props','.editorconfig','nuget.config')) {
    if (Test-Path -LiteralPath $file) {
        Write-Output $file
    }
}
Write-Output ""

Write-Output "## Project References and Packages"
Get-ChildItem -Recurse -File -Include '*.csproj','*.fsproj','*.vbproj' |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.opencode\\node_modules\\' } |
    Sort-Object FullName |
    Select-Object -First $ProjectReferenceLimit |
    ForEach-Object {
        Write-Output ("### " + $_.FullName)
        Select-String -LiteralPath $_.FullName -Pattern '<ProjectReference','<PackageReference','<FrameworkReference' |
            ForEach-Object { $_.Line.Trim() }
        Write-Output ""
    }

Write-Output "## Suggested Iris Verification Commands"
if (Test-Path -LiteralPath '.\Iris.slnx') {
    Write-Output 'dotnet build .\Iris.slnx'
    Write-Output 'dotnet test .\Iris.slnx'
    Write-Output 'dotnet format .\Iris.slnx --verify-no-changes'
} else {
    Write-Output 'No Iris.slnx found. Choose the closest solution or project file from discovery output.'
}
```

- [ ] **Step 2: Run .NET discovery**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\dotnet-discovery.ps1
```

Expected:

- output contains `## Solution Files`;
- output includes `Iris.slnx`;
- output contains `## Test Projects`;
- output contains `## Suggested Iris Verification Commands`;
- script does not run build/test/format.

### Task 7: Add Architecture Context Script

**Files:**

- Create: `.opencode/scripts/architecture-context.ps1`

- [ ] **Step 1: Create `architecture-context.ps1`**

Create `.opencode/scripts/architecture-context.ps1`:

```powershell
[CmdletBinding()]
param(
    [int]$FileLimit = 160
)

$ErrorActionPreference = 'Continue'

$repo = . "$PSScriptRoot\resolve-repo.ps1" -Quiet

Write-Output ("Repository: " + $repo)
Write-Output ""

Write-Output "## Solution and Project Files"
Get-ChildItem -Recurse -File -Include '*.sln','*.slnx','*.csproj','*.fsproj','*.vbproj','Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json' |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.opencode\\node_modules\\' } |
    Sort-Object FullName |
    Select-Object -First $FileLimit -ExpandProperty FullName
Write-Output ""

Write-Output "## Project References"
Get-ChildItem -Recurse -File -Include '*.csproj','*.fsproj','*.vbproj' |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.opencode\\node_modules\\' } |
    Sort-Object FullName |
    ForEach-Object {
        Write-Output ("### " + $_.FullName)
        Select-String -LiteralPath $_.FullName -Pattern '<ProjectReference','<PackageReference','<FrameworkReference' |
            ForEach-Object { $_.Line.Trim() }
        Write-Output ""
    }

Write-Output "## Dependency Injection / Host Files"
Get-ChildItem -Recurse -File -Include '*.cs' |
    Where-Object {
        $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.opencode\\node_modules\\' -and
        $_.Name -match 'DependencyInjection|ServiceCollection|Startup|Program'
    } |
    Sort-Object FullName |
    Select-Object -First $FileLimit -ExpandProperty FullName
Write-Output ""

Write-Output "## Architecture / Dependency Tests"
Get-ChildItem -Recurse -File -Include '*.cs' |
    Where-Object {
        $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.opencode\\node_modules\\' -and
        $_.FullName -match 'Architecture|Dependency|Boundary|Reference' -and
        $_.FullName -match 'test|tests'
    } |
    Sort-Object FullName |
    Select-Object -First $FileLimit -ExpandProperty FullName
Write-Output ""

Write-Output "## Suspicious Namespace / Boundary References"
$patterns = @(
    'using Microsoft.EntityFrameworkCore',
    'using System.Data',
    'using Iris.Persistence',
    'using Iris.ModelGateway',
    'using Iris.Perception',
    'using Iris.Tools',
    'using Iris.Voice',
    'using Iris.Infrastructure',
    'DbContext',
    'HttpClient',
    'Ollama',
    'LmStudio',
    'OpenAi',
    'File\.',
    'Directory\.',
    'Process\.Start'
)

Get-ChildItem -Recurse -File -Include '*.cs' |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.opencode\\node_modules\\' } |
    Select-String -Pattern $patterns |
    Select-Object -First 240 |
    ForEach-Object { $_.Path + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }
```

- [ ] **Step 2: Run architecture context**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\architecture-context.ps1
```

Expected:

- output contains `## Project References`;
- output contains `## Dependency Injection / Host Files`;
- output contains `## Architecture / Dependency Tests`;
- output contains `## Suspicious Namespace / Boundary References`;
- script does not decide pass/fail by itself.

### Task 8: Verify Script Safety And Scope

**Files:**

- Inspect: `.opencode/scripts/*.ps1`
- Inspect: `.opencode/docs/irisengineering-v2-phase-3-shared-scripts.md`

- [ ] **Step 1: Verify all expected scripts exist**

Run:

```powershell
$expected = @(
  '.opencode/scripts/resolve-repo.ps1',
  '.opencode/scripts/git-context.ps1',
  '.opencode/scripts/agent-memory-context.ps1',
  '.opencode/scripts/project-guidance-context.ps1',
  '.opencode/scripts/dotnet-discovery.ps1',
  '.opencode/scripts/architecture-context.ps1'
)

$expected | ForEach-Object {
  [PSCustomObject]@{
    Path = $_
    Exists = Test-Path $_
    Lines = if (Test-Path $_) { (Get-Content $_).Count } else { 0 }
  }
}
```

Expected:

- every `Exists` value is `True`;
- script sizes are readable and focused.

- [ ] **Step 2: Verify scripts parse**

Run:

```powershell
Get-ChildItem .\.opencode\scripts -File -Filter '*.ps1' |
  ForEach-Object {
    $errors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content $_.FullName -Raw), [ref]$errors) > $null
    [PSCustomObject]@{
      Name = $_.Name
      ParseErrors = $errors.Count
    }
  }
```

Expected:

- every `ParseErrors` value is `0`.

- [ ] **Step 3: Verify scripts do not contain forbidden mutating commands**

Run:

```powershell
Get-ChildItem .\.opencode\scripts -File -Filter '*.ps1' |
  Select-String -Pattern 'git push','git clean','git reset --hard','Remove-Item','Set-Content','Add-Content','Out-File','dotnet format(?! .*--verify-no-changes)','dotnet restore','dotnet add package','Update-Database'
```

Expected:

- no output.

- [ ] **Step 4: Verify scripts avoid hard-coded repository path**

Run:

```powershell
Get-ChildItem .\.opencode\scripts -File -Filter '*.ps1' |
  Select-String -Pattern 'E:\\Work\\Iris','C:\\Users\\User'
```

Expected:

- no output.

- [ ] **Step 5: Verify scripts avoid known secret-bearing files**

Run:

```powershell
Get-ChildItem .\.opencode\scripts -File -Filter '*.ps1' |
  Select-String -Pattern '\.env','appsettings\.local','secrets\.json','id_rsa','private key'
```

Expected:

- no output.

If a future script must mention a secret-bearing file, it must do so only in an exclusion or warning context and the verification command must be narrowed accordingly.

- [ ] **Step 6: Verify out-of-scope files were not modified**

Run:

```powershell
git diff --name-only -- .opencode/commands .opencode/skills .opencode/rules .opencode/agents .opencode/plugins opencode.jsonc AGENTS.md
```

Expected:

- no output for Phase 3 changes.
- Existing unrelated `AGENTS.md` user changes may appear in `git status`, but Phase 3 must not edit it.

### Task 9: Run All Scripts As OpenCode Would Invoke Them

**Files:**

- Execute: `.opencode/scripts/*.ps1`

- [ ] **Step 1: Run every script from repository root**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\resolve-repo.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\git-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\agent-memory-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\dotnet-discovery.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\architecture-context.ps1
```

Expected:

- every command exits successfully;
- output is readable;
- output uses stable headings.

- [ ] **Step 2: Run representative scripts from a nested folder**

Run:

```powershell
Push-Location .\src\Iris.Application
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\.opencode\scripts\git-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\.opencode\scripts\agent-memory-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\.opencode\scripts\dotnet-discovery.ps1
Pop-Location
```

Expected:

- every command still resolves the repository root;
- every command exits successfully.

- [ ] **Step 3: Inspect git state after script execution**

Run:

```powershell
git status --short --branch
git diff --name-only -- .opencode/scripts .opencode/docs .agent
```

Expected:

- only planned files are changed;
- script execution itself does not create extra changes.

### Task 10: Update Agent Memory

**Files:**

- Modify: `.agent/PROJECT_LOG.md`
- Modify: `.agent/overview.md`

- [ ] **Step 1: Prepend `PROJECT_LOG.md` entry**

Add this entry near the top of `.agent/PROJECT_LOG.md`:

```markdown
## 2026-04-30 - OpenCode IrisEngineering v2 Phase 3 shared scripts plan

### Changed
- Created the Phase 3 implementation plan for shared OpenCode PowerShell context scripts.
- Scoped Phase 3 to script foundation only: repository resolution, git context, agent memory, project guidance, .NET discovery, and architecture context.
- Deferred command thinning to the next phase so scripts can be verified independently before command migration.

### Files
- docs/superpowers/plans/2026-04-30-opencode-irisengineering-v2-phase-3-shared-scripts.md
- .agent/PROJECT_LOG.md
- .agent/overview.md

### Validation
- Inspected current `.opencode/commands` duplication and confirmed repeated inline PowerShell context blocks.
- Used Context7 OpenCode docs to confirm command shell output injection and `opencode.jsonc` instruction behavior.
- Did not run .NET build/test because this iteration only creates a plan and local memory updates.

### Next
- Implement Phase 3 shared scripts, then use them in the command thinning phase.
```

- [ ] **Step 2: Update `overview.md`**

Update `.agent/overview.md` so:

- `Current Implementation Target` mentions Phase 3 shared scripts plan is ready;
- `Next Immediate Step` says to implement Phase 3 shared scripts before command thinning.

- [ ] **Step 3: Verify memory diff**

Run:

```powershell
git diff -- .agent/PROJECT_LOG.md .agent/overview.md
```

Expected:

- only factual Phase 3 planning notes were added;
- no unrelated memory content was removed.

## Acceptance Criteria

- Phase 3 plan exists under `docs/superpowers/plans`.
- Phase 3 baseline document exists under `.opencode/docs`.
- `.opencode/scripts` exists.
- Six script files exist:
  - `resolve-repo.ps1`
  - `git-context.ps1`
  - `agent-memory-context.ps1`
  - `project-guidance-context.ps1`
  - `dotnet-discovery.ps1`
  - `architecture-context.ps1`
- Scripts parse with zero PowerShell parser errors.
- Scripts run from repository root.
- Representative scripts run from nested folders.
- Scripts do not contain mutating commands.
- Scripts do not contain hard-coded `E:\Work\Iris` or `C:\Users\User`.
- Scripts do not print known secret-bearing file content.
- `.opencode/commands` are not modified in Phase 3.
- `.opencode/skills`, `.opencode/rules`, `.opencode/agents`, `.opencode/plugins`, `opencode.jsonc`, and `AGENTS.md` are not modified in Phase 3.
- `.agent` is updated with factual plan status.

## Validation Commands

Run at the end:

```powershell
node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('opencode.jsonc','utf8')); console.log('opencode.jsonc ok')"

Get-ChildItem .\.opencode\scripts -File -Filter '*.ps1' |
  ForEach-Object {
    $errors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content $_.FullName -Raw), [ref]$errors) > $null
    [PSCustomObject]@{ Name = $_.Name; ParseErrors = $errors.Count }
  }

Get-ChildItem .\.opencode\scripts -File -Filter '*.ps1' |
  Select-String -Pattern 'git push','git clean','git reset --hard','Remove-Item','Set-Content','Add-Content','Out-File','dotnet restore','dotnet add package','Update-Database'

Get-ChildItem .\.opencode\scripts -File -Filter '*.ps1' |
  Select-String -Pattern 'E:\\Work\\Iris','C:\\Users\\User'

powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\resolve-repo.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\git-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\agent-memory-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\project-guidance-context.ps1 -SkillPath .opencode/skills/iris-engineering/SKILL.md
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\dotnet-discovery.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.opencode\scripts\architecture-context.ps1

Push-Location .\src\Iris.Application
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\.opencode\scripts\git-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\.opencode\scripts\agent-memory-context.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File ..\..\.opencode\scripts\dotnet-discovery.ps1
Pop-Location

git diff --name-only -- .opencode/commands .opencode/skills .opencode/rules .opencode/agents .opencode/plugins opencode.jsonc AGENTS.md
git status --short --branch
```

For branch-completion confidence, run:

```powershell
dotnet build .\Iris.slnx --no-restore
dotnet test .\Iris.slnx --no-restore
```

If only scripts/docs changed and .NET checks are skipped, report that explicitly.

## Commit Guidance

Recommended commit split:

```powershell
git add .opencode/docs/irisengineering-v2-phase-3-shared-scripts.md docs/superpowers/plans/2026-04-30-opencode-irisengineering-v2-phase-3-shared-scripts.md
git commit -m "docs: plan OpenCode shared scripts"

git add .opencode/scripts
git commit -m "chore: add OpenCode shared context scripts"
```

Do not commit automatically unless the user asks.

## Self-Review Checklist

- [ ] The plan keeps Phase 3 limited to shared scripts.
- [ ] Command thinning remains deferred.
- [ ] Every script has one clear responsibility.
- [ ] Every script is read-only.
- [ ] Repository root resolution does not rely on hard-coded absolute paths.
- [ ] `.agent` is preferred over `.agents`.
- [ ] Scripts avoid secret-bearing files.
- [ ] Validation commands are exact.
- [ ] Out-of-scope files are protected.
- [ ] Phase 4 command thinning has enough script contracts to proceed.
