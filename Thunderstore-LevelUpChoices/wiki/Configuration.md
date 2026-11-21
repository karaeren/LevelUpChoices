# Configuration Guide

LevelUpChoices is highly configurable. All settings are available through the Risk Of Options menu in the game.

## Shared Settings
Stuff that affects both clientside and serverside gameplay.

- **Always Enable Mod**: When true, the mod remains active even if you don't use the artifact. Also works with game modes that don't support artifacts.
- **Pause On Item Select**: Pauses the game when item selection is open on any client. In multiplayer, you have to enable the "Multiplayer Pause" option when creating the lobby otherwise it will not work.

## Client Settings
These settings are personal and only affect your local game.

- **Toggle Menu Key**: The keyboard shortcut used to open and close the selection menu. The default is F3.
- **UI Scale**: UI size multiplier. Zooms in or out the UI.
- **Show Item Descriptions**: Toggles the item descriptions on item cards.
- **Enable Notifications**: Displays a notification bar when you have item choices available.

## Server Settings
These settings must be configured by the host and affect all players in a session.

- **Remove Chests & Interactables**: When enabled, the mod removes most item-giving objects from stages.
- **Interactable Budget Multiplier** changes how many interactables can spawn. It starts at `0.9`. Set it to `1.0` to use the usual budget. Changes kick in on the next stage.
- **Item Blacklist** lets the host search for and block items. The list applies on the next run, both to the picker and to items monsters get from the Artifact of Equality.
- **Enable Equipment Choices** gives everyone equipment choices on their 10th round. Equipment barrels and shops are removed when this and interactable removal are both on.
- **Equipment Round Chance** is the chance to see equipment after round 10. It starts at 12%, with at least one item round in between.
- **Enable Level System**: Toggles the custom leveling system and the extended level cap.
- **Max Level**: Sets the maximum level achievable (default 256, up to 9999).
- **Enable Monster Level Scaling**: Toggles monster level scaling. If you set the maximum level to 256, monsters level will be scaled by (256/94)=2.72. If max level is 940 the monsters levels will be multiplied by 10, etc...
- **Item Choices**: The number of items presented to each player upon leveling. It's recommended to not change this from 3.
- **Levels Per Banish Token**: The number of levels required to earn a new Banish token.
- **Starting Banish/Reroll Tokens**: The number of tokens each player receives at the start of a run.
- **Reroll Token Refresh On Pick**: Toggles whether players receive a new Reroll token after selecting an item.

### Tier Weight Settings
You can set different tier weights for early and late game. The odds change as you make more picks, until you hit the late-game weights at 36 picks. The default values should be good enough.

### Similarity Settings
Each player can proc a "synergetic" roll where it replaces the normally rolled item with a same tier item that is similar to the players most used items. This way it's easier to stack the same item or items that are synergistic like different types of attack speed items.

- **Similar Item Chance**: The percentage chance for a roll to be replaced with a similar item.
- **Similar Item Count**: Used when calculating the similarity map of items. You probably shouldn't change this.
- **Similarity Threshold**: Internal number used when calculating the distance between two items. You probably shouldn't change this.
