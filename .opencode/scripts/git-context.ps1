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
