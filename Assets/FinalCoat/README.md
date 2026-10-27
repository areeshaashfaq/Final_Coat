# Final Coat: M2.5 prototype scripts

Placeholder build for the M2.5 check-in. Everything you see is plain squares and circles drawn by code. No outside art, sound or AI-generated assets.

## Setup (once)

1. Open the project in Unity 6.3 (6000.3.22f1) and wait for it to compile.
2. Open `Assets/FinalCoat/Scenes/Prototype` and press **Play**.
3. If the scene is missing, click **Final Coat > Build Prototype Scene** first.

Commit everything in `Assets/FinalCoat`, including the `.meta` files. Running the builder again rebuilds the scene from scratch, so make hand edits in a copy.

## Controls

Both players share one keyboard.

| | Player 1 | Player 2 |
| --- | --- | --- |
| Move | A / D | Left / Right |
| Jump | W | Up |
| Hold on | S | Down |
| Strand button: tap = tug, hold 1s = snap, tap near partner = rejoin | Left Shift | Right Shift |

Esc pauses. While paused or at the end, R restarts. F1 hides the help bar.

## What to show in the M2.5 demo

**Player movement and interactions**
- Running, jumping, landing on each other
- The paint strand: walk apart until it pulls, hang off the gap and get pulled up, swing for the large blob over the first gap
- Tug, hold on, snap and rejoin
- Pushing Crate B under the high ledge to reach the large blob
- The seesaw over the pit and the rolling ball

**Other systems (in progress)**
- Paint scoring with 4 colour stages per toy (watch the blank toys fill in)
- Wind-up toy AI with a visible state label: Patrol, Chase P1/P2, Hit, Wound down, Return. It always chases the nearest toy, so tug your rival toward it
- Getting hit costs paint
- Solo rules: the purple pot only works while the strand is snapped, and losses double when solo
- Checkpoint, falling and respawning, exit that only opens for joined toys, first-to-exit bonus

## Where things are

| Folder | What it does | Course requirement it shows |
| --- | --- | --- |
| `Scripts/Core` | GameManager (singleton), GameEvents (observer), keyboard input | System design and patterns |
| `Scripts/Player` | ToyController movement, ToyPaint colour stages | Direct locomotion |
| `Scripts/Strand` | PaintStrand: DistanceJoint2D, LineRenderer, tug, snap, rejoin | Rigid body physics, player dilemmas |
| `Scripts/Paint` | PaintBlob pickups | Resource system |
| `Scripts/Enemies` | WindUpToy and one class per AI state | State-machine AI (state pattern) |
| `Scripts/Level` | Camera, checkpoint, exit, kill zone, prototype HUD | Level flow |
| `Editor` | One-click builder for the prototype level | |

All paint numbers (hit losses, fall losses, stage thresholds) are on the **Game Manager** object in the Inspector, so you can tune them without touching code.

## Next steps after M2.5

- Vacuum boss (GameManager.MostPaintedToy() is already there for its targeting)
- Main menu, settings and credits screens
- Controller support
- Proper pause menu
- Dropped paint when hit, using an object pool
- Jump feel: coyote time, jump buffering, squash and stretch
- Tune the seesaw, ball and crate physics
- Sprite animations
- 3 to 4 players
