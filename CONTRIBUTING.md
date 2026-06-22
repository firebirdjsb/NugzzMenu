# Contributing To NugzzMenu

Thanks for helping keep the menu usable. This project moves fast, but it should
still feel understandable to the next person who opens it.

## Start Here

Read these before making a non-trivial change:

- `docs/ARCHITECTURE.md` for the project shape and safety rules.
- `docs/CODEBASE_MAP.md` for where each feature lives.
- `docs/FEATURE_PLAYBOOK.md` for the checklist to follow when adding features.

## Local Build

```powershell
dotnet build SeshMenu.csproj -c Release
```

Output:

```text
bin/Release/net6.0/NugzzMenu.dll
```

## Coding Style

- Keep UI drawing in `UI/*TabRenderer.cs`.
- Keep gameplay logic in `Services/*Service.cs`.
- Keep Harmony patches short and delegate to services.
- Prefer vanilla Schedule I systems before custom replicas.
- Avoid new dependencies unless there is a clear reason.
- Do not add noisy per-frame logs.
- Use clear status messages for host-only, main-menu-only, or unsupported
  actions.

## Pull Request Checklist

- Build passes in Release.
- The changed tab still fits in the menu.
- Logs stay quiet during normal play.
- Host/client behavior is tested if the feature touches multiplayer.
- Dangerous save/world actions are guarded and recoverable.
- README or docs are updated when behavior changes.
