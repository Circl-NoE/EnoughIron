# Enough Iron!
Allows metal items that were stuck with the appearance of iron to be any metal
Must be on client and server
## What does this mod actually do?
Adds the component that any texturable item uses to set its material to the iron items, as well as adding the metal material distribution to them so that they can spawn as different materials in the mines
## What items are included right now?
- All metal handles (Highly recommend using [MetalHandleMoulds](https://github.com/CG-Nik/MetalHandleMoulds) to get the best out of this)
- Large and small metal shields
- The stock Metal Bow

## Metal bows

Cave loot uses the existing metal distribution. Plain spawns start in Iron, and the game saves the bow's material.

Arrow impact damage and draw response both use the bow metal's damage stat relative to Iron. Iron matches the stock bow. Copper has 0.7× the damage and draw response of Iron, while Mythril has 2.5×. These comparisons use the same arrow and draw strength.

Use the usual server spawn command to select a metal. Replace `YourName` with the player name.

```text
spawn YourName 25100 copper
spawn YourName 25100 mythril
```
