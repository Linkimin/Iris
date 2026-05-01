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
