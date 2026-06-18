# NugzzMenu v0.9.6

This release tightens up the property worker controls and polishes the
management clipboard camera behavior from v0.9.5.

## Worker Property Rules

- Worker controls are now blocked for any owned property with `0/0` worker
  capacity.
- This covers locations like Motel Room, Sewer Office, Laundromat, the RV, and
  any other property the game marks as not supporting workers.
- Existing workers can no longer be moved to unsupported properties.
- New workers can no longer be hired for unsupported properties.
- The Properties tab now shows a clear unsupported-worker message instead of
  hire or move controls for those locations.

## Management Clipboard Camera

- If third person is active and the management clipboard is equipped/opened,
  Nugzz now exits third person through the normal camera toggle-off path.
- This should restore first-person visuals properly instead of leaving the
  player pawn's head, eyes, body, or arms visible.
- Third-person toggles remain blocked while the clipboard is equipped so the
  custom camera does not interfere with management targeting.

## Notes

- This builds directly on the v0.9.5 management clipboard fix.
- No large assets or embedded files were added.
