# Custom player-body animation — test revision 2026-10-12

Historical revision: superseded by the
[animation seating and motorcycle repair](ANIMATION_MOTORCYCLE_REPAIR_20261012.md).
Keep its earlier ZIP only for rollback; use the new handoff for current QA.

Status: the **Boinking** action now animates the player's body. The previous
tent action is removed from this revision. Host/client paths and cleanup pass
offline checks; actual native playback and two-player visual QA remain pending.

Open **Lobby → Local Effect → Local NPC Animations → Boinking**. This is a
clothed, standalone procedural emote with coordinated hip, torso, arm and leg
motion, a 1.25-second loop and smooth entry/exit. It uses your existing player
rig and outfit. No animation bundle or external assets are required.

It lasts 20 seconds. Stop, Clear FX, Stand, switching actions, movement,
ragdoll/death, vehicle entry, native phone/Character takeover, avatar replacement,
scene changes and loss of approval end it. The other seven animation actions
and prior fixes remain included. Police spawning remains reverted.

## Implementation and multiplayer

`Services/LocalBodyAnimationService.cs` resolves the game's named joints from
the native avatar's bone list. The 27 inspected serialized rigs contain all
11 required joints. No process offsets or invented animator parameters are
used. Curves change render-skeleton position/rotation only; player root,
movement/collision, clothing, scale and animator controller remain native.

The native `AvatarAnimation.LateUpdate` postfix applies the pose after the
native bone-cache update. A mod LateUpdate fallback covers hidden/cull-disabled
components. Both use fixed snapshots, so repeated application cannot accumulate
rotation or translation. Stop restores the owned joint snapshots. A replaced
rig or native ragdoll/death retains control of its transforms.

Requests keep the existing owner-checked native RPC and host approval. A new
readiness/ack exchange identifies peers with this body-animation revision.
An older host cannot enable this action, and an older tent build cannot request
or receive it as a tent. Other seven actions keep their existing network paths.
The host sends remaining duration and elapsed loop milliseconds to matching
approved clients; local resynchronization retains the original joint snapshots.
Newly approved matching observers receive the current phase. Network latency
can still produce small differences in visible timing.

## Files and checks

Added `Services/LocalBodyAnimationService.cs`; changed the animation catalog,
local service, visual service and network service, `Core.cs`, the linked
animation tests, README and codebase map. The tent lookup/clone/playback code
is removed from the production visual service. No config-format changes,
save migrations, actual vehicle changes or production writes were made.

- Clean Release build, zero warnings/errors.
- 27 animation checks using the production services: all eight actions on
  host/client paths, ownership and approval, native parameter validation,
  custom curve bounds/fade/period, repeated frame application, root/controller
  preservation, joint restoration, ragdoll/replaced-rig ownership, trusted
  readiness, legacy-peer rejection and elapsed-phase snapshots.
- 55 prior checks: native state/grow policy, prompts, clothing picker, config
  and accessory failure handling. **82 checks total**.
- Compiled game API and Harmony target audit against the updated dump; native
  joint/animation/RPC file audits; generated Unity call-stub inspection.
- ZIP CRC and per-file hashes verified. Game DLL hashes remain unchanged
  during packaging. No game assemblies, meshes or extracted animation assets
  are distributed.

Fixtures and file audits do not execute Unity or FishNet. Joint orientation,
native callback ordering, first/third-person visibility, clothing clipping and
actual two-player delivery remain the runtime verification boundary.

## Install, QA and rollback

Install this **matching custom-body revision on host and clients**. Close the
game, back up DLLs outside Mods/Plugins and replace the included files. The
bundle includes the matching stable S1API 3.2.1 DLL/loader pair. Preserve your
`nuggzmenu-config.json`; the example is reference-only. Do not leave renamed
backup DLLs in the loader folders. The earlier tent-test ZIP remains intact.

Startup marker:
`Test build: Custom player-body animation + Local NPC animations 2026-10-12`.

Test on a copied/disposable save:

1. As host, play Boinking and inspect in third person. Check coordinated body
   motion, clothing, stable feet/root, loop transitions, Stop and timeout.
2. Approve a matching client and wait up to two seconds for readiness. Play
   the action on each player and inspect from both computers. Check Stop,
   movement, switching actions, and a newly approved observer midway throughCharacter clothing
   changes. Verify native controls work and no joint pose remains stuck.
4. Check that an older tent build cannot trigger this body action. Repeat
   the other seven actions and the previous phone/prompt/watering/picker QA.

Rollback: close the game and restore every replaced DLL from its backup. The
agent has not deployed this build or edited saves/production files.
