---
name: silk-api
description: Handle the public API analyzer gate after adding, changing, or removing public API — run DeclareApi, review the PublicAPI.Unshipped.txt diff, and decide whether ShipApi is appropriate. Use when RS0016/RS0017 errors appear, when CI's EnsureApiDeclared fails, or proactively after touching public surface area.
---

`RS0016` (undeclared public API) and `RS0017` (removed public API) are configured as **errors** in `.editorconfig`, and CI runs `EnsureApiDeclared`. Any new public member fails the build until it is declared.

## 1. Declare the new API

```
./build.sh DeclareApi
```

This runs `dotnet format analyzers Silk.NET.sln --diagnostics=RS0016 --severity=error -v=diag --include-generated` (see `build/nuke/Build.PublicApi.cs`) and appends new entries to the relevant `PublicAPI/<tfm>/PublicAPI.Unshipped.txt` files. `common.props` auto-creates those files if missing.

If a subset `Silk.NET.gen.sln` exists it will be used instead of the full solution, which under-declares the API. Run `./build.sh Clean --sln` first, or pass `--all`.

## 2. Review the diff — this is the important step

```
git diff --stat -- '**/PublicAPI.Unshipped.txt'
git diff -- '**/PublicAPI.Unshipped.txt'
```

Check for:
- **Unintended additions.** Something accidentally `public` that should be `internal`? Fix the modifier and re-run rather than declaring it.
- **Removals** (lines disappearing from `Shipped.txt`, or `RS0017`). `main` is the 2.x maintenance branch and breaking changes are rejected there — removing shipped public API is almost always wrong. Report it to the user instead of papering over it.
- **Huge diffs in `.gen.cs`-backed projects.** That means the generator output changed; per CONTRIBUTING.md those belong in a separate PR.

## 3. Do NOT run ShipApi casually

`./build.sh ShipApi` moves Unshipped → Shipped, which freezes the API as a compatibility promise. That is a release-time action. Never run it as part of ordinary feature work — leave entries in `Unshipped.txt` and say so.

## 4. Verify the gate passes

```
./build.sh EnsureApiDeclared
```

Same analyzer pass with `--verify-no-changes`; this is what CI enforces.
