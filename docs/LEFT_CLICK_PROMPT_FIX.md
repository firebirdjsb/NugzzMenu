# Left-click grow prompts: 0.4.7f12 test build

**Historical first prompt build:** this document records the earlier canvas
repair. The new screenshot exposed an additional native message writer that
can overwrite it. The current fix and S1API 3.2.1 installation instructions
are in [SPECIAL_CUSTOMERS_FIXES_20261011.md](SPECIAL_CUSTOMERS_FIXES_20261011.md).
The beta.9 dependency and menu-only package instructions below apply only to
the earlier artifact.

The user screenshot shows a mature plant, equipped trimmers, and the native
"Use trimmers to harvest" world label, without a left-click action. The installed
menu at that test had SHA-256
`07693e3510de997f6a0cbbf7f556fe2cc819d5ef005fb422cbdc93d25df7c07e`,
matching the previous local test artifact. Its log recorded the prompt patch and
S1API 3.2.1-beta.9 loading without a prompt-patch error.

## Diagnosis

Read-only native disassembly of the current game's `GameAssembly.dll`, paired
with the replacement dummy dump, establishes this display behavior:

- `InteractionManager.IsAnythingBlockingInteraction` returns true when an
  equipped tool's `CanInteractWhenEquipped` is false.
- `InteractionCanvas.Update` clears its display-request flag in that case,
  hides its main prompt container, and enables the canvas only when that flag
  or separate world labels require it. It clears the flag at the end of Update.
- `EnableInteractionDisplay` sets prompt content and the display-request flag;
  it does not activate the parent prompt container or enable the canvas.
- `InteractionCanvas.LateUpdate` refreshes separate world labels. It does not
  consume the main prompt display flag, contrary to the previous patch comment.

Thus, calling EnableInteractionDisplay later in the frame cannot show the
custom trimmer action on its own. The parent remains hidden, and the next
Update discards the request under the equipped-tool rule. Separate world labels
can remain visible, matching the screenshot. Native disassembly file hash:
`61829050e56ca180d67be592e4bfe61ec97d2e9573f720d90a54e318a7e4381d`.
Inspection evidence is in `trialbuild/prompt-diagnosis/native-methods.txt`.
No process memory was written and no native instructions were patched.

## Focused change

- `Services/GrowInteractionPromptPatch.cs`: publish validated prompts in a
  last-priority LateUpdate postfix after the native UI work.
- `Services/InteractionPromptDisplayService.cs`: set native content and make
  its container/canvas visible in this frame. Preserve any native display
  request, without leaving a custom request for the next Update. If the icon
  sprite is unavailable, include the input hint in the message.
- `Services/GrowToolFallbackService.cs`, `Services/ShapePrefabService.cs`: use
  the shared display path for grow actions and shape pickup prompts.
- `tests/InteractionPromptFrameChecks`: exercise the production display helper
  against a fixture of the observed native canvas lifecycle.
- This document, `docs/CROWN_HOVER_FIXES.md`, `docs/CODEBASE_MAP.md`: record the
  corrected behavior and verification boundary.

Tool interaction permissions, harvest/click logic, target selection, inventory,
save schemas, and multiplayer authority are unchanged. Menu/native-input and
session access guards still apply. No dependencies were added.

## Verification

- Release build: zero warnings/errors against current 0.4.7f12 references.
- Game API audit: 215 types, 649 members, 65 Harmony targets; no errors.
- Eleven prompt-frame checks pass: old invisible behavior reproduced, current
  visibility, aim-away/unequip cleanup, native request/world-label preservation,
  input/session gates, missing icons/UI, and failed-render cleanup.
- All nine accessory exception classifier checks still pass.
- `git diff --check` passes.

The native UI fixture checks frame behavior, not actual Unity rendering or live
Harmony execution. Patched in-game visual/controller and host/client tests are
still pending. The supplied screenshot is baseline evidence of the failure.

## Test and rollback

The replacement ZIP contains only `Mods/NugzzMenu.dll`, instructions, and this
build's evidence. Keep the existing S1API 3.2.1-beta.9 mod/loader pair installed.
Close the game, copy the previous menu DLL outside Mods, and replace only
NugzzMenu.dll. Do not leave a renamed duplicate DLL inside Mods.

At the pictured pots, equip trimmers and check for the left-mouse icon and
"Harvest plant". Move between pots and aim away; the prompt should follow the
current target and disappear promptly. Check watering/soil/seed/additive/misting,
switch tools, open the menu/phone/console, and verify existing native labels.
Check shape pickup too. Harvest only on a disposable/copy save if preserving
the displayed plants matters.

Rollback with the game closed by restoring the previous menu DLL. This work
does not install the replacement, edit saves, or publish a release. Production
was not touched by this fix.
