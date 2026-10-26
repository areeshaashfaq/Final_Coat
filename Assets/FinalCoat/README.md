# Final Coat: M2.5 prototype scripts

Placeholder build for the M2.5 check-in. Everything you see is plain squares and circles drawn by code. No outside art, sound or AI-generated assets.

## Setup (once)

1. Drag this whole `FinalCoat` folder into your Unity project's `Assets` folder and wait for it to compile.
2. Click **Final Coat > Build Prototype Scene** (the level).
3. Click **Final Coat > Build Main Menu Scene** (the home screen). It becomes the first scene in the build.
4. Open `Assets/FinalCoat/Scenes/MainMenu` and press **Play**.

Commit everything in `Assets/FinalCoat`, including the `.meta` files. Running a builder again rebuilds that scene from scratch, so make hand edits in a copy.

## Choosing devices (Settings)

Home screen > **Settings > Who plays which toy**. Press a player's row to cycle their device:
Keyboard (W A S D), Keyboard (arrow keys), Controller 1, Controller 2.
Two players can never share a device: picking the other player's device swaps you. Choices are saved between sessions.

Settings also has: show enemy AI states, show controls in game, fullscreen.

## Controls

| | Keyboard (W A S D) | Keyboard (arrows) | Controller |
| --- | --- | --- | --- |
| Move | A / D | Left / Right | Left stick or d-pad |
| Jump | W | Up | A |
| Hold on | S | Down | B |
| Strand button: tap = tug, hold 1s = snap, tap near partner = rejoin | Left Shift | Right Shift | X |

Pause: Esc or Start. While paused or at the end: R or Y restart, M or Select main menu. F1 hides the help bar.
Menus: mouse, arrows + Enter, or stick + A (B or Esc goes back).

## What to show in the M2.5 demo

**Player movement and interactions**
- Running, jumping (with coyote time and jump buffering), landing on each other
- The paint strand: walk apart until it pulls, hang off the gap and get pulled up, swing for the large blob over the first gap
- Tug, hold on, snap and rejoin
- Pushing Crate B under the high ledge to reach the large blob
- The seesaw over the pit and the rolling ball

**Other systems (in progress)**
- Paint scoring with 4 colour stages per toy (watch the blank toys fill in)
- Wind-up toy AI with a visible state label: Patrol, Chase P1/P2, Hit, Wound down, Return. It always chases the nearest toy, so tug your rival toward it
- Getting hit sprays your paint out as blobs anyone can grab (object pool)
- Solo rules: the purple pot only works while the strand is snapped, and losses double when solo
- Checkpoint, falling and respawning, exit that only opens for joined toys, first-to-exit bonus

## Where things are

| Folder | What it does | Course requirement it shows |
| --- | --- | --- |
| `Scripts/Core` | GameManager (singleton), GameEvents (observer), input with controller support, saved settings | System design and patterns |
| `Scripts/Player` | ToyController movement, ToyPaint colour stages | Direct locomotion, code-driven animation |
| `Scripts/Strand` | PaintStrand: DistanceJoint2D, LineRenderer, tug, snap, rejoin | Rigid body physics, player dilemmas |
| `Scripts/Paint` | PaintBlob pickups, PaintBlobPool | Resource system, object pooling (memory) |
| `Scripts/Enemies` | WindUpToy and one class per AI state | State-machine AI (state pattern) |
| `Scripts/Level` | Camera, checkpoint, exit, kill zone, prototype HUD | Level flow |
| `Scripts/UI` | MainMenu: home, settings, credits | GUI menus |
| `Editor` | One-click builders for the level and the main menu | |

All paint numbers (hit losses, fall losses, stage thresholds) are on the **Game Manager** object in the Inspector, so you can tune them without touching code.

## Next steps after M2.5

- Vacuum boss (GameManager.MostPaintedToy() is already there for its targeting)
- Restyle the menus with the final art, and turn the pause overlay into a proper pause menu
- Sprite animations to replace the squash-and-stretch code
- 3 to 4 players
