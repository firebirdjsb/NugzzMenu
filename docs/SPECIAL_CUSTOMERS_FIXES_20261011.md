# NugzzMenu: Special Customers follow-up test build

**Superseded police/picker behavior:** the subsequent
[police revert and picker repair](POLICE_REVERT_COLOR_PICKER_20261011.md)
restores the previous police spawning path and replaces the broken swatch
draw call. The police-model fix and startup marker below describe the earlier
artifact. Its other compatibility features remain included.

Target: Schedule I **0.4.7f12**, Steam build **25796331**. This is a local test
artifact dated 2026-10-11, with the existing menu version label 1.0.0. It has
not been published or installed into the game by the agent. The startup log
identifies it as `Test build: Special Customers fixes 2026-10-11`.

## Changes and causes

| Report | Evidence and change | Runtime acceptance check |
| --- | --- | --- |
| Trimmer click prompt loses to “Use trimmers to harvest” | Native `InteractableObject.ShowMessage` writes the same canvas after the tool update. The native container update can also restore its fallback label. Current-frame ownership now suppresses that conflicting display writer only for the validated grow target, and keeps the fallback label consistent. The shared renderer activates the hidden parent canvas. | Equipped trimmers show the LMB Harvest action continuously while aiming at a mature plant; aim-away, adjacent pots, tool changes, and phone/menu transitions clear it correctly. |
| Phone > Character > close phone hides HUD and blocks F8 | Native `CharacterInterface.Close` hides the panel but does not remove its gameplay state. Phone closure now removes that panel's native state with `SetIsActiveGameplayScreen(false)`, retaining its owner even after `ActiveApp` is null. | Close the whole phone directly from Character repeatedly; HUD and menu recover without reopening the phone. TV, console, shops, and open-phone input blocking still work. |
| Shitbox Police spawns SUV | Police-station deployment chooses its own model. The menu now spawns the cached selected police prefab through its existing networked custom-vehicle path. | Shitbox Police and police SUV spawn as their respective models; seats, ownership, collision, lights, sirens, and client visibility work. |
| Missing `ApplyAccessorySettings` warning | 0.4.7 removed that legacy method. Its optional Harmony target is now found with quiet reflection and skipped when absent. The new AvatarObject crown guard remains active. | No missing-method warning at startup. Equip the sewer-office shelf crown and the dweller inventory crown separately. |
| Never Wanted still attracts police | Clearing the wanted meter alone does not prevent detection or stop active AI. Host-side guards now reject local-player crimes, detection, investigations, pursuit, body search, and police combat starts; an immediate cleanup stops active local pursuits/searches/combat. | With the cheat enabled, assault/shooting and checkpoint interactions do not start police action against the local player. Turning it off restores normal reactions. Other players and non-police NPCs retain their behavior. |

The shelf-crown crash's exact original cause is still unconfirmed. Exception
containment and the new avatar binding are included, but neither metadata
inspection nor a classifier fixture proves that the original shelf case is fixed.

## New content coverage

The read-only serialized-asset audit resolves group leader/customer references
to actual NPC IDs, checks registered item definitions and AvatarObjects, and
checks the native console teleport-point hierarchy. It passes for all content
listed below. It does not execute the menu in Unity.

| Surface | Verified additions | Menu access |
| --- | --- | --- |
| Visiting NPCs | **31**, across Hippies (8), Party Bus (9), Businessmen (6), Bikers (8) | Relationships group filters and searchable roster. People not currently spawned remain visible as “Not in town”; edits require a live NPC. |
| Group arrivals | All four native groups | Select a group in Relationships, then **Queue … for tomorrow**. Host-only; feedback confirms the game scheduled it. Sleep to the next day. This replaces the current native group schedule. |
| Inventory content | **26**: three rubber mats, Persian rug, and all 22 listed clothing/accessory items | Items, using actual `ItemDefinition.ID` registrations. |
| New appearance styles | **10**: three hairstyles and seven facial hair styles | Native appearance/shop flow. These are AvatarObjects, not fake inventory items. Their presence is checked by the content audit. |
| Teleports | `southoverpass`, `suburbia`, `park`, `westville`, `casino` | Teleport catalog, from the game's native point transforms. |
| Business | Hyland Medical | Existing business discovery and property controls. |

The exact expected roster and native IDs are in `0.4.7-content.json`; the asset
audit report includes every visitor ID. The native game spells Vince's name
“Vince Knuckes”; the menu displays **Vince Knuckles** by confirmed ID
`vince_knuckles`, without changing game data.

New NPC activities (phone/laptop, sitting, dancing, peeing, vomiting, boinking,
and motorcycle revving), visit scheduling, laundering capacities, pooled
stored items, icons, and the new appearance/save format remain native game
behavior. Queuing uses the actual `QueueCustomerGroup` method, rather than
constructing visitors or replacing their schedules and animations.

Settings includes **Audit 0.4.7 content registrations**. Run it in a loaded
save to check the menu catalog and live registrations. Missing entries are
reported individually in the MelonLoader log. Presence is not a rendering or
behavior test.

## Clothing colors

Items start with **Native / No color**. The normal spawn path calls the item's
native default factory. Only selecting a tint changes future colorable clothing
spawns; choosing Native restores original defaults. Queued spawns retain the
color selection made when they were requested. Fixed-color items always use
their native factory.

The expanded picker shows all **27 native tints** with swatches and selection
labels. Gold, silver, and bronze tone shortcuts select Yellow, Light Grey, and
Tan respectively. These are tint approximations, not new metallic materials.
Native Gold/Silver Aviators and the three Tie-Dye Shirts have separate spawn
shortcuts that retain their actual original finishes/patterns regardless of
the selected tint. The game saves clothing colors as an enum, so arbitrary
RGB materials or procedural tie-dye have not been introduced.

