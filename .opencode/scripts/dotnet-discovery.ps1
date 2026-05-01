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
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } |
    Sort-Object FullName |
    Select-Object -ExpandProperty FullName
Write-Output ""

Write-Output "## Project Files"
Get-ChildItem -Recurse -File -Include '*.csproj','*.fsproj','*.vbproj' |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } |
    Sort-Object FullName |
    Select-Object -ExpandProperty FullName
Write-Output ""

Write-Output "## Test Projects"
Get-ChildItem -Recurse -File -Include '*.csproj','*.fsproj','*.vbproj' |
    Where-Object {
        $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' -and
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
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\.git\\|\\.worktrees\\|\\.opencode\\node_modules\\' } |
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
