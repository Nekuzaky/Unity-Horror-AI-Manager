# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [0.1.0] - 2026-10-25

First release.

### Added

- **Sight:** view cone (angle, range, eye height or an Eye transform), proximity radius, up to four rays to points on
  the target's body, and a suspicion gauge that fills with the visible fraction, the light on the target, the
  distance and the target's visibility, and decays after a delay.
- **Light:** `Horror Light Source` for point and spot lights, following a Unity `Light` or set by hand. A target's
  exposure is estimated from the marked lights (distance falloff, beam cone, occlusion ray) plus an ambient level,
  ten times per second. A flashlight with **Reveals Its Holder** makes its holder visible while it is on.
- **Crouching:** `HorrorTarget.Crouching` lowers the body points and multiplies visibility.
- **Hearing:** `HorrorAI.EmitNoise(position, loudness, source)`, damped by distance and by each wall in the way;
  `Noise Emitter` for collisions (scaled by impact speed), triggers, start, enable, Unity Events and animation events.
  Hearing alone is capped below a chase.
- **Memory:** last known position, time, certainty and source, decaying over time.
- **Behavior:** a deterministic state machine, Patrol, Alert, Investigate, Search, Chase, Lost Track, with every
  threshold, duration, speed, search radius and catch distance in a `Behavior Profile`, and a seeded search pattern.
- **Events:** On Spotted, On Chase Started, On Caught, On Lost Track, On Search Started, On Gave Up, On Alerted,
  On Investigate, On Noise Heard, On State Changed, and the C# events `HorrorEnemy.StateChanged`, `Caught`,
  `NoiseHeard` and `HorrorAI.StateChanged`. Every transition carries a reason explained in plain words.
- **Data:** `Perception Profile`, `Behavior Profile` and `Horror Archetype` assets, and the **Patroller** preset.
- **Patrol routes:** waypoints with waits, loop, ping-pong or once.
- **Scripting:** `HorrorEnemy.Investigate(position)`, `Calm()`, `SetMover(IHorrorMover)`; `HorrorAI.Stats`,
  `RaycastBudget`, `AmbientLight`, `GetExposure`, `CopyRecentNoises`.
- **Performance:** perception time-sliced across frames, a global raycast budget per frame that is never exceeded,
  and zero managed allocation per frame once warmed up. 50 enemies cost 0.045 ms per frame on average.
- **Editor:** plain-word inspectors with one-click fixes (bake a NavMesh, add a Horror Target to the player, use the
  Patroller preset, add a NavMesh Agent, create a patrol route, mark every light, add a collider or a Rigidbody);
  **GameObject > Horror AI** menus; Scene view gizmos (view cone, hearing radius, suspicion bar, last known position,
  search area and path, noise rings, colors per state); the **Window > Horror AI** debug window.
- **Integrations:** `Resonance Noise Bridge` and `Resonance Noise Map` (Resonance sound events heard by enemies), and
  Hindsight bookmarks and tracked values for every enemy, each compiled only when that package is installed.
- **Greybox Demo** sample: a dark level with lamps, cover, clattering bottles, a creaky floor, one patrolling enemy and
  a first-person player with a flashlight and crouch.