## Editable startup defaults

The file is **`nuggzmenu-config.json`**, beside the loaded `NugzzMenu.dll`
(normally the game's `Mods` folder). The spelling follows the requested
`nuggzmenu-config` name. Startup creates it when missing, preserving existing
keybind/HUD preferences. Cold defaults leave cheats off and clothing Native.

Use Settings > Startup Defaults / Config:

- **Save current as defaults** captures current menu preferences. Replacing an
  existing config preserves its previous contents in `.json.bak`.
- **Load / reload config** validates and applies edits. You can edit the JSON
  outside the game and reload it, or restart the game.
- Fly and third-person enable defaults wait until the local player is loaded
  and gameplay input is available. Existing multiplayer authorization remains.

`examples/nuggzmenu-config.json` is a reference, not an installer that overwrites
your preferences. Editable fields include keybind, startup tab, HUD/logging,
double-space fly, spawn quantity/quality/tint, player cheat toggles/tuning,
fly/third-person defaults, camera tuning, and light/reflection preferences.
Unknown fields survive saving; comments and trailing commas are accepted on
load, though saving normalizes formatting and removes comments. Unsupported
versions, invalid keys/tints, nonfinite numbers, or files over 64 KB are rejected
without partially applying settings or replacing the invalid file. Numeric
tuning values are clamped to the same bounds used by the menu.

The config does not store balances, ownership, relationships, save slots, or
one-time world actions such as item spawns or queuing a group. It does not
automatically overwrite your defaults when quitting.

## S1API and installation

This package includes the stable
[S1API 3.2.1](https://github.com/ifBars/S1API/releases/tag/v3.2.1) IL2CPP mod and
loader pair. The checksum and download URL are pinned in `S1API.lock.json`.
The menu uses public S1API item/NPC discovery, lifecycle events, time changes,
and weather discovery, supplemented by native content registries. See
`SPECIAL_CUSTOMERS_COMPATIBILITY.md` for the contracts and build references.

1. Close the game. Back up the current menu DLL and both S1API DLLs **outside**
   the game's Mods/Plugins folders. Preserve an existing config too.
2. Copy `Mods/NugzzMenu.dll` and `Mods/S1API.Il2Cpp.MelonLoader.dll` into the
   game's `Mods` folder, and `Plugins/S1APILoader.MelonLoader.dll` into `Plugins`.
   Replace the corresponding files; do not leave duplicate renamed mod DLLs.
3. Retain an existing `nuggzmenu-config.json`. Otherwise startup creates it.
4. Launch and confirm the test-build log line and S1API 3.2.1, then test using
   a copied/disposable save for actions that alter gameplay or visitor timing.

**Rollback:** close the game, restore all three backed-up DLLs, and restore the
config if you changed it. To return to MelonPreferences-only behavior with an
older menu, move the new JSON config outside Mods as well. No save migration is
introduced; ordinary in-game actions can still be saved by the game.

## Verification and remaining QA

- Release build: **0 warnings, 0 errors** against updated generated interop.
- Compiled metadata audit: **234 types, 695 members, 70 attribute-based Harmony
  targets, 12 dynamic targets**, required update markers, and public S1API 3.2.1
  contracts pass.
- **39 focused checks pass**: accessory classification (9), prompt frame
  rendering (11), isolated config files (9), and native-state policy fixtures
  for phone/prompt/police behavior (10).
- Serialized asset audit: **4 groups / 31 visitors / 26 items / 10 cosmetics /
  5 teleports / Hyland Medical**, no errors.
- ZIP integrity and manifest hashes are verified during packaging.

Reproduce the acceptance cases in the first table; test each new item with
Native selected, explicit tint, and Native again. Visit each group and test
relationship edits only after arrival. Confirm controller navigation, long
names, changing saves, weather controls (Clear/Fog/Light Rain/Storm/lightning),
and host/client behavior. Never Wanted and group scheduling are host-only;
the cheat protects the local host player, not every player in the lobby.

The fixtures use production code with narrow game API doubles. They do not
execute Harmony, Unity rendering, native AI, or FishNet replication. **Actual
in-game QA remains pending.** No game installation, saves, published release,
or production environment was modified to produce this test package.

## Reproducing offline checks

```powershell
dotnet build SeshMenu.csproj -c Release
dotnet run --project tests/GameApiCompatibilityChecks -c Release -- bin/Release/net6.0/NugzzMenu.dll <updated-dummy-directory> --require-special-customers --s1api net6.0/S1API.Il2Cpp.MelonLoader.dll --report trialbuild/prompt-diagnosis/followup-api-audit.json
dotnet run --project tests/AccessoryFailureChecks -c Release -- .
dotnet run --project tests/InteractionPromptFrameChecks -c Release
dotnet run --project tests/MenuConfigChecks -c Release
dotnet run --project tests/NativeStateChecks -c Release
```

The separate asset auditor requires UnityPy in an isolated tools directory;
UnityPy is not a runtime mod dependency. Point `PYTHONPATH` at that directory:

```powershell
python scripts/audit_update_assets.py --game-data '<game>/Schedule I_Data' --report trialbuild/prompt-diagnosis/update-content-audit.json
```

Changed surfaces: Core lifecycle/default loading; ItemService and the clothing
picker; RelationshipService, SpecialCustomerService and its group UI;
NativeActivityGateService; grow prompt ownership/display services;
NeverWantedService and PlayerCheatService; VehicleService; TeleportService;
the optional avatar hook; Settings and content audit; S1API lock/reference;
the focused tests, scripts, and documentation. No unrelated save/schema,
network protocol, authentication, or permanent-progress refactor is included.
