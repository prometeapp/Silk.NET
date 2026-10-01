---
name: silk-sln
description: Generate and work inside a subset solution (Silk.NET.gen.sln) so builds don't load the full 348 KB Silk.NET.sln. Use when starting focused work on a few projects, or when a build is unexpectedly slow or picking up the wrong solution. Accepts project name fragments as arguments.
disable-model-invocation: true
---

Scope the build to just the projects being worked on. `$ARGUMENTS` is a space-separated list of project name fragments (quote fragments containing spaces).

## 1. Check for an existing subset solution

```
ls -la Silk.NET.gen.sln 2>/dev/null
```

`Silk.NET.gen.sln` is gitignored, and **while it exists every NUKE target uses it instead of `Silk.NET.sln`** unless `--all` is passed (`build/nuke/Build.SolutionGenerator.cs`). If one already exists and covers different projects, say so and confirm before regenerating — the user may be mid-task on it.

## 2. Generate

```
./build.sh Sln --projects $ARGUMENTS
```

If `$ARGUMENTS` is empty, ask which projects to include. Infer fragments from the task when possible, e.g.:
- OpenGL work → `opengl "opengl tutorials"`
- Vulkan work → `silk.net.vulkan`
- Build/NUKE work → `build`
- Always include `build` when touching anything under `build/`.

Fragments are matched against project names, so `core.win32extras` and `silk.net.vulkan` are both valid forms.

## 3. Build and test against the subset

```
./build.sh Compile
./build.sh Test --skip Clean Restore Compile
```

These now operate on the subset. Note MSBuild output is filtered to errors only — add `--warnings` when diagnosing a warning.

## 4. Clean up when done

```
./build.sh Clean --sln
```

Always do this (or tell the user to) before running a full-repo build, `Pack`, `ValidateSolution`, or `EnsureApiDeclared` — those need the real solution. Alternatively pass `--all` for a one-off full-solution run without deleting the subset.
