# Phase 7 — Regression Safeguards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add repo-level safeguards — architecture tests, centralized build properties, package version management, editorconfig, architecture docs, and CI — to prevent regression as Iris grows past the OpenCode v2 migration.

**Architecture:** Six independent subsystems deployed sequentially: (1) `Directory.Build.props` centralizes MSBuild properties, (2) `Directory.Packages.props` centralizes package versions, (3) `.editorconfig` enforces code style, (4) `Iris.Architecture.Tests` enforces dependency/boundary rules, (5) `docs/architecture.md` provides public-facing architecture documentation, (6) `.github/workflows/ci.yml` adds automated CI. Each subsystem is independently verifiable.

**Tech Stack:** .NET 10, MSBuild, xUnit, NetArchTest.Rules (or custom reflection), GitHub Actions, C# code style conventions.

---

## Current State Summary

| Asset | Exists? | State |
|---|---|---|
| `Directory.Build.props` | ❌ No | 13 src/ + 5 test/ .csproj files duplicate `<TargetFramework>`, `<ImplicitUsings>`, `<Nullable>` |
| `Directory.Packages.props` | ❌ No | Package versions inconsistent: `Iris.Architecture.Tests` uses older coverlet 6.0.4 / Test.Sdk 17.14.1 while other test projects use 10.0.0 / 18.4.0 |
| `.editorconfig` | ❌ No | No code style enforcement |
| `Iris.Architecture.Tests` | ⚠️ Stub | Project exists, has only `UnitTest1.cs` with empty `Test1()`. No architecture rules enforced |
| `docs/architecture.md` | ❌ No | Only `.agent/architecture.md` exists (agent memory, not public-facing docs) |
| CI (`.github/workflows/`) | ❌ No | No automated build/test pipeline |

---

## File Structure

### Files to create

| File | Responsibility |
|---|---|
| `Directory.Build.props` | Centralize `<TargetFramework>`, `<ImplicitUsings>`, `<Nullable>`, `<AnalysisLevel>`, `<TreatWarningsAsErrors>` for all projects |
| `tests/Directory.Build.props` | Add `<IsPackable>false</IsPackable>` and test-specific defaults for all test projects |
| `Directory.Packages.props` | Centralize package versions: pin all NuGet versions in one file, remove `Version=` from individual `<PackageReference>` elements |
| `.editorconfig` | C# code style: naming conventions, var preferences, using ordering, whitespace rules |
| `tests/Iris.Architecture.Tests/DependencyDirectionTests.cs` | Verify Domain→Shared only, Application→Domain+Shared only, no adapter→adapter, no host→host |
| `tests/Iris.Architecture.Tests/ForbiddenNamespaceTests.cs` | Verify EF Core not in Domain, Avalonia not outside Desktop, provider types not in Application |
| `tests/Iris.Architecture.Tests/ProjectReferenceTests.cs` | Verify adapter→adapter, host→host, Application→adapter references are absent |
| `tests/Iris.Architecture.Tests/ShortcutDetectionTests.cs` | Verify Desktop doesn't reference IrisDbContext, OllamaChatModelClient directly |
| `docs/architecture.md` | Public-facing architecture document synthesized from `.agent/architecture.md` |
| `.github/workflows/ci.yml` | Build + test on push/PR, dotnet format verify |

### Files to modify

| File | Change |
|---|---|
| `src/*/*.csproj` (13 files) | Remove `<TargetFramework>`, `<ImplicitUsings>`, `<Nullable>` — now inherited from `Directory.Build.props` |
| `tests/*/*.csproj` (5 files) | Remove `<TargetFramework>`, `<ImplicitUsings>`, `<Nullable>`, `<IsPackable>` — now inherited from `Directory.Build.props` |
| All `.csproj` files with `<PackageReference Version="...">` | Remove `Version=` attribute — now inherited from `Directory.Packages.props` |
| `tests/Iris.Architecture.Tests/Iris.Architecture.Tests.csproj` | Add `NetArchTest.Rules` PackageReference, add project references to all src projects for inspection |
| `tests/Iris.Architecture.Tests/UnitTest1.cs` | Delete (replaced by real test files) |

---

## Tasks

### Task 7.1: Create root Directory.Build.props

**Files:**
- Create: `Directory.Build.props`
- Modify: `src/*/*.csproj` (13 files) — remove duplicate properties

- [ ] **Step 1: Write Directory.Build.props**

