# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this fork is

A fork of dotnet/Silk.NET, pruned to the subsystems one game engine needs and
retargeted to **.NET 10 only**, so it can rely on modern trimming and
NativeAOT. Upstream 2.x is in maintenance mode and still targets
netstandard2.0/netcoreapp3.1/net5.0, which blocks both.

Kept: Core, Maths, Input, Windowing, OpenAL, OpenGL (core profile only),
Vulkan, and the OpenGL ImGui extension — 86 projects. Removed: OpenXR,
OpenCL, WebGPU, Assimp, SPIRV/shaderc, the DirectX family, OpenGL
Legacy/ES/WGL, Lab experiments, Templates, and 13 of the 15 native packages.

Do not re-add upstream subsystems or TFMs without being asked.

## Target framework

The TFM lives in **one** place: `SilkTargetFramework` in the root
`Directory.Build.props`. Every csproj reads `$(SilkTargetFramework)`, and
retargeting the repo is a one-line change there.

The per-group `src/*/Directory.Build.props` files do **not** chain to the
parent by MSBuild default, so each one explicitly imports it via
`GetPathOfFileAbove`. A new group directory needs that import or its projects
will have no TargetFramework.

Two deliberate exceptions, commented in place:

- `src/Core/Silk.NET.SilkTouch` stays `netstandard2.0` — Roslyn loads source
  generators into the compiler, so it cannot be net10.0.
- `build/nuke/Silk.NET.NUKE.csproj` stays `net8.0` — Nuke.Common 6.3.0 uses
  `BinaryFormatter`, which .NET 9 **removed outright**, so the build host
  cannot run on net10.0. (`EnableUnsafeBinaryFormatterSerialization` does not
  help; the API is gone, not just disabled.) Upgrading Nuke.Common would lift
  this.

Neither is shipped, so neither affects trimming or AOT.

Conditions that need to apply only to the shipped libraries are written as
`Condition="'$(TargetFramework)' == '$(SilkTargetFramework)'"` rather than
matching TFM name prefixes — upstream gated the trim/AOT properties on
`net6`/`net7`/`net8` and they silently stopped applying on retarget.

## Prerequisites

- .NET 10 SDK (`global.json` pins 10.0.100, `rollForward: major`)
- .NET 8 **runtime** — needed to run the NUKE build host

No Android/iOS workloads, no JDK, no Android SDK. Submodules are only needed
for the native packages and bindings regeneration.

## Build system

All builds go through NUKE. Use the bootstrap scripts, not a global `nuke` tool:

