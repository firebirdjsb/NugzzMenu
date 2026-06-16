# NugzzMenu v0.9.0

This update is a big cleanup and stability pass for NugzzMenu. It focuses on
making the menu feel smoother in real gameplay, especially when building,
using third person, playing multiplayer, and messing with story/world tools.

## New Stuff

- Added an adjustable third-person camera that stays locked behind the player.
- Added working host-only RV controls to blow up the story RV and repair it
  again.
- Added Benzie Manor access controls for hosts.
- Added player size controls in the Cheats tab. This is still marked buggy
  because multiplayer scale syncing is limited by the game.
- Added stacked lobby FX support so multiple effects can run together instead
  of replacing each other.
- Added clearer host-only feedback for tools that only the lobby host can sync.

## Build Anywhere

- Made Place Anywhere much more reliable for grid, surface, and wall-mounted
  placeables.
- Fixed several placement crashes caused by missing build data.
- Fixed objects snapping up, floating, blinking, or falling through the floor
  while placing.
- Improved close-to-feet placement so objects stay grounded when aiming down.
- Fixed invisible placed objects such as drying racks and other buildables.
- Added support for picking up placed objects outside owned areas and returning
  them to inventory when there is room.
- Added safer multiplayer handling for placed objects so non-host clients do
  not spam null-reference errors as easily.

## Combat And Camera

- Fixed punch damage while NugzzMenu is loaded.
- Fixed PvP melee and fist hits so bats, machetes, pans, and punches can damage
  other players through the game's normal hit path.
- Fixed third-person melee getting stuck after swinging.
- Fixed baseball bat camera flicker in third person.
- Fixed first-person viewmodels after switching between first and third person,
  so arms and held weapons show correctly again.
- Fixed third-person vehicle visibility so the local pawn is hidden in vehicles
  and restored after leaving.
- Fixed skateboard visibility issues in first person.

## Cheats And Player Tools

- Fixed God Mode toggle behavior.
- Fixed infinite stamina.
- Fixed speed boost and added a multiplier.
- Fixed fly toggle state text.
- Removed duplicate infinite energy behavior because stamina already covers it.
- Improved ragdoll and stand controls so standing restores player control.
- Added host-only warnings for vehicle/world tools in multiplayer.

## Items And FX

- Fixed item spawning quality so items with quality support use the selected
  quality instead of always spawning as standard.
- Items that do not support quality are left alone.
- Improved lobby FX so other players can see effects without needing NugzzMenu
  when the game supports the effect path.
- Added Clear FX behavior for the active FX stack.
- Lethal FX now waits longer before killing the player.

## Menu And Stability

- Removed the old plugins tab and related dead plugin UI.
- Improved dynamic menu sizing so tabs fit their content better.
- Fixed cursor behavior when opening and closing NugzzMenu from the main menu
  and pause menu.
- Added compatibility handling for different S1API package names and versions.
- Added safety guards around several vanilla null-reference crashes triggered
  by building, NPC animation, and scene changes.

## Install

Install MelonLoader IL2CPP and S1API for Schedule I, then place
`NugzzMenu.dll` in the game's `Mods` folder.
