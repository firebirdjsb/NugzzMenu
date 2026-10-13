# Solo authority and complete-avatar seating — 2026-10-12

Status: implemented in a separate test build. Release compilation, 99 offline
checks, API/Harmony target inspection and Unity binding inspection pass.
The four reported visual/gameplay cases still require in-game retesting.

## Findings and repairs

The user confirmed the motorcycle rejection occurred in **solo** play. The
earlier animation/motorcycle code used a helper requiring a Steam lobby host.
That contradicted the existing menu policy allowing solo play. It also prevented
Boinking from passing its matching-host readiness gate in a solo game.

These features now identify the local native server using the local player's
`IsServerInitialized` flag. A client cannot gain server authority from a Steam
lobby flag or menu approval. Existing session approval and other cheats' authority
policies were not changed. Multiplayer spawning remains host-only; approved
clients can ride and use the Local Effects actions through the existing host path.

The old seating implementation moved a hip joint at camera render time and
assumed that was sufficient to seat the rendered avatar. The screenshots show
standing/off-chair placement and hair separated from the head. The new game
appearance system provides explicit attachment-anchor joints; those now take
precedence over similarly named cached/mesh bones for seating and Boinking.

Laptop and motorcycle seating now align the **complete child avatar visual**
to the seat. They never translate the player's movement/network root, rewrite
joint positions for seating, or remove hair/clothing. Stop restores the saved
avatar visual position when the same living rig remains under our control.
Appearance replacement and ragdoll hand transform ownership back to the game.

The mod's LateUpdate now applies all active cosmetic poses before camera culling,
with the existing camera callbacks retained as a fallback. Seated animator flags
are evaluated with zero elapsed time once per frame, before seat alignment,
so late native standing/IK updates cannot leave alignment based on an earlier
standing pose. Repeated camera passes do not advance animation. Revving explicitly
disables the ordinary car/chair sitting layer while its motorcycle pose is active.
Rideable-bike riders use the same complete-avatar placement path.

## Changed files and boundary

Changed `Core.cs`, `LocalAnimationNetworkService.cs`,
`LocalAnimationPlacementService.cs`, `LocalBodyAnimationService.cs`,
`LocalAnimationVisualService.cs`, `LocalAnimationCatalog.cs`,
`MotorcycleService.cs`, `MotorcycleNetworkService.cs`, `UI/MotorcycleRenderer.cs`,
animation fixtures/checks, README and codebase documentation.
Services are under `Services/` unless otherwise noted.

Police spawning stays reverted: `Services/VehicleService.cs` remains identical
to HEAD. Existing phone/Character, prompts, watering, clothing picker, content,
weather and config changes remain included. Bikes still use a modified native
sedan chassis and are session-only, capped at four. No persistence/config format,
native networking prefab or game asset changes were introduced.

## Validation

- Release build: zero warnings/errors.
- 99 checks: 44 animation/motorcycle, 20 native-state/grow, 11 prompt-frame,
  6 picker, 9 isolated config and 9 accessory checks against the built DLL.
- Compiled API audit: 246 types, 743 members, 79 Harmony targets and 12 dynamic
  targets; zero errors against the supplied 0.4.7f12 dump and stable S1API 3.2.1.
- 586 Unity call sites, including nested callback/rollback types, avoid generated
  throwing stubs. Read-only serialized/native inspection confirms the attachment
  provider's body hierarchy, native seat positions and unowned save-list boundary.
- ZIP CRC, file hashes, exact three-DLL payload and unchanged installed DLL/earlier
  archive hashes are checked during packaging. Extracted game assets are excluded.

The fixtures now represent a child avatar instead of incorrectly putting its
rig on the movement root. New cases cover solo authority, actual client refusal,
current attachment bones, whole-visual/hair translation, once-per-frame seated
evaluation, appearance replacement and refusing unsafe movement-root fallback.
Fixtures do not establish actual Unity rendering, IK, physics or two-player delivery.

## Install, retest and rollback

Close the game and back up replaced DLLs **outside Mods/Plugins**. Install the
included menu and stable S1API 3.2.1 mod/loader pair together. Preserve
`nuggzmenu-config.json`; `examples/` is reference-only. Host and clients need this
matching revision, whose animation readiness is `body4`. Approve clients in Lobby
and allow two seconds for readiness. Earlier animation revisions do not match.

Startup marker: `Test build: Solo authority + complete avatar seating 2026-10-12`.

Using a copied/disposable save, test these exact cases:

1. Solo: Boinking displays its countdown and visible body motion in third person;
   Stop/movement restores the pose and any refusal remains visible in the panel.
2. Laptop: hips sit on the chair, feet/typing pose stay aligned, and hair follows
   the head. Repeat after changing clothes/hair, then Stop and walk away.
3. Revving: hair remains on the head, hands/body align to the saddle, and Stop,
   ragdoll and timeout remove the prop and restore normal posture.
4. Solo: spawn a motorcycle in clear ground ahead, interact to ride, drive/exit
   with keyboard and controller, then Remove Unoccupied.
5. Repeat Local Effects and riding with matching approved host/client builds;
   check the other player's view and a newly approved observer.

Motorcycle steering/physics remains a car-controller approximation. Exact
hand/foot placement, clothing clipping and visual delivery are not yet verified
in-game. Restore every replaced DLL from its backup to roll back. The agent has
not deployed this revision or edited installed game files, saves or production.
