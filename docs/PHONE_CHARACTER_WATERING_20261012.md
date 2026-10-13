# Phone / Character and watering follow-up — 2026-10-12

Status: separate test build for Schedule I 0.4.7f12, with stable S1API 3.2.1
bundled. The game installation and saves were not changed by the agent.

Startup marker: `Test build: Phone character / watering fix 2026-10-12`.
The menu's version label remains 1.0.0.

## What caused the Character failure

The supplied 7.47-second recording shows Character/clothing controls briefly
appearing, then disappearing while the parent Phone/Character tabs and native
blur remain visible. The prior patch ran character cleanup from
`Phone.SetIsOpen(false)`. That method also runs when the phone model closes
during a normal transition to Character. The cleanup removed the selected
Character state and hid its clothing panel, even though the parent
`GameplayMenu` was still open.

Read-only inspection of the supplied metadata and installed native binary
confirms that `GameplayMenu.SetScreen` switches its native child states, and
`GameplayMenu.OnOpen`/`OnClose` own the enclosing screen's visibility and
camera UI registration. The mod now leaves this native stack intact.

The clothing panel remains available. NugzzMenu may open over Phone or
Character; world actions remain gated while either native screen owns input.
Closing NugzzMenu over a still-open native screen retains its cursor and camera
ownership. Native parent close events invalidate the cached gate immediately;
an animating child's old visibility flag cannot block F8 after exiting Character.
Native child states are retained for reopening the selected tab.

The game now polls phone/map/journal/messages InputAction references directly
in `GameplayMenu.Update`, bypassing the older GameInput callback guards.
The new guard blocks that input-only Update while NugzzMenu captures input,
plus direct parent opening, tab switching, and phone model opening. Native
closing and animation/state restoration remain available.

## Blur / dark tint

The demonstrated native blur is part of the broken Phone/Character transition.
Removing the incorrect child-state cleanup allows the native enclosing screen
to restore its presentation on exit. Existing GUI-state restoration and the
clothing-picker tint `finally` guard are retained. The mod does not force-clear
global post-processing, which could erase another native screen's effects.

This addresses the demonstrated transition path. An unrelated intermittent
dark tint, or its absence after this patch, has not been reproduced in-game.

## Planting and watering

The old quick planting path passed `SeedDefinition.name`. Native
`SowSeedTask.Success` passes the canonical registry ID and finishes by setting
the soil to `Packed`. The quick path now follows that contract.

Quick watering and misting previously swallowed moisture-sync exceptions and
still displayed success. Native `SyncMoistureData` also logs and returns without
sending when its network client is uninitialized. The shared grow-action
helper checks readiness before changing resources, refuses occupied containers
or an active native task, sends the updated moisture, and consumes the existing
quick-water cost afterward. A throwing sync restores previous local moisture,
does not spend water, and reaches the tool's error feedback. Menu clicks cannot
also trigger quick tool actions in the world.

A successful sync call queues the native RPC; it is not a server-acknowledgment
test. Native growth rates, light/warmth checks, drying, and time requirements
are unchanged. Read-only native inspection confirms that growth requires
positive moisture and also depends on light and other native multipliers.
These concrete defects are repaired, but the reported need for repeated
watering has not been conclusively reproduced or resolved in-game.

## Files in this follow-up

- `Core.cs`: distinct menu-hotkey gate, native cursor retention, build marker.
- `Services/NativeActivityGateService.cs`: parent-screen tracking, removal of
  premature Character cleanup, and new phone input guards.
- `Services/GameplayStateGateService.cs`: separate menu and world-action gates.
- `Services/ThirdPersonCameraService.cs`: retain native screen camera ownership
  while allowing the mod menu on either tab.
- `Services/GrowActionService.cs`: native planting and checked moisture writes.
- `Services/GrowToolFallbackService.cs`: helper integration, world-input guards,
  and failure feedback for water/misting.
- `tests/NativeStateChecks`: linked production gates/helper, updated native
  ownership fixtures and regressions.
- This handoff and the README; a separate archive and verification evidence.

Previously requested police spawning rollback is retained:
`Services/VehicleService.cs` still matches the prior HEAD exactly. The earlier
Shitbox/SUV mismatch can therefore return with that requested rollback.
The clothing picker, all four groups / 31 visitors, new content catalogs,
Never Wanted, weather, config, S1API integration, and prompt repairs are retained.
No new dependency, config format, save migration or forced growth is introduced.

## Checks and evidence

- Release build: zero warnings and errors against generated 0.4.7 references.
- 20 linked production state/policy/grow-action checks, including both native
  tabs, exit/reopen, phone guards, busy containers and sync failures.
- 11 prompt-frame checks, six clothing-picker checks (120 expanded redraws),
  nine config checks, and nine accessory-failure checks: 55 total passing.
- Updated compiled API audit: 236 types, 695 members, 75 declared patch targets,
  12 dynamic patch targets; no errors, Special Customers present, S1API 3.2.1.
- Read-only PE/disassembly inspection; no process patching or memory allocation.
- Exact police-service rollback and whitespace checks; archive CRC and per-file
  hashes verified during packaging.

The recording is baseline evidence. Fixtures, assembly audits and compilation
do not establish actual Unity rendering, plant growth, or FishNet transport QA.

## Install, test and rollback

Close the game. Back up the existing `Mods/NugzzMenu.dll` outside Mods, then
replace it with the DLL in the test ZIP. Preserve `nuggzmenu-config.json`.
The inspected runtime log still loaded S1API `3.2.1-beta.9`; if that is still
installed, also back up and replace the matching included stable files:

- `Mods/S1API.Il2Cpp.MelonLoader.dll`
- `Plugins/S1APILoader.MelonLoader.dll`

Do not keep renamed duplicate mod/plugin DLLs in those folders. The example
config stays under `examples`; it does not overwrite existing preferences.

Use a copied/disposable save for runtime QA:

1. Open Phone, switch to Character, and change clothes normally. Open/close
   NugzzMenu with F8/controller from each tab. Clothing controls must remain
   usable after closing NugzzMenu.
2. Exit the native screen while Character remains selected. HUD, cursor lock,
   movement, menu access, and blur must restore. Reopen Character and repeat.
3. With NugzzMenu open and during its closing animation, try Tab/controller
   phone toggle and map/journal/messages shortcuts. They must not open the
   native phone; repeat after closing NugzzMenu to confirm normal input returns.
4. Plant several native seed types, water once, and allow native time to pass
   under a working grow light. Verify actual progress and moisture after
   stepping away. Also check misting a mushroom bed. Repeat as host/client
   with authorized menu access, and while an employee/player is using a pot.
5. Repeat the sequence associated with the dark tint. If it persists, retain
   the full fresh log and note whether Phone/Character headers remain visible.
6. Confirm the clothing picker still works without repeated exceptions and
   the police vehicle spawn behavior matches the requested rollback.

Rollback: close the game and restore each DLL replaced from its backup.
Preferences and save data do not need migration. No installation, save edit,
release publication or production change was performed by the agent.
