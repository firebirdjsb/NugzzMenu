# Police spawning revert and clothing picker repair

This follow-up supersedes the Special Customers test ZIP from earlier on
2026-10-11. Target: Schedule I 0.4.7f12. The menu still shows version 1.0.0;
its startup marker is now `Test build: Police revert / clothing picker fix
2026-10-11`.

## Changes

- **Police spawning restored:** `Services/VehicleService.cs` matches the
  previous repository version exactly. Manager-prefab selection and the
  police-station deployment path are back. Consequently, the previous
  Shitbox/SUV mismatch may also return; this is the requested rollback rather
  than another attempt at custom police spawning.
- **Color picker fixed:** `UI/ClothingColorPicker.cs` uses the existing
  `GUIFit.Texture` helper, which renders with a cached GUIStyle and `GUI.Box`.
  Swatches draw only on Repaint; input controls continue processing other GUI
  events. `finally` restores the prior global GUI tint even if drawing fails.
- **Log flooding bounded:** `UI/ItemsTabRenderer.cs` reports an unexpected tab
  failure at most once per five seconds. This is secondary protection; the
  broken draw call itself was replaced. Native Unity logs are not suppressed.
- `Core.cs` identifies the new test build. Native/No color defaults, explicit
  tint selection, config, new visitors/group arrivals, phone cleanup, Never
  Wanted, weather, and the avatar compatibility guards remain included.

## Diagnosis

The supplied log includes the actual picker failure:

```text
[Nugzz] ItemsTab error: System.NotSupportedException: Method unstripping failed
   at NugzzMenu.UI.ClothingColorPicker.Draw(...)
```

Read-only inspection of the **installed generated IL2CPP interop**, matched by
hash to the build reference, shows that `GUI.DrawTexture(Rect, Texture)` calls
other managed overloads and eventually reaches a generated method whose body
throws `NotSupportedException("Method unstripping failed")`. The previous
picker called this chain for each swatch. The tab's catch logged the failure
on each redraw, causing the repeated error output. Type/member existence
checks could not detect this throwing method body.

The pasted NavMesh agent messages are a separate class of native AI warning;
the color fix does not silence or claim to repair them. No additional vehicle,
NPC schedule, game-save, or game-wide logging changes were made.

## Verification

- Clean Release build: zero warnings/errors.
- Compiled API audit: 234 types, 695 members, 70 attribute Harmony targets,
  12 dynamic targets, current update markers and public S1API contracts pass.
- Six new picker checks pass: the old compiled picker reaches the real
  throwing interop method; the replacement/shared helper avoid it; 120
  expanded redraws complete all 37 controls and 27 swatches; draw failure
  restores tint; input events select a color without drawing; Native reset
  does not spawn clothing.
- `git diff --exit-code HEAD -- Services/VehicleService.cs` confirms the
  complete police service revert. Whitespace checks pass.
- ZIP CRC and every manifest file hash are checked during packaging.

Fixtures and read-only method inspection do not execute native Unity rendering,
police AI, Harmony, or network replication. **Actual in-game QA is pending.**
Open Items, expand the picker, select several colors, choose Native again,
switch tabs, and close/reopen the menu. Confirm normal tint and absence of the
repeated picker exception. Check restored police spawning on a test save.

## Install and rollback

Close the game and back up `Mods/NugzzMenu.dll` outside Mods. Replace it with
the new menu DLL. Preserve your `nuggzmenu-config.json`.

The inspected startup log still loads **S1API 3.2.1-beta.9**. The ZIP includes
the stable 3.2.1 mod/loader pair from the earlier package: back up and replace
both corresponding Mods/Plugins DLLs if still using the beta. Do not leave
duplicate renamed DLLs in those folders. The lock file verifies the official
dependency archive; no extra runtime dependencies were introduced.

Rollback closes the game and restores the DLLs you replaced. No config format
change or save migration is introduced. The example config is reference-only
and is not copied into Mods by the package. The agent did not install the
artifact, edit game saves, or touch production.

Changed files: `Services/VehicleService.cs`, `UI/ClothingColorPicker.cs`,
`UI/ItemsTabRenderer.cs`, `Core.cs`, `tests/ClothingPickerChecks`, README and
follow-up documentation. Earlier compatibility changes remain in the build;
their content coverage and runtime limitations are documented in
`SPECIAL_CUSTOMERS_FIXES_20261011.md`.

```powershell
dotnet run --project tests/ClothingPickerChecks -c Release -- <new-menu.dll> net6.0 <before-fix-menu.dll>
```

The baseline DLL is an audit input retained locally; it is not shipped as an
extra mod. The package includes small baseline/API/interop evidence reports.
