# Components

## Horror Enemy

The monster. Needs a `NavMesh Agent` to walk around obstacles; without one it moves in straight lines (useful for
ghosts). Perception and behavior come from its **Archetype**.

| Field | Meaning |
|---|---|
| Archetype | A `Horror Archetype` asset: how it perceives and behaves. Without one, built-in defaults are used. |
| Patrol Route | The route it walks while calm. Without one it guards the spot where it starts. |
| Eye (optional) | Sight starts here and follows this transform's forward. Otherwise the pivot plus the eye height. |
| Search Seed | Seed of the search pattern, so a playthrough is repeatable. |
| Events | Unity Events for every change of mind: see [Behavior](behavior.md#events). |

## Horror Target

What enemies look for, usually the player.

| Field | Meaning |
|---|---|
| Crouching | Lowers the body points to *Crouch Height* (low cover hides you) and multiplies visibility by *Crouch Visibility*. |
| Visibility | Multiplies how fast enemies notice the target. 0 makes it invisible (a hiding spot), 2 twice as easy to notice. |
| Body Points | Up to four points, relative to the pivot, that enemies cast sight rays to. Defaults: feet, chest, head. |
| Light Blockers | Layers that block light reaching the target. |

`Exposure` (0 to 1) tells how lit the target is right now.

## Horror Light Source

Tells the AI that a light reveals what it shines on.

| Field | Meaning |
|---|---|
| Unity Light / Follow Its Settings | Read range, spot angle, shape and on/off from that `Light`. |
| Shape, Range, Spot Angle | Used when no Light is followed. |
| Reveal Strength | How lit a target right next to the light is (1 = fully). Independent of the rendered intensity. |
| On | Turn it off with `SetOn(false)` or `Toggle()`; a followed Light is enabled and disabled with it. |
| Reveals Its Holder / Holder Exposure | For a flashlight or lantern under a `Horror Target`: while on, the holder's exposure is at least this value. |

## Noise Emitter

Makes a noise enemies can hear.

| Field | Meaning |
|---|---|
| Loudness (m) | How far the noise carries in open space for an enemy with a hearing multiplier of 1. |
| Make Noise When | Collision Enter, Trigger Enter, Start, Enable, or none (then call `Emit()`). |
| Min / Full Impact Speed | Softer impacts are silent; between the two, the noise scales from 35 % to 100 %. |
| Cooldown (s) | Minimum time between two noises. |
| Only For Tag | Only react to triggers and collisions with objects that have this tag. |

From a Unity Event or an animation event, call `Emit()`, `EmitLoudness(float)` or `EmitScaled(float)`.

## Patrol Route

Waypoints are child transforms, each with a wait in seconds. **Mode**: *Loop* goes back to the first waypoint,
*Ping Pong* walks back and forth, *Once* stops at the last. The inspector warns about waypoints off the NavMesh.