- Build: `./build.sh` (default target is `Compile`)
- Test: `./build.sh Test --skip Clean Restore Compile` (CI's invocation; runs `dotnet test` per `*.Tests` project)
- Pack: `./build.sh Pack` → `build/output_packages`
- List targets: `./build.sh --plan`

Notes:
- MSBuild output is filtered to `/clp:errorsonly` unless you pass `--warnings`, so **warnings are invisible by default**.
- `Prerequisites` runs before every target.
- `Directory.Build.targets` at the root is intentionally empty. Shared settings come from explicit imports at the bottom of each csproj: `build/props/common.props` (tools) or `build/props/bindings.props` (generated binding projects).

### Subset solutions

`./build.sh Sln --projects opengl silk.net.vulkan` writes the gitignored
`Silk.NET.gen.sln`. **While that file exists, every subsequent NUKE run uses
it instead of `Silk.NET.sln`** unless you pass `--all`. Remove it with
`./build.sh Clean --sln`. Less essential than upstream now that the solution
is 86 projects rather than 269, but the override gotcha is unchanged.

### Solution membership

Every `*.csproj` outside `build/submodules` must be in `Silk.NET.sln`.
`./build.sh ValidateSolution` checks this and prints the
`dotnet sln Silk.NET.sln add <path>` commands to fix it. Genuine exceptions go
in `AllowedExclusions` in `build/nuke/Build.ReviewHelpers.cs`.

## Generated bindings — never hand-edit

`*.gen.cs` files are committed. To change them, edit `generator.json` or
`src/Core/Silk.NET.BuildTools`, then run `./build.sh RegenerateBindings`.
Keep `.gen.cs` churn in a separate commit from behavioral changes.

`generator.json` holds 6 binder tasks: OpenGL, Vulkan, VulkanVideo, SDL, Core,
Win32Extras. **Do not re-add the pruned tasks** — regenerating would recreate
the deleted projects.

`src/Core/Silk.NET.BuildTools/Bind/ProjectWriter.cs` emits the csproj for each
generated project, including its `<TargetFramework>`; it must keep emitting
`$(SilkTargetFramework)` or regeneration will revert the retarget.

Only `build/submodules/SDL` is needed to regenerate bindings. The submodule
list in `.github/workflows/bindings-regeneration.yml` must stay in sync with
`generator.json`.

## Trim / NativeAOT

This is the point of the fork. `common.props` sets `IsTrimmable`,
`IsAotCompatible`, `TrimMode=full` and the trim/AOT/single-file analyzers for
every shipped project; `bindings.props` generates `ILLink.Substitutions.xml`
and the per-package P/Invoke-override `.targets`.

Known remaining blockers — reflection-based platform discovery that will fail
under NativeAOT:

- `src/Windowing/Silk.NET.Windowing.Common/Window.cs` — `Assembly.Load` plus `Activator.CreateInstance` to find `IWindowPlatform` implementations
- `src/Input/Silk.NET.Input.Common/InputWindowExtensions.cs` — same pattern for `IInputPlatform`
- `src/OpenAL/Silk.NET.OpenAL` — `Activator.CreateInstance` in the extension loaders (`AL.cs`, `ALContext.cs`, `Extensions/ALExtensionLoader.cs`)

These need replacing with explicit registration or source generation.

## Public API analyzer — disabled

`RS0016`/`RS0017` are set to `none` and `SilkPublicApiExempt` is true globally
in `common.props`; the per-TFM `PublicAPI/*.txt` files are deleted. This fork
expects to break API freely, so the gate was only friction. The NUKE targets
`DeclareApi`, `ShipApi` and `EnsureApiDeclared` still exist but are now no-ops.
Do not re-enable without being asked.

## Code style (deviations from .NET defaults)

From `.editorconfig` — these differ from what you would otherwise assume:

- **Do not trim trailing whitespace** (`trim_trailing_whitespace = false`).
- Space **after** a cast: `(int) x`.
- Spaces on **both** sides of the inheritance colon: `class Foo : Bar`.
- Braces are **required** even for single-statement `if`/`for`/`foreach`/`while`.
- When a parameter or argument list wraps, `(` and `)` go on their own lines (this is why `build/nuke/*.cs` looks like `Target X => CommonTarget\n(\n    ...\n);`).
- `this.` qualification is a **warning**, not a suggestion.
- Leave XML doc indentation alone (`DoNotTouch`).
- Allman braces, 4-space indent, LF endings.

Every new `.cs` file needs this header (`IDE0073` is a warning):

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
```

`LangVersion=preview` and `AllowUnsafeBlocks=true` are near-universal.

## Gotchas

- **Do not clone submodules recursively.** Only `SDL` and `glfw` are still used (the two surviving native packages, plus SDL for bindings).
- Editing the root `README.md` changes shipped NuGet package descriptions — `common.props` generates the package README by substituting marker comments in it. The README still describes upstream's full API surface.
- The two `src/Native/*` packages still ship legacy `Ultz.Native.*` package IDs despite `Silk.NET.*` folder names.
- `VersionPrefix` is still upstream's `2.23.0` in `build/props/common.props`, and `common.props`/`RepositoryUrl` still point at dotnet/Silk.NET.
- `build/nuke/Native/*.cs` and several NUKE targets (`Angle`, `Assimp`, `Dxvk`, `MoltenVK`, `OpenALSoft`, `Shaderc`, `SPIRVCross`, `SPIRVReflect`, `SwiftShader`, `Vkd3d`, `VulkanLoader`, `Wgpu`) still exist for packages removed in the prune. They compile but will fail if invoked.
- `.vscode/launch.json` is stale (references `netcoreapp3.0` tutorial paths that no longer exist).
