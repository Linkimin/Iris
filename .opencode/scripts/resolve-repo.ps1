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
