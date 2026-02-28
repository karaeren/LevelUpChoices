# LevelUpChoices

![Demo - Item Select](https://raw.githubusercontent.com/karaeren/LevelUpChoices/refs/heads/main/other/demo_item_select.png)

![Demo - Artifact](https://raw.githubusercontent.com/karaeren/LevelUpChoices/refs/heads/main/other/demo_artifact.png)

![Demo - Config](https://raw.githubusercontent.com/karaeren/LevelUpChoices/refs/heads/main/other/demo_config.png)

This mod removes most of the normal item sources from stages — chests, shrines, 3D printers, scrappers, cleansing pools, lunar pods — and replaces them with a level-up item selection system.

Every time the team levels up, each player gets to pick from 3 items. Equipment barrels and Scavenger's Sacks still spawn as normal.

Almost anything such as disabling interactables, exp multipliers, rates of items are configurable in the game. It fully supports items added by 3rd party mods. LookingGlass mod is highly recommended!

Contact me at Discord (@erenkara) for any bugs, suggestions etc...

## How it works

**Pick** — Choose one of the 3 items shown. Picking resets your reroll token.

**Reroll** — Swap one item slot for a newly rolled option. You get 1 reroll token that resets each time you pick.

**Banish** — Permanently remove an item from your personal pool so it won't show up in future rolls. You start with 1 banish token and gain another every 10 levels.

Press **F3** at any time to open or close the selection menu.

The items are chosen randomly based on the item tiers and your level. You can get lucky and proc a "synergy" item which will be synergistic to the items you own.

## Levels and XP

You can configure the max level. By increasing the max level from 94, you can make each level easier or harder depending on the new max level.

## Mod Support

This mod fully supports items added by other mods. No configuration or tinkering needed! Some mod items are situationally disabled if they are unobtainable or if they are tied to interactables that are disabled.

If you think a certain mod item should be disabled or enabled, please open an issue on GitHub.

## Multiplayer

Each player has fully independent state — their own item options, their own banish list, their own tokens. Everything runs server-side, so the host has to have the mod. Clients need it installed too so they can select their items.

## TODO

- Everything is done! (for now)

## Changelog

### 1.1.2

- Refactored UI and some parts of the codebase.
- Added an artifact for the mod. Now you need to choose the artifact in-game to enable the mod. Or you can use the "Always Enable" config to use it like before.
- Added luck calculation. Your positive or negative luck determines how many rolls each item slot will get and depending on the luck value, it will pick the best or worst items for you.
- Added item similarity and synergy system. Items that are similar and synergistic to the items you own will have a higher chance of appearing in the item selection UI. Chances are configurable.
- Added dotnet tooling and refactored some code.
- Added max level configuration. Now instead of increasing the XP scaling, we increase the max level thus making each level easier or harder depending on the new max level.

<details>
<summary>Previous versions</summary>

## 1.1.1

- Fixed a bug where clients on a multiplayer game would not get any item rolls.

## 1.1.0

- Fixed a bug where some items weren't being disabled even though "remove interractables" option is set.
## 1.0.9

- Removed all types of "Scrap"s from the drop table, unless the player is using the Drifter character.
- Disabled items that are unobtainable. Previously if you didn't have a DLC, you still saw the item in the item selection UI but when you clicked it, you would lose your token and not get any items. This is automatically detected when a player joins.
- Fixed a bug where you would only get 1 experience if you set the exp multiplier configuration high and you were at 80+ levels.
- Removed "Artifact Key" from the drop table.
- Removed "Chance Doll", "Executive Card", "Sale Star", "Hallowed Ichor", "Sequenced Fate", "Universal VIP Pass", "Primal Birthright" from the drop table when interactable removal is enabled.

## 1.0.8

- Added a way to pause the game when item selection UI is displayed. Works in both singleplayer and multiplayer but when creating a multiplayer game, the host must enable multiplayer pause.

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
</details>