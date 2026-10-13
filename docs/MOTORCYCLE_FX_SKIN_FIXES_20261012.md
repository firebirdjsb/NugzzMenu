# Motorcycle interaction, animation FX and skin reset test build

Status: implemented and built against Schedule I 0.4.7f12. Offline checks pass;
native rendering, driving, audio and host/client runtime QA remain pending.

Startup marker: `Test build: Motorcycle interaction + animation FX + skin reset 2026-10-12`.
The menu's displayed version remains 1.0.0. S1API is pinned to stable 3.2.1.

## Findings and changes

- Car and motorcycle visible together: the sedan's LOD groups can re-enable
  renderers after the original one-time hide. Conversion now disables the
  cloned chassis's car LOD groups, retains its native driving components, and
  suppresses original renderers after `LandVehicle.SetVisible` and in the bike
  update. The motorcycle follows native distance visibility. Detach restores
  captured renderer/LOD settings. Other vehicles and registered prefabs are untouched.
- Unable to interact to drive: the previous conversion disabled the original
  `IntObj` collider and put its replacement on a child without the native
  interaction component. The replacement collider is now on `vehicle.intObj`.
  The native driving-state entry, driver ownership, throttle, steering, camera
  and exit path remain intact. Storage/trunk interactions are disabled on this
  temporary chassis; no inventory is cleared. The added collider is destroyed
  on rollback/detach.
- Revving while turning: the prop previously captured yaw only at start. Its
  native seat-orientation correction now follows the player's turn before
  complete-avatar seat alignment. The attached smoke and audio turn with it.
- Missing pee/vomit FX and revving sound: animator flags and render-only meshes
  do not start the NPC's separate effects. The new FX service copies validated
  effect-only `NPCAuxAction` templates for `IsPissing` and `IsVomiting`, positions
  them at the player's current hips/head with native relative offsets, and
  plays their particles/audio. Revving copies the native burnout VFX graph and
  audio clip/settings, using the native emission rate of 36. Idle NPC audio
  fades cannot mute the copied revving sound. No NPC AI, networking, rigidbody,
  burnout joint or arbitrary gameplay scripts are instantiated. Temporary FX
  end on Stop, action switching, movement, death, approval loss, scene reset,
  expiration or replacement of their attachment anchor. The NPC sources and
  existing player product particles/audio are not played or stopped.
- White skin after Clear FX: the menu calls every cached effect's cleanup.
  Native `Lethal.ClearFromPlayer` calls `SetSicklySkinColor(false)`, where `false`
  means “do not mirror,” rather than “disable.” That method adds a `sickly`
  color override; the installed log reports a white `1.1,1.1,1.1,1.1` result.
  After all cleanup attempts, the menu now removes the known skin overrides,
  restores the chosen `CurrentAppearance.SkinColor` (or native naked-appearance
  fallback), and resets the smoother's current/default colors. Mirrored display
  avatars are restored with cycle protection. Clothes, hair and saved
  appearance data are not rebuilt or rewritten. Other native cleanup routines
  can still warn when invoked without their expected state; skin restoration
  runs even after those failures.

## Scope and files

This revision is limited to the reported motorcycle visibility/mounting,
revving orientation, native pee/vomit/burnout FX and clearing-effect skin color.
The motorcycle remains a session-only model using a modified registered sedan
chassis; driving feel is an approximation. It is not a new network prefab or
owned vehicle. Previous crown, prompt, phone/clothing, watering, content,
weather and config repairs remain included. Police spawning remains reverted;
`Services/VehicleService.cs` is byte-identical to HEAD. No save/config schema
change, unrelated refactor, deployment or save editing was performed.

Changed source files for this pass:

- `Services/MotorcycleService.cs`, `Services/MotorcycleChassisSnapshot.cs`
- `Services/LocalAnimationFxService.cs` (new)
- `Services/LocalAnimationVisualService.cs`, `Services/LocalAnimationCatalog.cs`
- `Services/LocalAnimationNetworkService.cs` (matching-build handshake `body5`)
- `Services/EffectAppearanceResetService.cs` (new), `Services/EffectsService.cs`
- `UI/MotorcycleRenderer.cs`, `Core.cs` (test marker), `SeshMenu.csproj` (references
  to the installed Unity audio/particle/VFX bindings; no bundled game assemblies)
- `tests/LocalAnimationChecks/` (production-service fixtures and regressions)

## Verification

- Release build: zero warnings/errors.
- 55 local animation/motorcycle/skin checks, plus 20 state/grow, 11 prompt-frame,
  6 picker, 9 config and 9 accessory checks: 110 total.
- Compiled API audit: 253 game types, 770 members, 80 Harmony targets and 12
  dynamic targets; zero errors against the supplied updated dump and S1API 3.2.1.
- Generated Unity call inspection covers animation, FX, motorcycle and skin
  helper calls, including nested methods; no throwing unstripped bindings.
- Local serialized asset inspection confirms each native pee/vomit template has
  two particle systems and one AudioSource, with only the allowed FX/audio
  components. Native PE inspection confirms vehicle entry through the driving
  state, SetVisible's native visibility handling, burnout emission and Lethal's
  skin override call. These are read-only file checks, not live instruction or
  detour verification. No process-memory offsets, native JMP/CALL edits or
  allocated memory caves were introduced. `scripts/verify_offsets.py` is not
  present in this repository; no offset-writing module was created.
- ZIP CRC, payload hashes, dependency pair, preservation of earlier ZIPs, and
  unchanged installed DLL hashes are checked during packaging.

These fixtures exercise actual production control flow using an SDK fixture.
They do not simulate Unity rendering, native vehicle physics/audio or FishNet
packet delivery. No in-game/manual visual or two-player validation was performed.

## Installation, focused runtime QA and rollback

Close the game. Back up replaced DLLs outside `Mods`/`Plugins`, then replace the
package's `Mods/NugzzMenu.dll`, `Mods/S1API.Il2Cpp.MelonLoader.dll` and
`Plugins/S1APILoader.MelonLoader.dll`. Keep your existing `nuggzmenu-config.json`.
Install the same revision on host and clients, approve clients in Lobby and
allow two seconds for `body5` readiness. Solo needs no multiplayer handshake.
Earlier animation revisions do not satisfy the new motorcycle/body handshake.

Use a copied/disposable save:

1. Vehicles > Spawn Motorcycle, close the menu, aim at the bike and interact.
   Check only the bike model appears, steering/throttle/braking/exit work, and
   there is no trunk/storage prompt. Move away and return to test visibility.
2. In third person, play Revving and turn left/right. Check bike/saddle follow,
   hair stays attached, the rear tire emits smoke and spatial sound is audible.
3. Play Peeing and Vomiting as solo/host and as an approved client. Check the
   stream starts at the hips/mouth on both observers, audio is audible, and Stop,
   movement and expiry remove only the temporary FX. Verify regular consumed
   product particles/audio still work independently.
4. With a non-white skin tone, press Clear FX with no active effects, then after
   Lethal/skin-changing effects, and repeat. Check first/third-person and phone
   character displays retain the selected skin tone and clothing.
5. Have an approved client ride a host-spawned bike, then exit. Check both
   players see the same model/rider and controls transfer correctly. Remove
   unoccupied bikes and reload the copied save to verify they are not saved.

Rollback: close the game and restore all replaced DLLs from their backups.
The agent did not install this build, modify saves or touch production.
