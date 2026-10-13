# Crown and hover test build

Based on GitHub main at `2412c7c` (NugzzMenu 1.0.0). These changes are a local
test build, not a published Nexus release.

## Scope and evidence

- Custom grow and shape hover prompts: source inspection found a 50 ms target
  cache in both paths and shape display calls made during `Core.OnUpdate`.
  Grow prompts already used the native interaction canvas LateUpdate prefix.
- Sewer-office shelf trash crown: reported against the latest Nexus release.
  No matching crash log or live reproduction was available. The installed game
  was not launched for this work. The crown's exported clothing definition points
  to `Avatar/Accessories/Head/TrashCrown/TrashCrown`, and that prefab exists in the
  exported assets. A missing crown prefab is therefore **not an established
  cause**.
- The existing accessory finalizer only handled the missing-prefab error. The
  added null-reference containment is a mitigation for crown settings, not proof
  that this is the reported failure. It can leave an accessory visually incomplete
  if native application fails.

No save edits, clothing definition changes, multiplayer authority changes,
deployment, or unrelated refactors are included. The subsequent
[0.4.7 compatibility pass](SPECIAL_CUSTOMERS_COMPATIBILITY.md) updates S1API and
adds a crown guard on the new `AvatarAppearance.ApplyAvatarObject` path. The
original shelf-crown failure's root cause remains unconfirmed.

## Changes

- `Services/GrowInteractionPromptPatch.cs`: refresh shapes and grow prompts in a
  last-priority postfix after native canvas LateUpdate work. The subsequent
  [left-click prompt fix](LEFT_CLICK_PROMPT_FIX.md) activates their display in
  the current frame after the equipped-tool hide rule in native Update.
- `Services/GrowToolFallbackService.cs`: validate current tool/input state and
  refresh the target each display frame; discard older queued prompts.
- `Services/ShapePrefabService.cs`: refresh shape prompts at canvas display time
  and resolve the actual current target when the player clicks.
- `Services/ThirdPersonInteractionPatch.cs`: contain managed/native-wrapped null
  references only when trash-crown settings are present; preserve unexpected
  exceptions and log the original accessory error at most once every 30 seconds.
- `tests/AccessoryFailureChecks/`: exercise the exception classifier in the built
  DLL, including pass-through for unrelated clothing and unexpected crown errors.

## Automated checks

```powershell
dotnet build SeshMenu.csproj -c Release
dotnet run --project tests/AccessoryFailureChecks -c Release -- .
git diff --check
```

Baseline and patched Release builds succeeded with zero warnings and errors.
All nine accessory failure checks passed. No raw memory patches or allocation
caves are introduced; offset verification and JMP/CALL integrity checks do not
apply to this managed Harmony change.

## Required in-game verification

These checks remain pending; automated checks do not establish their outcomes.

1. Pick up the crown from the sewer-office shelf. Equip and remove it from the
   clothing screen, close the screen, and confirm F8 still opens/closes Nugzz.
   Repeat in first person and third person. Compare with the dweller's crown.
2. If the menu still fails, retain `MelonLoader/Latest.log`. Check whether it
   contains the new `Contained avatar accessory failure` warning. Its absence
   suggests the reported problem occurs outside this guard.
3. Aim across adjacent grow containers with each supported tool. Check prompt
   responsiveness, tool switching, starting a task, and moving the aim away.
4. Aim at adjacent shapes, move the camera quickly, then click. Confirm the
   currently aimed shape is picked up and native prompts remain available after
   leaving the shape.
5. Open the menu, pause, console, phone, and clothing screen. Custom prompts must
   disappear while these interfaces own input. Test shape pickup as both host
   and an approved client; authority behavior must remain unchanged.

## Deployment and rollback

The DLL is in `bin/Release/net6.0/NugzzMenu.dll`. Nothing was copied to the game's
Mods folder, pushed to GitHub, or published to Nexus. If installing for testing,
retain the previous DLL and restore it with the game closed to roll back. No
production or saved gameplay data was touched.
