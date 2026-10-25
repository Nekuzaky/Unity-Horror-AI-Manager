# Limitations and troubleshooting

## Limitations

- **Light is estimated, not rendered.** Baked lights, emissive materials and light probes do not count unless a
  `Horror Light Source` stands for them. Directional lights are not supported: raise `HorrorAI.AmbientLight` for
  moonlight or a lit exterior.
- **One gauge per enemy.** An enemy watches the four first targets and keeps one suspicion gauge for the most visible.
  Made for single-player games.
- **Straight-line hearing.** Noise is damped per wall crossed, it does not travel through doors and corridors.
- **NavMesh only.** Enemies use a NavMesh Agent, or move in straight lines without one. Off-mesh links work as
  Unity handles them; there is no climbing, door opening or hiding-spot logic yet.
- **Fixed capacities.** 64 recent noises, four noises per perception, four body points, eight light rays per target.

## The enemy never sees me

- Is there a `Horror Target` on the player? The enemy inspector says so.
- Is the player inside the view cone? Select the enemy: the cone is drawn in the Scene view.
- Does a collider sit between the eye and the body points (a glass pane, a trigger without *Ignore Raycast*,
  the enemy's own weapon)? Set *Occluders* in the Perception Profile to the layers that should block sight.
- In the dark, far away and crouching, it can take a long time: the Perception Profile inspector tells how long.

## The enemy sees me through the darkness instantly

- A light reveals you: select your lights and check their range. The flashlight's **Reveals Its Holder** makes you
  visible from afar while it is on.
- Lower *Dark Gain* or *Sight Gain* in the Perception Profile.

## The enemy does not move

- *There is no NavMesh under this enemy*: bake one (the inspector has a button).
- The patrol route needs two waypoints, all on the NavMesh (the route inspector checks).

## It never hears me

- Is the noise loud enough? Loudness is in meters: 4 for a footstep, 10 for running, 15 for a dropped object.
- Each wall multiplies the level by *Wall Damping* (0.45).
- Noises whose source is the enemy itself are ignored.

## The Horror AI window is empty

It lists running enemies in Play Mode. Enemies register when enabled; a disabled object is not listed.