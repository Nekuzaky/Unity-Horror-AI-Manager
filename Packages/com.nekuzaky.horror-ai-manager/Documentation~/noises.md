# Noises

Enemies hear what you tell them is a noise: footsteps, a bottle that falls, a door that slams, a radio. Nothing is
read from the audio mixer, so a noise can be silent for the player and loud for the AI, or the other way around.

## Making a noise

From code, anywhere:

```csharp
HorrorAI.EmitNoise(position, loudness, gameObject);
```

- **Loudness** is in meters: how far the noise carries in open space for an enemy with a *Hearing Multiplier* of 1.
  Suggested values: 1 to 1.5 for a crouched step, 4 for a walking step, 10 for running, 12 to 15 for a falling bottle
  or a slammed door.
- **Source** is optional. When it is the player (a `Horror Target`) or an enemy, its own colliders are ignored when
  counting walls, and an enemy never hears its own noises.

Without code, put a **Noise Emitter** on props: it makes its noise on a collision (scaled by the impact speed, so a
gentle touch is silent), when something enters its trigger, on start or enable, or when an inspector event or an
animation event calls `Emit()`, `EmitLoudness(float)` or `EmitScaled(float)`. Its inspector warns when it needs a
collider, a trigger or a Rigidbody. Resonance users can let enemies hear their sound events instead: see
[Integrations](integrations.md).

## What an enemy hears

At each perception (ten times a second), an enemy checks up to four noises it has not heard yet:

```
level = (1 - distance / (loudness x Hearing Multiplier)) x Wall Damping ^ walls
```

Each wall between the noise and the enemy's ears multiplies the level by *Wall Damping* (0.45 for the Patroller). A
level above *Hearing Threshold* is heard: the gauge rises by `level x Hearing Gain`, never above *Hearing Cap*, and the
noise becomes the last known position. Hearing alone makes an enemy investigate, never chase.

| Patroller, loudness 6 (a footstep) | Heard up to |
|---|---|
| In the open | 5.7 m |
| Through one wall | 5.3 m |
| Through two walls | about 4 m |

## Seeing the noises

In the Scene view, every noise in Play Mode draws a ring that grows to its loudness. The dashboard's **Noises** tab lists
the last 64 with their source, loudness and position, and which enemies heard each one, with the level left after
distance and walls. Click one to show it in the Scene view.

![The Noises tab](../../../docs/images/dashboard-noises.png)

`HorrorAI.CopyRecentNoises(buffer)` gives the same history to your own code, and `HorrorAI.NoiseEmitted` and
`HorrorEnemy.NoiseHeard` are raised for each noise.