Create `Directory.Build.props` at the repo root:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Remove duplicate properties from all src/*/*.csproj files**

For each of these 13 files, remove the `<TargetFramework>`, `<ImplicitUsings>`, `<Nullable>` lines from `<PropertyGroup>`:

Files:
- `src/Iris.Api/Iris.Api.csproj`
- `src/Iris.Application/Iris.Application.csproj`
- `src/Iris.Desktop/Iris.Desktop.csproj`
- `src/Iris.Domain/Iris.Domain.csproj`
- `src/Iris.Infrastructure/Iris.Infrastructure.csproj`
- `src/Iris.ModelGateway/Iris.ModelGateway.csproj`
- `src/Iris.Perception/Iris.Perception.csproj`
- `src/Iris.Persistence/Iris.Persistence.csproj`
- `src/Iris.Shared/Iris.Shared.csproj`
- `src/Iris.SiRuntimeGateway/Iris.SiRuntimeGateway.csproj`
- `src/Iris.Tools/Iris.Tools.csproj`
- `src/Iris.Voice/Iris.Voice.csproj`
- `src/Iris.Worker/Iris.Worker.csproj`

Example for `Iris.Domain.csproj` — change from:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

To:

```xml
<Project Sdk="Microsoft.NET.Sdk">
</Project>
```

For files that have additional properties in `<PropertyGroup>` (like `Iris.Desktop.csproj` with `OutputType`, `AvaloniaUseCompiledBindingsByDefault`), keep only the project-specific properties:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>
  ...rest unchanged...
</Project>
```

For `Iris.Worker.csproj`, keep `<UserSecretsId>`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <UserSecretsId>dotnet-Iris.Worker-a2fd4fd5-9ce5-4be6-a68b-3c2631a2e7a0</UserSecretsId>
  </PropertyGroup>
  ...rest unchanged...
</Project>
```

- [ ] **Step 3: Build and verify**

```powershell
dotnet build .\Iris.slnx
```

Expected: Build succeeds with 0 errors. All projects inherit `net10.0`, `enable` from `Directory.Build.props`.

- [ ] **Step 4: Commit**

```bash
git add Directory.Build.props src/*/*.csproj
git commit -m "feat: add Directory.Build.props, remove duplicate MSBuild properties from 13 src projects"
```

---

### Task 7.2: Create tests/Directory.Build.props

**Files:**
- Create: `tests/Directory.Build.props`
- Modify: `tests/*/*.csproj` (5 files) — remove duplicate properties

- [ ] **Step 1: Write tests/Directory.Build.props**

```xml
<Project>
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
</Project>
```

Note: `<TargetFramework>`, `<ImplicitUsings>`, `<Nullable>` are already inherited from the root `Directory.Build.props`. This file only adds test-specific properties.

- [ ] **Step 2: Remove duplicate properties from all test .csproj files**

Remove `<TargetFramework>`, `<ImplicitUsings>`, `<Nullable>`, and `<IsPackable>` from:

- `tests/Iris.Application.Tests/Iris.Application.Tests.csproj`
- `tests/Iris.Architecture.Tests/Iris.Architecture.Tests.csproj`
- `tests/Iris.Domain.Tests/Iris.Domain.Tests.csproj`
- `tests/Iris.Infrastructure.Tests/Iris.Infrastructure.Tests.csproj`
- `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj`

Example for `Iris.Domain.Tests.csproj` — change from:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
```

To:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
  </PropertyGroup>
```

If `<PropertyGroup>` becomes empty, remove it entirely:

```xml
<Project Sdk="Microsoft.NET.Sdk">
```

- [ ] **Step 3: Build and verify tests still compile**

```powershell
dotnet build .\Iris.slnx
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add tests/Directory.Build.props tests/*/*.csproj
git commit -m "feat: add tests/Directory.Build.props, remove duplicate properties from 5 test projects"
```

---

### Task 7.3: Create Directory.Packages.props with centralized versions

**Files:**
- Create: `Directory.Packages.props`
- Modify: All `.csproj` files with `<PackageReference Version="...">` — remove `Version=` attribute

- [ ] **Step 1: Collect all package references and versions**

From the inspected .csproj files, the full package list with their versions:

| Package | Version | Used by |
|---|---|---|
| `Avalonia` | 12.0.1 | Desktop |
| `Avalonia.Desktop` | 12.0.1 | Desktop |
| `Avalonia.Themes.Fluent` | 12.0.1 | Desktop |
| `Avalonia.Fonts.Inter` | 12.0.1 | Desktop |
| `Avalonia.Diagnostics` | 11.3.14 | Desktop |
| `CommunityToolkit.Mvvm` | 8.4.2 | Desktop |
| `coverlet.collector` | 10.0.0 | Domain.Tests, Application.Tests, Infrastructure.Tests, IntegrationTests |
| `coverlet.collector` | 6.0.4 | Architecture.Tests (outdated — use 10.0.0) |
| `Microsoft.AspNetCore.OpenApi` | 10.0.7 | Api |
| `Microsoft.EntityFrameworkCore` | 10.0.7 | Persistence |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.7 | Persistence |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.7 | Persistence, IntegrationTests |
| `Microsoft.Extensions.Configuration.Binder` | 10.0.7 | Desktop |
| `Microsoft.Extensions.Configuration.Json` | 10.0.7 | Desktop |
| `Microsoft.Extensions.DependencyInjection` | 10.0.7 | Desktop, Application.Tests |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.7 | Application, Persistence, ModelGateway |
| `Microsoft.Extensions.Hosting` | 10.0.7 | Worker |
| `Microsoft.Extensions.Http` | 10.0.7 | ModelGateway |
| `Microsoft.Extensions.Options` | 10.0.7 | Persistence |
| `Microsoft.NET.Test.Sdk` | 18.4.0 | Domain.Tests, Application.Tests, Infrastructure.Tests, IntegrationTests |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Architecture.Tests (outdated — use 18.4.0) |
| `Tmds.DBus.Protocol` | 0.92.0 | Desktop |
| `xunit` | 2.9.3 | All test projects |
| `xunit.runner.visualstudio` | 3.1.5 | Domain.Tests, Application.Tests, Infrastructure.Tests, IntegrationTests |
| `xunit.runner.visualstudio` | 3.1.4 | Architecture.Tests (outdated — use 3.1.5) |

- [ ] **Step 2: Write Directory.Packages.props**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- Avalonia -->
    <PackageVersion Include="Avalonia" Version="12.0.1" />
    <PackageVersion Include="Avalonia.Desktop" Version="12.0.1" />
    <PackageVersion Include="Avalonia.Themes.Fluent" Version="12.0.1" />
    <PackageVersion Include="Avalonia.Fonts.Inter" Version="12.0.1" />
    <PackageVersion Include="Avalonia.Diagnostics" Version="11.3.14" />

    <!-- CommunityToolkit -->
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.2" />

    <!-- Testing -->
    <PackageVersion Include="coverlet.collector" Version="10.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.4.0" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />

    <!-- Microsoft Extensions -->
    <PackageVersion Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="10.0.7" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.7" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="10.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.7" />

    <!-- ASP.NET / EF Core -->
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.7" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.7" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.7" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.7" />

    <!-- Desktop -->
    <PackageVersion Include="Tmds.DBus.Protocol" Version="0.92.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Remove Version= from all <PackageReference> elements**

For every `.csproj` that has `<PackageReference Include="X" Version="Y.Z" />`, remove the `Version="Y.Z"` attribute and any child elements that are specific to versions (keep `<PrivateAssets>`, `<IncludeAssets>` if present).

Example for `Iris.Architecture.Tests.csproj`:

```xml
<!-- Before -->
<PackageReference Include="coverlet.collector" Version="6.0.4" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />

