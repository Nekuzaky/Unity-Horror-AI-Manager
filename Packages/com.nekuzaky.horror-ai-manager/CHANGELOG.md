# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [0.1.0] - 2026-10-25

First release.

### Added

- **Sight:** view cone (angle, range, eye height or an Eye transform), proximity radius, up to four rays to points on
  the target's body, and a suspicion gauge that fills with the visible fraction, the light on the target, the
  distance and the target's visibility, and decays after a delay.
- **Light:** `Horror Light Source` for point, spot and directional lights, following a `Light` or set by hand. A target's
  exposure is estimated from the marked lights (distance falloff, beam cone, occlusion ray) plus an ambient level,
  ten times per second. A directional light (sun, moon) lights every target with open sky in its direction, checked
  by a ray toward the light. A flashlight with **Reveals Its Holder** makes its holder visible while it is on.
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
- **Performance:** perception time-sliced across frames, a global raycast budget per frame that is never exceeded
  and shared fairly (a deferred enemy goes first next frame), and no managed allocation per frame: the first enemy that
  registers compiles the package's code and runs every behavior path once, and Unity Events are prepared when an enemy
  is enabled. `HorrorAI.Stats.AllocatedBytes` reads Unity's profiler (`GC.Alloc` marker on the update's thread and
  `GC Allocated In Frame`). 50 NavMesh enemies cost 0.044 ms per frame on average, 0 B allocated (see Verified).
- **Editor:** plain-word inspectors with one-click fixes (bake a NavMesh, add a Horror Target to the player, use the
  Patroller preset, add a NavMesh Agent, create a patrol route, mark every light, add a collider or a Rigidbody);
  **GameObject > Horror AI** menus; Scene view gizmos (view cone, hearing radius, suspicion bar, last known position,
  search area and path, noise rings, colors per state); the Horror AI debug window under **Tools > Horror AI >
  Open Horror AI** (Ctrl+Shift+Alt+H), with **Documentation** and **GitHub Repository** next to it.
- **Integrations:** `Resonance Noise Bridge` and `Resonance Noise Map` (Resonance sound events heard by enemies,
  Resonance 0.6.0 or newer), and Hindsight bookmarks and tracked values for every enemy, each compiled only when that
  package is installed.
- **Greybox Demo** sample: a dark level with lamps, cover, clattering bottles, a creaky floor, one patrolling enemy and
  a first-person player with a flashlight and crouch. It reads the Input System when it is installed and active, the
  Input Manager otherwise, and swaps its Standard materials for the active pipeline's Lit shader under URP or HDRP.

### Fixed before release

An independent verification of the release candidate found these problems; each has a test unless stated otherwise.

- **Domain reload disabled:** the hidden runner survived leaving Play Mode and a new one was added each session, so
  the AI ran several times per frame. Entering Play Mode now destroys every old runner, a runner that is not the
  current one destroys itself instead of ticking, and the static events `StateChanged`, `NoiseEmitted`, `EnemyAdded`
  and `EnemyRemoved` are cleared. The Hindsight integration installs at `AfterAssembliesLoaded`, after that reset.
- **Allocation counter:** `HorrorAI.Stats.AllocatedBytes` used `GC.GetAllocatedBytesForCurrentThread()`, which is
  always 0 in Unity's Mono, so it could not see anything. It now reads Unity's profiler counters (bytes from
  `GC Allocated In Frame`, counted only when the `GC.Alloc` marker fired on the update's thread) and reports -1 where
  they do not exist (release players). With a real counter, the allocations it had hidden were found and removed:
  first-time code compilation and Unity Event preparation in the middle of a game, collider wrappers created by
  reading `RaycastHit.transform` (rays now compare collider ids), and the Hindsight bookmark label built at every
  change of mind (now built once per enemy). The benchmark's per-frame figure used an integer division and now reports
  the total bytes and the number of frames that allocated.
- **Eye height:** with a NavMesh Agent whose *Base Offset* lifts the pivot (enemies from **GameObject > Horror AI >
  Enemy**, the benchmark), the eyes were 1 m too high. The eye height is now measured from the feet.
- **Unreachable spots:** an enemy sent to a point off the NavMesh or behind an unreachable area claimed it had reached
  it. Destinations are snapped to the NavMesh, and an unreachable spot gives *could not reach the last known position*
  and a search around the nearest point it could reach.
- **Disabled enemies:** disabling an enemy mid-chase left Chase music playing; it now raises a change to Patrol
  (*was disabled*). Enabled again, it no longer spots the target at once from its old perception and memory.
- **Patrol:** a one-waypoint route waited forever, and waypoints added at runtime were ignored until the state changed.
- **Raycast budget:** an enemy deferred by the budget could be skipped for many frames while cheaper ones went first;
  it now goes first the next frame (a test bounds the longest wait).
- **Directional lights** linked to a `Horror Light Source` were treated as a 10 m point light.
- **Spotting times:** `perception.md` said about 4 s and 12 s to be spotted in the dark; with the default ambient light
  it is 3.2 s and 9.2 s, and the Perception Profile inspector now counts the ambient light too.
- **Editor:** percentages are formatted without the machine culture (French Windows drew a broken glyph); the state
  label above the suspicion bar sits on a dark background and the last known position cross is thicker and outlined
  (no test); the Horror AI window refreshes in Edit Mode when the scene changes (no test); every menu moved from
  **Window** to **Tools > Horror AI**; the light inspector says "Light", not "Unity Light".
- **Demo:** it works in a project that uses the Input System only (no test; checked in the development project, now
  set to the Input System only), and its materials are no longer pink under URP (no test; checked by rendering the
  demo in a temporary URP project before and after the swap).
- **Repository:** Git LFS is no longer used (`.gitattributes` and the CI checkout), and the UTF-8 byte order marks were
  removed from the workflow, `.gitignore` and `.gitattributes`.

### Verified

- 96 EditMode tests pass, and 104 with Resonance 0.6.0 and Hindsight installed.
- `Tools/Benchmark.ps1`, run twice: 50 NavMesh enemies cost 0.044 / 0.045 ms per frame on average (p95 0.058 to
  0.059 ms), and the update allocated 0 B on every one of the 3,840 frames of each run, warm-up included.
- `Tools/Build-Check.ps1`: in a Windows development player the enemy reacts to a noise and the update allocates 0 B.
