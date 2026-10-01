# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build system

All builds go through NUKE. Use the bootstrap scripts, not a global `nuke` tool:

- Build: `./build.sh` (default target is `Compile`)
- Test: `./build.sh Test --skip Clean Restore Compile` (CI's exact invocation; runs `dotnet test` per `*.Tests` project)
- Pack: `./build.sh Pack` → `build/output_packages`
- List targets: `./build.sh --plan`

Notes:
- MSBuild output is filtered to `/clp:errorsonly` unless you pass `--warnings`, so **warnings are invisible by default**.
- `Prerequisites` runs before every target; it may install the `android` workload when `--native` is passed.
- `Directory.Build.props` / `.targets` at the root are intentionally empty. Shared settings come from explicit imports at the bottom of each csproj: `build/props/common.props` (tools) or `build/props/bindings.props` (generated binding projects).
- `global.json` pins SDK 8.0.100 with `rollForward: major`. Full native/Android builds additionally need .NET 6 + 7 SDKs, JDK 11+, and the Android SDK/NDK (see README).

## Subset solutions (important)

`Silk.NET.sln` is ~348 KB and slow to load. Generate a subset instead:

```
./build.sh Sln --projects opengl silk.net.vulkan core.win32extras
```

This writes the gitignored `Silk.NET.gen.sln`. **While that file exists, every subsequent NUKE run uses it instead of `Silk.NET.sln`** unless you pass `--all`. Remove it with `./build.sh Clean --sln` before running a full build.

## Generated bindings — never hand-edit

There are ~10,800 committed `*.gen.cs` files under `src/`. To change them, edit `generator.json` (or `src/Core/Silk.NET.BuildTools`) and run `./build.sh RegenerateBindings`. Keep `.gen.cs` churn in a separate PR from behavioral changes.

`.github/workflows/bindings-regeneration.yml` keeps a submodule list that must stay in sync with `generator.json` — update both together.

## Public API gate

`RS0016` (undeclared public API) and `RS0017` (removed public API) are **errors**. After adding or changing public API:

```
./build.sh DeclareApi   # populates PublicAPI.Unshipped.txt
```

CI runs `EnsureApiDeclared` (the same `dotnet format analyzers` pass with `--verify-no-changes`) and fails if those files are stale. `./build.sh ShipApi` promotes Unshipped → Shipped; only do that at release time.

## Solution membership

Every `*.csproj` outside `build/submodules` must be in `Silk.NET.sln`. `./build.sh ValidateSolution` checks this and prints the `dotnet sln Silk.NET.sln add <path>` commands to fix it. Genuine exceptions go in `AllowedExclusions` in `build/nuke/Build.ReviewHelpers.cs`.

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

## Repo etiquette

- `main` is the 2.x maintenance branch (`VersionPrefix 2.23.0` in `build/props/common.props`); 3.0 work lives on `develop/3.0`.
- API signature and behavioral compatibility must be preserved on `main` — breaking changes are rejected.
- Style-only PRs are not accepted.
- `origin` points at `github.com/dotnet/Silk.NET` with no fork remote; CONTRIBUTING.md assumes a fork workflow, so do not push branches to `origin` without checking.
- See @CONTRIBUTING.md for the full policy.

## Gotchas

- **Do not clone submodules recursively.** They are unnecessary for a normal build; only native and bindings-regeneration targets need them (`git submodule update --init --depth 0`).
- Editing the root `README.md` changes shipped NuGet package descriptions — `common.props` generates the package README by substituting marker comments in it.
- `src/Native/*` packages still ship legacy `Ultz.Native.*` package IDs despite `Silk.NET.*` folder names.
- Build-related env vars: `PUSHABLE_GITHUB_TOKEN` (bindings/API PR creation), `GITHUB_TOKEN`, `ANDROID_HOME` / `AndroidSdkDirectory`.
- `.vscode/launch.json` is stale (references `netcoreapp3.0` tutorial paths that no longer exist).