<!-- After -->
<PackageReference Include="coverlet.collector">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Apply this pattern across ALL projects. Files to edit:

| File | PackageReferences to update |
|---|---|
| `src/Iris.Api/Iris.Api.csproj` | `Microsoft.AspNetCore.OpenApi` |
| `src/Iris.Application/Iris.Application.csproj` | `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `src/Iris.Desktop/Iris.Desktop.csproj` | `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`, `Avalonia.Diagnostics`, `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.Configuration.Json`, `Microsoft.Extensions.DependencyInjection`, `Tmds.DBus.Protocol` |
| `src/Iris.ModelGateway/Iris.ModelGateway.csproj` | `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Http` |
| `src/Iris.Persistence/Iris.Persistence.csproj` | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Options` |
| `src/Iris.Worker/Iris.Worker.csproj` | `Microsoft.Extensions.Hosting` |
| `tests/Iris.Application.Tests/Iris.Application.Tests.csproj` | `coverlet.collector`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` |
| `tests/Iris.Architecture.Tests/Iris.Architecture.Tests.csproj` | `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` |
| `tests/Iris.Domain.Tests/Iris.Domain.Tests.csproj` | `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` |
| `tests/Iris.Infrastructure.Tests/Iris.Infrastructure.Tests.csproj` | `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` |
| `tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj` | `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `Microsoft.EntityFrameworkCore.Sqlite`, `xunit`, `xunit.runner.visualstudio` |

- [ ] **Step 4: Build, restore, and verify**

```powershell
dotnet restore .\Iris.slnx
dotnet build .\Iris.slnx
```

Expected: Restore and build succeed. Package versions are resolved from `Directory.Packages.props`. Architecture.Tests now uses coverlet 10.0.0, Test.Sdk 18.4.0, xunit.runner.visualstudio 3.1.5 (upgraded from outdated versions).

