# LevelUpChoices

![Demo image](https://raw.githubusercontent.com/karaeren/LevelUpChoices/refs/heads/main/Thunderstore-LevelUpChoices/demo.png)

This mod removes most of the normal item sources from stages — chests, shrines, 3D printers, scrappers, cleansing pools, lunar pods — and replaces them with a level-up item selection system.

Every time the team levels up, each player gets to pick from 3 items. Equipment barrels and Scavenger's Sacks still spawn as normal.

---

## How it works

**Pick** — Choose one of the 3 items shown. Picking resets your reroll token.

**Reroll** — Swap one item slot for a newly rolled option. You get 1 reroll token that resets each time you pick.

**Banish** — Permanently remove an item from your personal pool so it won't show up in future rolls. You start with 1 banish token and gain another every 10 levels.

Press **F3** at any time to open or close the selection menu.

---

## Item weights

Early levels skew heavily toward white items. As you pick more items the odds gradually shift toward greens, reds, boss drops, and lunars. The starting split is roughly 75/16/6/1/2 (white/green/red/boss/lunar) and drifts toward higher tiers the more you've already picked.

Rusted Keys are excluded from the pool since crate spawns are removed.

Note: This system will be changed in the future.

---

## XP behavior

From level 10 onward the mod caps the gap to the next level at 2000 XP, which keeps leveling from slowing down too much in the later stages of a run. You can now reach level 94!

---

## Multiplayer

Each player has fully independent state — their own item options, their own banish list, their own tokens. Everything runs server-side, so the host has to have the mod. Clients need it installed too so they can select their items.