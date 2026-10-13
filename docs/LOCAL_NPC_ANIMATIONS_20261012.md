# Local NPC animations test build — 2026-10-12

This describes the earlier tent-based test. The latest
[custom player-body revision](CUSTOM_PLAYER_BODY_ANIMATION_20261012.md)
replaces Boinking with player-joint motion and includes the other seven actions.

Status: implemented for Schedule I 0.4.7f12; offline checks pass. Actual native
playback, prop placement and two-player visibility still require in-game QA.

Open **Lobby → Local Effect → Local NPC Animations**. Choose an action for your
own player. The highlighted action and remaining time appear beside **Stop
animation**. Actions last 20 seconds and stop on movement, death, ragdoll,
vehicle entry, native activity/phone/Character takeover, loss of approval,
scene changes or mod shutdown. Stand, Ragdoll, Clear FX and Reset also end them.
Use the existing third-person camera to inspect your character.

| Action | Native animation / visual |
| --- | --- |
| Talking on phone | `RightArm_HoldPhone_Raised`, native `PhoneEquippable` through the player networked equipper. Put away your held item first. |
| Using laptop | `Sitting` + `IsTyping`, temporary native laptop mesh. |
| Sitting on ground | `IsCasualSitting`. |
| Dancing | `IsDancing1`; temporarily disables the other two dance variants. |
| Peeing | `IsPissing`; temporarily disables the alternate variant. |
| Vomiting | `IsVomiting`. |
| Boinking (tent) | Native legacy `OrgyTent` clip on a temporary tent beside your player. The game has no humanoid boinking clip. |
| Revving motorcycle | `IsMotorbikeSitting`, native motorcycle mesh and rear-wheel spin using the native RPM. |

These are animation effects. They do not start NPC AI, phone conversations,
building occupancy, crime, bodily-fluid gameplay effects, or drivable vehicles.
The motorcycle visual does not run the native burnout physics or engine audio.
Your clothes remain editable through the native Character panel. Animations
end when that screen takes ownership rather than replacing its avatar controls.

## Host and client paths

Install this same test build on the host and participating clients, with the
matching stable **S1API 3.2.1** DLL/loader pair. The host must approve clients in
Lobby as usual. Actions target the owning local player; the selected lobby
player is not the animation target.

Body parameters use `Player.SendAnimationBool`, the game's owner-checked
ServerRpc → ObserversRpc path. The phone uses `Equip_Networked`; Stop unequips
only the handler this feature created. Parameter snapshots restore only the
booleans owned by the action, preserving unrelated native state.

The other temporary props require the matching menu on observers. Requests
travel through the owner-checked `SendAnimationTrigger` RPC. The mod consumes
only its reserved request names before they reach the animator. Inspection of
the actual 0.4.7f12 PE shows both the sender and server reader call
`SetAnimationTrigger_Networked` directly: the generated RpcLogic method is
inlined. The Harmony patch therefore intercepts the real broadcast method.
The server validates approval, bounds and source identity, then sends visual
states only to approved clients through the existing Player target-value RPC.
Receivers accept actor state only from the known host. Newly approved clients
receive the remaining duration and the current native body pose.

Visuals copy native mesh/transform data into mod-owned objects and reuse the
native tent clip. They copy no AI, building, network, collider or vehicle
components, and allocate no native burnout hinge/anchor bodies. Missing assets
or parameters give a warning and unwind the action instead of inventing an
animator name or repeatedly logging an error.

## Files and evidence

New files: `Services/LocalAnimationCatalog.cs`, `LocalAnimationService.cs`,
`LocalAnimationNetworkService.cs`, `LocalAnimationVisualService.cs`,
`UI/LocalAnimationRenderer.cs`, and `tests/LocalAnimationChecks`.
Integration touches `Core.cs`, `UI/LobbyTabRenderer.cs`,
`Services/SessionAuthorityService.cs`, `Services/PlayerCheatService.cs`, README
and the codebase map. The previous phone, watering, crown, prompt, color,
weather, visiting-customer, config and S1API fixes remain included. Police
spawning still matches the requested original implementation.

- Release build: zero warnings/errors.
- 20 linked-production animation checks: host/client paths, ownership, denial,
  invalid parameters, switching, phone handler isolation, expiry/movement,
  cleanup, malformed/forged network states, newly approved observers, visual
  creation failure, native tent/wheel setup and bounded button layout.
- Prior checks: 20 state/grow/policy, 11 prompt, 6 picker, 9 config, 9 accessory.
  **75 total checks**. Fixtures do not simulate Unity or FishNet transport.
- Actual game assets: all 12 referenced bool parameters exist with Bool type;
  11 tent/motorcycle templates contain renderers, the tent clip exists and the
  motorcycle wheel is inside its copied hierarchy. Phone and laptop assets
  were identified in the updated asset files.
- Compiled game API/patch audit against the updated supplied dump: no errors,
  stable S1API 3.2.1. New animation Unity call sites avoid known throwing
  generated stubs. ZIP includes bounded audit reports and file hashes, not
  game assemblies or meshes.

The native-code inspection reads file bytes only. No process-memory writes,
raw-offset injection, game-file installation or save editing was performed.

## Install, test and rollback

Close the game and back up `Mods/NugzzMenu.dll` outside the mod folders. Replace
it with the ZIP's menu DLL on both computers. Use the included stable S1API
DLL/loader pair if your installation differs; back up each file you replace.
Keep `nuggzmenu-config.json` unchanged. Never leave renamed backup DLLs in Mods
or Plugins. The example config is reference-only and is not an active preset.

Expected startup marker:
`Test build: Local NPC animations + Phone character / watering fix 2026-10-12`.

Test on a copied/disposable save:

1. As host, try all eight actions and inspect them in third person. Check the
   phone prop, laptop/hand placement, ground pose, dance, pee/vomit poses,
   moving tent and motorcycle/rider alignment. Check Stop and natural expiry.
2. Join with the matching client build, approve it, and repeat all eight on
   the client. Check the owner's view and the other player's view in both
   directions. Watch for unknown-parameter or stripped-method log warnings.
3. Switch actions rapidly; move/jump, ragdoll, die, enter a vehicle, open the
   native phone/Character screen and leave the scene. Check that temporary
   props disappear, the avatar stands normally and held inventory is retained.
4. Deny and reapprove a client; approve a newly joined observer while an effect
   is active. Verify the remaining animation/visual appears and later stops.
5. Repeat the phone/Character exit, clothing picker, grow prompt and watering
   checks from the prior handoff. The police-spawn revert remains intentional.

The live visual alignment, animation transitions, Harmony detours and actual
host/client packet delivery are residual risks until those checks are run.
Rollback by closing the game and restoring every replaced DLL from its backup.
No new dependencies, config-format changes, save migrations or production
writes were introduced. The agent has not deployed this build.