- [ ] **Step 5: Run tests to confirm nothing broke**

```powershell
dotnet test .\Iris.slnx
```

Expected: All tests pass (same count as before package upgrade — the empty Architecture.Tests.UnitTest1.Test1 still passes).

- [ ] **Step 6: Commit**

```bash
git add Directory.Packages.props
git add src/Iris.Api/Iris.Api.csproj src/Iris.Application/Iris.Application.csproj src/Iris.Desktop/Iris.Desktop.csproj src/Iris.ModelGateway/Iris.ModelGateway.csproj src/Iris.Persistence/Iris.Persistence.csproj src/Iris.Worker/Iris.Worker.csproj
git add tests/Iris.Application.Tests/Iris.Application.Tests.csproj tests/Iris.Architecture.Tests/Iris.Architecture.Tests.csproj tests/Iris.Domain.Tests/Iris.Domain.Tests.csproj tests/Iris.Infrastructure.Tests/Iris.Infrastructure.Tests.csproj tests/Iris.IntegrationTests/Iris.Integration.Tests.csproj
git commit -m "feat: add Directory.Packages.props, centralize all NuGet versions, upgrade Architecture.Tests packages"
```

---

### Task 7.4: Create .editorconfig

**Files:**
- Create: `.editorconfig`

- [ ] **Step 1: Write .editorconfig**

```ini
# Top-level .editorconfig for Iris
root = true

# Default for all files
[*]
charset = utf-8
end_of_line = crlf
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

# C# source files
[*.cs]
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = true
csharp_style_var_for_built_in_types = true:warning
csharp_style_var_when_type_is_apparent = true:warning
csharp_style_var_elsewhere = false:warning
csharp_style_prefer_switch_expression = true:suggestion
csharp_style_prefer_pattern_matching = true:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_constructors = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = when_on_single_line:suggestion
csharp_style_expression_bodied_indexers = when_on_single_line:suggestion
csharp_style_expression_bodied_accessors = when_on_single_line:suggestion
csharp_prefer_braces = true:suggestion
csharp_prefer_simple_using_statement = true:suggestion
csharp_using_directive_placement = outside_namespace:warning

# Naming conventions
[*.cs]
dotnet_naming_rule.interface_should_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interface_should_be_prefixed_with_i.symbols = interface_types
dotnet_naming_rule.interface_should_be_prefixed_with_i.style = i_prefix
dotnet_naming_symbols.interface_types.applicable_kinds = interface
dotnet_naming_symbols.interface_types.applicable_accessibilities = *
dotnet_naming_symbols.interface_types.required_modifiers =
dotnet_naming_style.i_prefix.capitalization = pascal_case
dotnet_naming_style.i_prefix.required_prefix = I

dotnet_naming_rule.private_internal_fields_underscore.severity = warning
dotnet_naming_rule.private_internal_fields_underscore.symbols = private_internal_fields
dotnet_naming_rule.private_internal_fields_underscore.style = underscore_prefix
dotnet_naming_symbols.private_internal_fields.applicable_kinds = field
dotnet_naming_symbols.private_internal_fields.applicable_accessibilities = private, internal
dotnet_naming_style.underscore_prefix.capitalization = camel_case
dotnet_naming_style.underscore_prefix.required_prefix = _

# MSBuild files
[*.{csproj,props,targets}]
indent_style = space
indent_size = 2

# Markdown
[*.md]
trim_trailing_whitespace = false
```

- [ ] **Step 2: Run dotnet format to verify no existing violations**

```powershell
dotnet format .\Iris.slnx --verify-no-changes
```

Expected: May find some violations (these are pre-existing and not introduced by the .editorconfig). Note them as pre-existing. Do NOT run `dotnet format` without `--verify-no-changes`.

- [ ] **Step 3: Commit**

```bash
git add .editorconfig
git commit -m "feat: add .editorconfig with C# naming and style conventions"
```

---

### Task 7.5: Expand Iris.Architecture.Tests — Dependency direction

**Files:**
- Create: `tests/Iris.Architecture.Tests/DependencyDirectionTests.cs`
- Modify: `tests/Iris.Architecture.Tests/Iris.Architecture.Tests.csproj` — add project references

- [ ] **Step 1: Add project references to Architecture.Tests.csproj**

