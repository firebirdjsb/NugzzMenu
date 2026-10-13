# NugzzMenu v1.0.1

October 13, 2026 · For Schedule I 0.4.7f12 · S1API 3.2.1 included

This update brings NugzzMenu up to date with the Special Customers game update
and adds rideable motorcycles, player animations, weather controls and saved
startup settings. It also addresses the reported phone, clothing, interaction
prompt, watering and police-cheat problems.

These notes cover the final changes since the published v1.0.0 release, including
the improvements made during testing. They describe changes to NugzzMenu, not
every fix made by the game itself.

## New

### Special customers in Relations

All four visiting groups and their 31 members are now listed in **Relations**.
You can filter by group and see who is currently out of town. A visitor needs
to be in town before you can change their relationship settings.

- **Hippies:** Amethyst Pidley, Bodie Hunt, Charlie Menson, Dylan Roberts,
  Jayne Lemons, Krystal Bird, Rose Welsh and Samantha Spears.
- **Party Bus:** Brad Chaddington, Chad Chaddington, Broc Evans, Dave Trotter,
  Josh Harrington, Michelle Delacroix, Stacy Sinclair, Terry Haines and Tracy Dupree.
- **Businessmen:** Bill Short, Grant Gainsworth, Hunter Yieldman, Johnny Smith,
  Micky Boone and Penny Stockman.
- **Bikers:** Bob Harley, Butch Malone, Diesel Rodd, Joey Quinn, Rick Boggers,
  Shane Mulligan, Timmy Benson and Vince Knuckles.

**Queue for tomorrow** lets solo players and multiplayer hosts choose the next
visiting group. The menu confirms the result; sleep to the next day for arrival.
Choosing another group replaces the current visiting-group schedule.

### Rideable motorcycle

The **Vehicles** tab now has **Spawn Motorcycle** and **Remove Unoccupied**.
Solo players and multiplayer hosts can spawn bikes; approved players can ride
them using the normal vehicle controls.

- The motorcycle is visible on its own, without the car body showing through.
- The rider remains visible, sits higher on the saddle and places their hands
  on the handlebars and feet on the bike.
- Steering is smoother, leaning is limited and the bike returns toward upright.
- Better grip and hill stability help prevent spinning out and flipping.
- Stronger brakes make stopping easier.
- Original chopper-style engine audio adds a deep, uneven idle, exhaust thumps
  and a sharper sound under throttle. Engine sound responds to acceleration,
  coasting, braking and reversing, with smoother starts and stops.
- Bikes have no storage and are temporary for the current session. Up to four
  can be spawned. They are not saved as owned vehicles.

### Player animations

Under **Lobby > Local Effects**, your character can now:

- Talk on the phone.
- Use a laptop.
- Sit on the ground.
- Dance.
- Pee.
- Vomit.
- Play a custom Boinking body animation.
- Rev a motorcycle.

Actions include temporary props where needed. Peeing and vomiting include their
effects and sounds; motorcycle revving includes its effects. Laptop seating and
motorcycle alignment were improved, and turning during revving turns the bike
with the player. Seated actions preserve the character's hair and appearance.

Use **Stop animation** to end an action. Actions also stop after 20 seconds or
when movement or another activity interrupts them. Matching, approved players
can use these actions as hosts or clients.

### Weather on demand

The **Time** tab now includes clear weather, fog, light rain and heavy rain/storm
presets. **Trigger Lightning** requires a second selection to confirm the strike.
Weather changes are available in solo play and to multiplayer hosts.

### Save your preferred settings

**Settings > Startup Defaults / Config** lets you save the current menu setup
as your startup defaults or load it again later.

The file is called **nuggzmenu-config.json** and lives beside NugzzMenu.dll in
the game's **Mods** folder. You can edit it with a text editor while the game is
closed, or reload your edits from Settings.

Saved preferences include the menu key, starting tab, item quantity and quality,
clothing color, selected cheats, movement, flying and camera settings. Saving
keeps a backup of the previous settings. Your game progress, money, relationships
and one-off actions such as spawning items are not stored in this settings file.
Cheats start off and clothing colors stay original unless you choose otherwise.

## Updated game content

The menu's item and people lists now refresh as game content becomes available
and support the game's new character appearance system.

- **Furniture:** Black Rubber Mats in 1x1, 2x2 and 4x4 sizes, plus the Persian Rug.
- **Glasses:** Gold Aviators, Silver Aviators, Browline Glasses and Round Tinted Glasses.
- **Hats and bandanas:** Beer Hat, Jet Helmet, Panzer Helmet, Paisley Bandana and USA Bandana.
- **Clothing:** Blue, Red, Green and Brown Ties; Leather Jacket; Aqua, Purple and
  Yellow Tie-Dye Shirts; Dirty Tank Top; Hot Shirt; Molerats Jersey; Tube Top and Mesh Top.
