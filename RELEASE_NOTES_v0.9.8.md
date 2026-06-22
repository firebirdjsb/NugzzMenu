# NugzzMenu v0.9.8

This update focuses on save tools, cleaner Settings behavior, vehicle polish,
and making the project easier for other coders to maintain.

## New

- Added a main-menu Save Manager in the Settings tab.
- Save slots can now be inspected from the menu.
- Existing save slots can be backed up.
- Save deletion is handled as an archive move into the save profile's Backups
  folder instead of a permanent delete.
- Added tutorial flag editing for saves that need to replay or skip the opening
  tutorial.
- Added local Steam Cloud marker handling for the active save profile, with
  clear messaging that full Steam Cloud control still belongs in Steam.
- Added RV controls under Properties for blowing up, fixing, and respawning the
  RV.
- Added Benzie Manor access under the Properties tab.
- Added current-vehicle tuning controls for driven vehicles.

## Improved

- Settings now participates in the dynamic window sizing system.
- Large Settings panels now stay reachable with a safer scroll path.
- Vehicle menu camera handling is more stable when opening and closing the menu
  while driving.
- Vehicle spawning has broader support for special vehicles, including police
  vehicle handling work.
- Properties worker controls continue to block unsupported `0/0` worker
  locations.
- The codebase now includes contributor documentation, architecture notes, a
  codebase map, a feature checklist, release process notes, and editor formatting
  rules.

## Fixed

- Fixed Settings save tools being pushed off-screen.
- Fixed save slot details and archive-delete controls not being reachable in the
  Settings tab.
- Reworked the save editor to avoid IL2CPP-unfriendly JSON and UI paths.
- Reduced repeated missing-variable warning spam for legacy game variables such
  as `cash_balance`, `total_money`, and `player_in_vehicle`.
- Preserved the vehicle-camera fix so closing the menu while driving does not
  detach or wobble the camera.

## Notes

- Save deletion is intentionally recoverable. The slot folder is archived under
  Backups instead of being permanently removed.
- Save editing is intended for the main menu only.
- No large assets were added.