Update `tests/Iris.Architecture.Tests/Iris.Architecture.Tests.csproj` to reference all src projects for inspection:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Iris.Api\Iris.Api.csproj" />
  <ProjectReference Include="..\..\src\Iris.Application\Iris.Application.csproj" />
  <ProjectReference Include="..\..\src\Iris.Desktop\Iris.Desktop.csproj" />
  <ProjectReference Include="..\..\src\Iris.Domain\Iris.Domain.csproj" />
  <ProjectReference Include="..\..\src\Iris.Infrastructure\Iris.Infrastructure.csproj" />
  <ProjectReference Include="..\..\src\Iris.ModelGateway\Iris.ModelGateway.csproj" />
  <ProjectReference Include="..\..\src\Iris.Perception\Iris.Perception.csproj" />
  <ProjectReference Include="..\..\src\Iris.Persistence\Iris.Persistence.csproj" />
  <ProjectReference Include="..\..\src\Iris.Shared\Iris.Shared.csproj" />
  <ProjectReference Include="..\..\src\Iris.SiRuntimeGateway\Iris.SiRuntimeGateway.csproj" />
  <ProjectReference Include="..\..\src\Iris.Tools\Iris.Tools.csproj" />
  <ProjectReference Include="..\..\src\Iris.Voice\Iris.Voice.csproj" />
  <ProjectReference Include="..\..\src\Iris.Worker\Iris.Worker.csproj" />
</ItemGroup>
```

- [ ] **Step 2: Write the failing test**

Create `tests/Iris.Architecture.Tests/DependencyDirectionTests.cs`:

```csharp
using System.Reflection;

namespace Iris.Architecture.Tests;

public class DependencyDirectionTests
{
    private static readonly Assembly DomainAssembly = typeof(Iris.Domain.Common.Result).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Iris.Application.Abstractions.IUnitOfWork).Assembly;
    private static readonly Assembly SharedAssembly = typeof(Iris.Shared.Results.Result).Assembly;

    [Fact]
    public void Domain_depends_only_on_Shared()
    {
        var referencedAssemblies = DomainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n!.StartsWith("Iris."))
            .ToList();

        // Domain may reference Shared
        Assert.Contains("Iris.Shared", referencedAssemblies);

        // Domain must NOT reference Application or any adapter/host
        var forbidden = referencedAssemblies
            .Where(r => r != "Iris.Shared")
            .ToList();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Application_depends_only_on_Domain_and_Shared()
    {
        var referencedAssemblies = ApplicationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n!.StartsWith("Iris."))
            .ToList();

        var allowed = new[] { "Iris.Domain", "Iris.Shared" };
        var violations = referencedAssemblies
            .Where(r => !allowed.Contains(r))
            .ToList();

        Assert.Empty(violations);
    }
}
```

Note: The exact type references (`Iris.Domain.Common.Result`, `Iris.Application.Abstractions.IUnitOfWork`, `Iris.Shared.Results.Result`) need to match actual types in the codebase. If these types don't exist yet, use the most fundamental type in each assembly. Check before writing:

```powershell
# Find a reliable type in each assembly
Select-String -Path src\Iris.Domain\*\*.cs -Pattern 'public (class|struct|enum|interface|record) \w+' | Select-Object -First 5
Select-String -Path src\Iris.Application\*\*.cs -Pattern 'public (class|struct|enum|interface|record) \w+' | Select-Object -First 5
Select-String -Path src\Iris.Shared\*\*.cs -Pattern 'public (class|struct|enum|interface|record) \w+' | Select-Object -First 5
```

- [ ] **Step 3: Build and run the test (should FAIL if any boundary is violated)**

```powershell
dotnet test .\tests\Iris.Architecture.Tests\Iris.Architecture.Tests.csproj --filter "FullyQualifiedName~DependencyDirectionTests"
```

If the test passes, the architecture is clean. If it fails, report the violations — but this is a safeguard test, so failures mean pre-existing boundary violations that need investigation, not test bugs.

- [ ] **Step 4: Commit**

```bash
git add tests/Iris.Architecture.Tests/Iris.Architecture.Tests.csproj tests/Iris.Architecture.Tests/DependencyDirectionTests.cs
git commit -m "test: add architecture dependency direction tests (Domain→Shared, Application→Domain+Shared)"
```

---

### Task 7.6: Expand Iris.Architecture.Tests — Host and adapter boundaries

**Files:**
- Create: `tests/Iris.Architecture.Tests/ProjectReferenceTests.cs`

- [ ] **Step 1: Write ProjectReferenceTests.cs**

```csharp
using System.Reflection;

namespace Iris.Architecture.Tests;

