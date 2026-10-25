# Perception

Each enemy perceives ten times per second by default (*Update Interval*), spread over frames.

## Sight and the suspicion gauge

A target is a candidate when its center is inside the view cone (*View Angle*, *View Range*, from the eye), or
within *Proximity Radius* in any direction. The enemy then casts a ray to each body point of the target: the
**visible fraction** is the share of points not blocked by an occluder.

While the target is visible, suspicion rises by:

```
Sight Gain × visible fraction × light factor × distance factor × target visibility   (per second)
```

- **Light factor** goes from *Dark Gain* (target in total darkness) to 1 (fully lit), following the target's exposure.
- **Distance factor** is 1 up to *Close Range*, then falls to *Far Gain* at the edge of the view range.
- **Target visibility** is the target's *Visibility*, times *Crouch Visibility* when crouching.

With the Patroller values and the default ambient light (0.04), a standing player in plain view is spotted in 0.5 s
when lit and close, about 3.2 s in the dark close by, and about 9.2 s in the dark at the edge of the range. The
Perception Profile inspector computes these times with the current ambient light. Once the target is out of sight, the gauge holds
for *Decay Delay* seconds, then loses *Suspicion Decay* per second.

### Detection times

How long a Patroller needs to spot a fully visible target (every body point in view), with the default ambient light.
Half hidden behind cover, the times double.

| Light on the target | Standing, 3 m | Standing, 10 m | Standing, 18 m | Crouched, 3 m | Crouched, 10 m | Crouched, 18 m |
|---|---:|---:|---:|---:|---:|---:|
| Lit (exposure 100%) | 0.5 s | 0.7 s | 1.4 s | 1.0 s | 1.4 s | 2.9 s |
| Dim (exposure 30%) | 1.3 s | 1.9 s | 3.7 s | 2.6 s | 3.7 s | 7.4 s |
| Dark (ambient 4%) | 3.2 s | 4.6 s | 9.2 s | 6.4 s | 9.2 s | 18.4 s |

The dashboard shows the same number live for the picked enemy (**Gauge full in**), and the **Archetypes** tab and the
Perception Profile inspector compute it for each profile.

### Tuning light and crouching

- **Darkness should feel safe, not invisible.** *Dark Gain* sets how much the dark slows the gauge: 0.12 makes it about
  eight times slower than under a lamp. Lower it for a game about hiding, raise it toward 0.4 for a hunter that sees
  well at night.
- **Crouching** halves the speed (*Crouch Visibility* on the target) and lowers the body points (*Crouch Height*), so
  low cover hides the whole body. For a stealth game where crouching is the main tool, try 0.3.
- **A flashlight is a choice.** With **Reveals Its Holder**, the holder counts as lit to at least *Holder Exposure*
  (0.7) while it is on: the dark row of the table becomes almost the lit one.
- **Ambient light** (`HorrorAI.AmbientLight`) is the floor of every exposure: raise it for a moonlit level instead of
  marking every window.
- **Far Gain** makes distant targets slower to notice; *Close Range* is where distance stops mattering.

The gauge drives the [behavior](behavior.md): above *Alert Threshold* the enemy stops and stares, at 100 % while
seeing the target it **spots** it and chases.

## Light exposure

Each target's exposure (0 to 1) is recomputed ten times per second from the `Horror Light Source` components that
are on:

```
exposure = ambient + Σ Reveal Strength × falloff(distance, range) × cone(angle, spot angle)
```

`falloff` is 1 at the light and 0 at its range (smooth, like Unity's lights), `cone` is 1 inside the beam and fades
to 0 at its edge. A ray from the light to the target's center checks that no wall is in between. Ambient light is
`HorrorAI.AmbientLight` (0.04 by default). A light under the target with **Reveals Its Holder** sets the exposure to
at least its *Holder Exposure* while it is on, without a ray. A **directional** light adds its *Reveal Strength* (no
falloff, no cone) when a 200 m ray from the target toward the light is not blocked.

The estimate does not read the rendered image: what you see on screen and what the AI considers lit only match if
the lamps that matter carry a `Horror Light Source`.

## Crouching

`HorrorTarget.Crouching` scales the height of the body points by *Crouch Height* (0.55): a crate one meter high now
hides the whole body, where it only hid the feet of a standing player. Visibility is also multiplied by
*Crouch Visibility* (0.5).

## Hearing

`HorrorAI.EmitNoise(position, loudness, source)` adds a noise to a short history (64 noises). At its next perception,
each enemy checks the noises it has not heard yet (four at most per perception):

```
level = (1 − distance / (loudness × Hearing Multiplier)) × Wall Damping ^ walls
```

`walls` is the number of occluders crossed by a ray from the noise to the enemy's ears, not counting the enemy's own
colliders, the source's colliders when the source is a `Horror Target` or a `Horror Enemy`, or anything within 25 cm
of the noise (the body of the object that made it). The eyes and ears are at the eye height above the feet. A level above
*Hearing Threshold* is heard: suspicion rises by `level × Hearing Gain`, never above *Hearing Cap* (hearing alone
makes the enemy investigate, never chase), and the noise position becomes the last known position (unless the enemy
saw the target less than 1.5 s ago). Enemies ignore noises whose source is themselves.

## Memory

`HorrorEnemy.Memory` holds the last known **Position**, the **Time** it was updated, the **Certainty** (0 to 1) and
the **Source** (Sight, Hearing or Scripted). Seeing the target updates it every perception with the current
suspicion as certainty; hearing updates it with the noise level. Certainty decays by *Memory Decay* per second.
Investigate, Lost Track and Search all head for the last known position.