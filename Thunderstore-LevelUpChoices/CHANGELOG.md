## 1.0.7

- Fixed a bug where you didn't get correct amoutn of item selection tokens when levelling up multiple times.
- Added support for LookingGlass. It's not a required dependency but when installed alongside this mod, it will enhance the descriptions of items.
- Improved item selection UI.

## 1.0.6

- Added configuration and RiskOfOptions support. Available configurations;
1. Shared: Toggling mod to completely disable it.
2. Client: Changing settings such as shortcut key for the item selection UI, UI scale etc...
3. Server: Toggling disabling of interactables, the number of item options serverd to players when they level up, the rates for the items etc...

## 1.0.5

- Updated README

## 1.0.4

- Updated experience scaling logic. It's a bit harder than the previous logic after level 30 but still not as bad as the game's original logic.
- Updated drop table logic. Now seeing Tier 3's shouldn't be too common in early game. Additionally memory and CPU usage is optimized for the host.
- Added new icon
- Added back normal barrels to reduce the spawn rate of equipment barrels.
- Updated design of the item select menu.
- Split code for readability.

## 1.0.3

- Fixed a bug with Banish/Reroll buttons overlapping.
- Released the code on GitHub

## 1.0.2

- You can now select the entire item card to select that, instead of just being able to select an item by clicking its image.
- Fixed user being able to open item select UI without having a select token or being in the lobby.
- Fixed cursor/camera lock bug after selecting an item.
- Added demo image to README

## 1.0.1

- Updated README

## 1.0.0

- First release