public class ProjectReferenceTests
{
    private static readonly Assembly DesktopAssembly = typeof(Iris.Desktop.Program).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Iris.Api.Program).Assembly;
    private static readonly Assembly WorkerAssembly = typeof(Iris.Worker.Program).Assembly;

    [Fact]
    public void Desktop_does_not_reference_Api_or_Worker()
    {
        var references = DesktopAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n!.StartsWith("Iris."))
            .ToList();

        Assert.DoesNotContain("Iris.Api", references);
        Assert.DoesNotContain("Iris.Worker", references);
    }

    [Fact]
    public void Api_does_not_reference_Desktop_or_Worker()
    {
        var references = ApiAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n!.StartsWith("Iris."))
            .ToList();

        Assert.DoesNotContain("Iris.Desktop", references);
        Assert.DoesNotContain("Iris.Worker", references);
    }

    [Fact]
    public void Worker_does_not_reference_Desktop_or_Api()
    {
        var references = WorkerAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n!.StartsWith("Iris."))
            .ToList();

        Assert.DoesNotContain("Iris.Desktop", references);
        Assert.DoesNotContain("Iris.Api", references);
    }
}
```

Note: `typeof(Iris.Desktop.Program)` may not exist if the Desktop project doesn't have a `Program` class. Find the entry point type:

```powershell
Select-String -Path src\Iris.Desktop\Program.cs -Pattern 'class Program' 2>$null
Select-String -Path src\Iris.Desktop\App.axaml.cs -Pattern 'class App' 2>$null
```

Use `typeof(Iris.Desktop.App)` or whichever bootstrap class exists. Same for `Iris.Api` and `Iris.Worker`.

- [ ] **Step 2: Build and run**

```powershell
dotnet test .\tests\Iris.Architecture.Tests\Iris.Architecture.Tests.csproj
```

- [ ] **Step 3: Commit**

```bash
git add tests/Iris.Architecture.Tests/ProjectReferenceTests.cs
git commit -m "test: add host-to-host reference prohibition tests"
```

---

### Task 7.7: Expand Iris.Architecture.Tests — Forbidden namespaces

**Files:**
- Create: `tests/Iris.Architecture.Tests/ForbiddenNamespaceTests.cs`

- [ ] **Step 1: Write ForbiddenNamespaceTests.cs**

```csharp
using System.Reflection;

namespace Iris.Architecture.Tests;

public class ForbiddenNamespaceTests
{
    private static readonly Assembly DomainAssembly = typeof(Iris.Domain.Common.Result).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Iris.Application.Abstractions.IUnitOfWork).Assembly;

    [Fact]
    public void Domain_does_not_reference_EntityFrameworkCore()
    {
        var efCoreTypes = DomainAssembly.GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("Microsoft.EntityFrameworkCore"))
            .ToList();

        Assert.Empty(efCoreTypes);
    }

    [Fact]
    public void Application_does_not_reference_Persistence()
    {
        var persistenceRefs = ApplicationAssembly.GetReferencedAssemblies()
            .Where(a => a.Name == "Iris.Persistence")
            .ToList();

        Assert.Empty(persistenceRefs);
    }

    [Fact]
    public void Application_does_not_reference_ModelGateway()
    {
        var gatewayRefs = ApplicationAssembly.GetReferencedAssemblies()
            .Where(a => a.Name == "Iris.ModelGateway")
            .ToList();

        Assert.Empty(gatewayRefs);
    }
}
```

- [ ] **Step 2: Build and run**

```powershell
dotnet test .\tests\Iris.Architecture.Tests\Iris.Architecture.Tests.csproj
```

- [ ] **Step 3: Commit**

```bash
git add tests/Iris.Architecture.Tests/ForbiddenNamespaceTests.cs
git commit -m "test: add forbidden namespace tests (EF Core in Domain, adapters in Application)"
```

---

### Task 7.8: Clean up Architecture.Tests — remove stub, run full suite

**Files:**
- Delete: `tests/Iris.Architecture.Tests/UnitTest1.cs`

- [ ] **Step 1: Delete the stub**

```bash
git rm tests/Iris.Architecture.Tests/UnitTest1.cs
```

- [ ] **Step 2: Run the full architecture test suite**

```powershell
dotnet test .\tests\Iris.Architecture.Tests\Iris.Architecture.Tests.csproj
```

Expected: All architecture tests pass (DependencyDirectionTests, ProjectReferenceTests, ForbiddenNamespaceTests).

- [ ] **Step 3: Run full solution test suite**

```powershell
dotnet test .\Iris.slnx
```

Expected: All tests pass. Architecture tests now contribute real assertions instead of a stub.

- [ ] **Step 4: Commit**

```bash
git commit -m "test: remove Architecture.Tests stub, finalize architecture test suite"
```

---

### Task 7.9: Create docs/architecture.md

**Files:**
- Create: `docs/architecture.md`

- [ ] **Step 1: Write docs/architecture.md**

Create `docs/architecture.md` by synthesizing key content from `.agent/architecture.md` into a public-facing document. Keep it shorter than the agent memory version — focus on what external contributors and future maintainers need to know.

```markdown
# Iris — Architecture

## Overview

