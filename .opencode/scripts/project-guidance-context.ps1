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
