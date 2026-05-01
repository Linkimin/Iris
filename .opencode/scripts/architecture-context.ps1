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
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } |
    Sort-Object FullName |
    Select-Object -First $FileLimit -ExpandProperty FullName
Write-Output ""

Write-Output "## Project References"
Get-ChildItem -Recurse -File -Include '*.csproj','*.fsproj','*.vbproj' |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } |
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
        $_.FullName -notmatch '\\.worktrees\\' -and
        $_.Name -match 'DependencyInjection|ServiceCollection|Startup|Program'
    } |
    Sort-Object FullName |
    Select-Object -First $FileLimit -ExpandProperty FullName
Write-Output ""

Write-Output "## Architecture / Dependency Tests"
Get-ChildItem -Recurse -File -Include '*.cs' |
    Where-Object {
        $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.opencode\\node_modules\\' -and
        $_.FullName -notmatch '\\.worktrees\\' -and
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
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } |
    Select-String -Pattern $patterns |
    Select-Object -First 240 |
    ForEach-Object { $_.Path + ':' + $_.LineNumber + ': ' + $_.Line.Trim() }