Iris (Айрис) is a local personal AI companion built on .NET 10 with a Clean / Hexagonal modular architecture.

**Core principle:** Domain + Application define the system. Adapters implement external technology. Hosts compose and run the system.

## Solution Layout

```
Iris/
├── src/             # 13 .NET projects
│   ├── Iris.Shared/           # Neutral reusable primitives
│   ├── Iris.Domain/           # Pure domain model
│   ├── Iris.Application/      # Use cases, orchestration, ports
│   ├── Iris.Persistence/      # EF Core / SQLite adapter
│   ├── Iris.ModelGateway/     # LLM provider adapter (Ollama, LM Studio)
│   ├── Iris.Perception/       # Desktop capture adapter
│   ├── Iris.Tools/            # Tool execution adapter
│   ├── Iris.Voice/            # Audio/STT/TTS adapter
│   ├── Iris.Infrastructure/   # Shared technical plumbing
│   ├── Iris.SiRuntimeGateway/ # Python SI runtime bridge
│   ├── Iris.Desktop/          # Avalonia desktop host
│   ├── Iris.Api/              # HTTP API host
│   └── Iris.Worker/           # Background worker host
├── tests/           # 5 test projects
│   ├── Iris.Domain.Tests/
│   ├── Iris.Application.Tests/
│   ├── Iris.Infrastructure.Tests/
│   ├── Iris.Integration.Tests/
│   └── Iris.Architecture.Tests/
├── python/          # Python SI runtime sidecar
├── docs/            # Public documentation
└── .agent/          # Agent working memory (INTERNAL ONLY)
```

## Dependency Direction

```
Shared ← Domain ← Application ← Adapters ← Hosts
```

### Core (Domain + Application)

| Project | May depend on | Must NOT depend on |
|---|---|---|
| `Iris.Shared` | Nothing product-specific | Iris product concepts |
| `Iris.Domain` | `Iris.Shared` | EF Core, HTTP, UI, infrastructure |
| `Iris.Application` | `Iris.Domain`, `Iris.Shared` | Concrete adapters, hosts |

### Adapters

All adapters implement Application abstractions. They may depend inward on Application / Domain / Shared.

Adapters must NOT depend on each other unless explicitly approved.

### Hosts

Hosts (Desktop, API, Worker) compose Application + adapters. They must NOT depend on each other.

## Forbidden Shortcuts

- ViewModel → Ollama / DbContext (must go through Application)
- API endpoint → DbContext (must go through Application)
- Application → Persistence / ModelGateway (use abstractions)
- Domain → EF Core / HTTP
- Tools → permission decisions (Application decides)
- Voice → chat orchestration (Application orchestrates)
- Perception → memory extraction (Application owns)
- Shared → product-specific behavior

## First Vertical Slice

```
ChatView → ChatViewModel → IrisApplicationFacade
  → SendMessageHandler → PromptBuilder
  → Ollama (via ModelGateway)
  → SQLite (via Persistence)
  → response back to UI
```

## Testing Strategy

| Test project | Scope |
|---|---|
| `Iris.Domain.Tests` | Pure domain behavior, no infrastructure |
| `Iris.Application.Tests` | Use cases with fakes/stubs |
| `Iris.Infrastructure.Tests` | Serialization, event bus, background tasks |
| `Iris.Integration.Tests` | Composed behavior (Persistence+SQLite, ModelGateway stubs) |
| `Iris.Architecture.Tests` | Dependency direction, forbidden references, namespace guards |

## Build & Test

```powershell
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx
dotnet format .\Iris.slnx --verify-no-changes
```

## Decision Policy

When unsure where code belongs:

1. Domain concept or invariant → `Iris.Domain`
2. Use case or policy → `Iris.Application`
3. Database / EF / SQLite → `Iris.Persistence`
4. Model provider HTTP logic → `Iris.ModelGateway`
5. Desktop capture / WinAPI → `Iris.Perception`
6. Tool execution / sandbox → `Iris.Tools`
7. Audio / STT / TTS → `Iris.Voice`
8. Shared technical plumbing → `Iris.Infrastructure`
9. Python SI runtime calls → `Iris.SiRuntimeGateway`
10. UI → `Iris.Desktop`
11. HTTP transport → `Iris.Api`
12. Background host → `Iris.Worker`

If code seems to belong everywhere, it probably belongs nowhere yet. Stop and design the boundary first.
```

- [ ] **Step 2: Commit**

```bash
git add docs/architecture.md
git commit -m "docs: add public-facing architecture documentation"
```

---

### Task 7.10: Create .github/workflows/ci.yml

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Write ci.yml**

```yaml
name: CI

on:
  push:
    branches: [main, master]
  pull_request:
    branches: [main, master]

