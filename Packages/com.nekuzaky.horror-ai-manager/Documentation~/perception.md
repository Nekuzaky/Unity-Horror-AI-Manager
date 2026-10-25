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

With the Patroller values, a standing player in plain view is spotted in 0.5 s when lit and close, about 4 s in the
dark close by, and about 12 s in the dark at the edge of the range. Once the target is out of sight, the gauge holds
for *Decay Delay* seconds, then loses *Suspicion Decay* per second.

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
at least its *Holder Exposure* while it is on, without a ray.

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

`walls` is the number of occluders crossed by a ray from the noise to the enemy's ears. A level above
*Hearing Threshold* is heard: suspicion rises by `level × Hearing Gain`, never above *Hearing Cap* (hearing alone
makes the enemy investigate, never chase), and the noise position becomes the last known position (unless the enemy
saw the target less than 1.5 s ago). Enemies ignore noises whose source is themselves.

## Memory

`HorrorEnemy.Memory` holds the last known **Position**, the **Time** it was updated, the **Certainty** (0 to 1) and
the **Source** (Sight, Hearing or Scripted). Seeing the target updates it every perception with the current
suspicion as certainty; hearing updates it with the noise level. Certainty decays by *Memory Decay* per second.
Investigate, Lost Track and Search all head for the last known position.