- **Hair:** The Businessman, Poofy Wavey and Long Wavey.
- **Facial hair:** Chevron Mustache, Swirly Mustache, Soul Patch, Biker Mustache,
  Handlebars, Mutton Chops and Full Beard.
- **Locations:** South Overpass, Suburbia, Park, Westville and Casino are added
  to teleport discovery. Business discovery includes the updated Hyland Medical.

New hairstyles and facial hair remain part of the game's character customization.
They are not turned into inventory items. **Settings > Audit 0.4.7 content
registrations** checks whether the new content is available in your loaded save.

## Improved

### Clothing colors

- Added a clearer picker with 27 named color swatches and a visible selection.
- **Native / No color** is the default. Spawned clothing keeps its original color
  until you select a tint; choosing Native restores the original colors.
- Added Gold, Silver and Bronze tone shortcuts. These are color tints, not new
  shiny metal materials.
- Added shortcuts for the actual Gold/Silver Aviators and Tie-Dye Shirts, keeping
  their original finishes and patterns. Fixed-color items retain their own colors.

### Never Wanted

- Expanded protection so police ignore the protected player more consistently,
  rather than simply clearing the wanted level.
- Clears existing police attention toward that player and prevents new police
  pursuit, searches and combat targeting while enabled.
- Available in solo play, for hosts and for approved multiplayer clients. Clients
  are no longer blocked just because they are not hosting.
- Each player's protection is separate. Turning it off restores normal police
  behavior for that player without changing other players' settings.

### Lists, prompts and transitions

- Item searches and NPC selections survive list refreshes. Visitors leaving town
  cannot silently switch a relationship edit to a different person.
- Grow and shape prompts follow the object you are currently aiming at. Shape
  pickup also checks the current target when you click.
- Temporary props, animations, bike sounds and cached selections are cleaned up
  when leaving a session or loading another save.
- Updated the included S1API to 3.2.1 and refreshed support for the game's new
  clothing, player visibility, time and NPC systems.

## Fixed

- Opening Character from the phone and then closing it could leave NugzzMenu
  inaccessible or the HUD missing. The clothing panel stays available, and the
  menu can be used over either Phone or Character.
- Phone hotkeys could open the phone while NugzzMenu was handling input. Phone
  opening and tab shortcuts are now blocked while the menu is open or closing.
- The clothing color picker could repeatedly fill the log with errors and leave
  the menu blank. Color swatches now draw without that failure, and the picker
  restores the normal screen tint after a drawing error.
- The phone/Character close path now restores its normal screen presentation
  instead of interrupting the clothing panel's transition.
- Custom left-click grow prompts could flicker or lose to the normal
  "Use trimmers to harvest" message. The current custom action is kept visible
  while valid, and normal prompts return when you look away or change tools.
- Quick planting now finishes the soil setup correctly. Quick watering reports
  failed updates instead of pretending they succeeded, and avoids watering over
  another active task. This addresses the reported quick-grow path where seeds
  needed repeated watering before progressing.
- Clearing effects could turn the player's skin white. The selected skin color
  is now restored.
- Motorcycle spawning could be wrongly blocked in solo play and could crash the
  game. Both reported paths were addressed.
- The motorcycle ride prompt now uses a stable position instead of jumping
  around. Rider visibility, saddle height, hands, feet and hill handling were
  refined during testing.
- Removed the obsolete clothing compatibility warning after the game update
  and added extra protection against trash-crown equip errors.
- Restored the original police-vehicle spawning behavior. The experimental
  police-spawning changes are not part of v1.0.1.

## Before updating

- This package targets **Schedule I 0.4.7f12** with **MelonLoader IL2CPP**. The
  matching **S1API 3.2.1** files are included.
- For the new motorcycles and animations, use **NugzzMenu v1.0.1 on the host and
  all participating clients**. The host must approve clients as before.
- Xbox/PlayStation support, keyboard/mouse controls and existing menu tabs remain.
- Preserve your existing **nuggzmenu-config.json**. The bundled example is for
  reference and is not placed over your settings.
- No save conversion is required. Close the game and back up the DLLs you replace;
  restore those backups with the game closed to return to the previous version.

## Known notes

- The sewer-office shelf trash crown has extra protection, but its original
  failure still needs a final in-game check.
- The reported occasional dark screen outside the demonstrated Phone/Character
  transition has not been reproduced. It is not claimed fixed everywhere.
- The package passed **186 automated checks**, plus build and game-compatibility
  checks. Final in-game checks of the new sounds, animations and multiplayer
  behavior are still pending; automated checks do not replace a playtest.