jobs:
  build-and-test:
    runs-on: windows-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore Iris.slnx

      - name: Build
        run: dotnet build Iris.slnx --no-restore --configuration Release

      - name: Test
        run: dotnet test Iris.slnx --no-build --configuration Release --verbosity normal

      - name: Format Check
        run: dotnet format Iris.slnx --verify-no-changes --no-restore
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add GitHub Actions CI workflow (build, test, format verify)"
```

---

### Task 7.11: Final verification — full solution build and test

- [ ] **Step 1: Build**

```powershell
dotnet build .\Iris.slnx
```

Expected: 0 errors, 0 warnings. All projects inherit from Directory.Build.props.

- [ ] **Step 2: Run all tests**

```powershell
dotnet test .\Iris.slnx
```

Expected: All tests pass, including new architecture tests.

- [ ] **Step 3: Format check**

```powershell
dotnet format .\Iris.slnx --verify-no-changes
```

Expected: Passes or reports only pre-existing violations (not introduced by Phase 7).

- [ ] **Step 4: Verify architecture tests protect boundaries**

Make a deliberate boundary violation to prove the tests catch it:

```powershell
# Temporarily add a forbidden reference (DO NOT COMMIT)
# Example: add EF Core to Domain.csproj
# Then run architecture tests — they should FAIL
# Then REVERT the temporary change
```

If tests catch the violation, the safeguards are working. Revert immediately.

- [ ] **Step 5: Inspect final diff**

```powershell
git diff --stat HEAD~10..HEAD
git status --short
```

Expected: New files (Directory.Build.props ×2, Directory.Packages.props, .editorconfig, docs/architecture.md, .github/workflows/ci.yml, 3 test files), modified .csproj files (13 src + 5 test), deleted UnitTest1.cs. No product code changes in `src/` beyond .csproj trimming.

- [ ] **Step 6: Commit final verification**

```bash
git commit --allow-empty -m "verify: Phase 7 regression safeguards complete, all tests pass"
```

---

## Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| `Directory.Packages.props` version mismatch causes build failure | Build broken | Step 7.3.4 verifies restore+build before commit. Versions are pinned to current working values |
| Architecture tests reference types that don't exist | Test won't compile | Step 7.5.2 instructs to find actual types first via `Select-String` |
| `dotnet format --verify-no-changes` fails on pre-existing issues | False alarm | Document pre-existing violations in commit message, do not fix in this phase |
| Architecture tests pass only because project has no code yet | False confidence | Tests check assembly-level references and namespaces, which are present even with stub code |
| CI workflow fails because `.slnx` is unsupported by older SDK | CI broken | Use `setup-dotnet@v4` with `10.0.x` — `.slnx` is supported in .NET 10 SDK |

---

## Implementation Handoff Notes

**Order matters:** Tasks 7.1-7.3 (MSBuild centralization) must complete before Task 7.5 (Architecture.Tests) because the .csproj cleanup affects all projects including test projects. Tasks 7.5-7.8 (architecture tests) form a logical group. Tasks 7.4, 7.9, 7.10 are independent and can be done in any order after 7.3.

**Critical constraints:**
- Do not change product code in `src/` beyond .csproj streamlining.
- Do not change package versions — only centralize existing versions in `Directory.Packages.props`. Architecture.Tests packages are upgraded to match other test projects (6.0.4→10.0.0, 17.14.1→18.4.0, 3.1.4→3.1.5).
- Do not fix pre-existing formatting violations found by `dotnet format --verify-no-changes`.
- Architecture tests use only `System.Reflection` (no external NuGet packages) to avoid dependency on NetArchTest.Rules.

**Expected final state:**
- `Directory.Build.props` (root) centralizes `<TargetFramework>`, `<ImplicitUsings>`, `<Nullable>`, `<AnalysisLevel>`.
- `tests/Directory.Build.props` adds `<IsPackable>false`, `<IsTestProject>true`.
- `Directory.Packages.props` centralizes all package versions.
- All `.csproj` files are minimal (no duplicate properties, no explicit versions).
- `.editorconfig` enforces C# style.
- `Iris.Architecture.Tests` has 3 test files with real assertions (dependency direction, host references, forbidden namespaces).
- `docs/architecture.md` is public-facing.
- `.github/workflows/ci.yml` runs build + test + format on push/PR.
- `dotnet build .\Iris.slnx` passes.
- `dotnet test .\Iris.slnx` passes (all existing tests + new architecture tests).

**Verification commands:**
```powershell
dotnet build .\Iris.slnx
dotnet test .\Iris.slnx
dotnet format .\Iris.slnx --verify-no-changes
```

**Manual gaps:** None. All verification is automated.

---

## Open Questions

No blocking open questions.
