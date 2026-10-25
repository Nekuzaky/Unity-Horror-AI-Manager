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
  `GC Allocated In Frame`). 50 NavMesh enemies cost 0.044 to 0.050 ms per frame on average over three runs, 0 B
  allocated (see Verified).
- **Editor:** plain-word inspectors with one-click fixes (bake a NavMesh, add a Horror Target to the player, use the
  Patroller preset, add a NavMesh Agent, create a patrol route, mark every light, add a collider or a Rigidbody);
  **GameObject > Horror AI** menus; Scene view gizmos (view cone, hearing radius, suspicion bar, last known position,
  search area and path, noise rings, colors per state); **Documentation** and **GitHub Repository** under
  **Tools > Horror AI**.
- **Dashboard:** **Tools > Horror AI > Dashboard** (Ctrl+Shift+Alt+H), a UI Toolkit window for the dark and light
  editor skins that replaces the first debug window. **Overview**: enemies by state, highest suspicion, AI cost per
  frame (average and 95th percentile), rays against the budget, perceptions, measured garbage (*n/a* where it cannot
  be measured) and graphs of the last 30 seconds. **Enemies**: one row per enemy with its state, suspicion, target,
  distance and last change in plain words; a click selects and frames it; a panel with what it perceives (sight, light
  on the target, crouching, hearing), what it remembers, its suspicion graph and its last 16 changes of mind.
  **Noises**: the last 64 noises with their source, loudness, position and who heard them after the walls; a click
  shows the noise in the Scene view. **Setup**: scene checks with one-click fixes (NavMesh, target, lights, archetypes,
  agents, eyes, patrol routes) and notes on the pipeline, input and ray budget. **Archetypes**: every archetype and
  profile with its summary in plain words, and new ones from the Patroller preset. Pause, follow the selection and a
  link to its documentation in the header.
- **Zero cost from the dashboard:** the runtime reaches it through three calls marked
  `[Conditional("UNITY_EDITOR")]`, removed from players by the compiler (the player's runtime assembly holds none of
  them); in the Editor with the window closed each is a null check. While it is open it records into preallocated
  buffers without allocating, refreshes the visible tab ten times a second, reuses its list rows and cached strings,
  and allocates nothing when nothing changed (tested). Benchmark with it: 0.044 to 0.050 ms per frame for 50 enemies
  over three runs, 0 B.
- **Icons:** a rounded colored tile with a white glyph for Horror Enemy, Horror Target, Horror Light Source, Noise
  Emitter, Patrol Route, Horror Archetype, Perception Profile, Behavior Profile, the Resonance Noise Map and Bridge,
  the Hindsight probe and the dashboard, shown in the Inspector, the Project window and the Add Component menu, and
  SVG glyphs for the dashboard tabs and states. Rendered by `docs/brand/render_icons.py` from Bootstrap Icons (MIT,
  see Third Party Notices) and three original glyphs.
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
- **Mark every light** also marked the glow carried by an enemy (the demo enemy's eye), which would have revealed
  targets around the enemy; lights under a `Horror Enemy` are now skipped and not reported as unmarked (no test).
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

A second verification, of the dashboard, the icons and the media, found these; each has a test unless stated otherwise.

- **Garbage shown in the Editor:** the 616 to 684 B the dashboard showed on a change of mind were wrongly put down to
  editor work at the first change. The real cause: while an Inspector shows a `Horror Enemy` whose Unity Events have
  persistent listeners, Unity rebuilds those listeners by reflection (`UnityEventBase.RebuildPersistentCallsIfNeeded`)
  at every change of mind of that enemy. It is Unity's editor-only work, not the dashboard's, and players allocate 0 B.
  The dashboard page and the FAQ now say so (no test; it cannot be avoided from the package without bypassing Unity).
- **Noises tab:** with many enemies the 256 last hearings were overwritten within seconds and noises said *nobody*.
  The dashboard now keeps, for each of the 64 listed noises, how many enemies heard it, the six loudest by name and how
  many found it too faint, in buffers allocated once (tested with 50 enemies, and that recording allocates nothing).
  An empty list says *No noise yet* instead of Unity's *List is empty*.
- **Scene changes in Edit Mode:** opening, creating or switching scenes now refreshes the dashboard, and the Enemies
  list drops destroyed enemies.
- **After Play Mode:** the Overview kept the last cost, graphs and counts; they are now reset. In Edit Mode the card
  says *Enemies in the scene* and the Enemies tab no longer shows the archetype in the Target column.
- **Narrow window:** below 600 px the Enemies tab stacks the list above the panel and hides the distance and reason
  columns; state chips keep their width; the garbage card wraps instead of overflowing (no test for the layout itself;
  checked in the captured screenshots).
- **Add a Horror Target to the player** fell back to the selected object (the NavMesh after **Bake a NavMesh**). With
  no object tagged `Player` and no main camera it now creates a `Player` object, and Setup warns, with **Move it to the
  player**, when a target sits on a static object, a NavMesh Surface or an enemy, instead of reporting it as fine.
- **Icons:** the `.png.meta` files had a stray line from another project (invalid YAML) and generated mipmaps; both are
  fixed. `HorrorEnemy.Perception.cs` now has the enemy's icon in the Project window.
- **Media:** the first frame of the demo GIF showed the NavMesh overlay; the recording now starts paused, with the
  capture frame rate set before entering Play Mode, so the run is the same every time; the frame rate is reset right
  after the recording, so the dashboard screenshots show real times; the README caption matches how the GIF ends
  (no test; checked in the regenerated media).
- **Benchmark figures** quote the range of three runs instead of the lowest one.
- **`Tools/Render-Brand.ps1`** runs the browser with a temporary profile and closes it when done (no test).

### Verified

- 118 EditMode tests pass (96 before the dashboard, 111 before the second verification; 104 with Resonance 0.6.0 and
  Hindsight installed, measured before the dashboard and not re-run since).
- For the release, with the dashboard, the icons and the fixes above: `Tools/Benchmark.ps1` run three times gives 0.044
  to 0.050 ms per frame on average for 50 enemies (p95 0.057 to 0.079 ms), 0 B allocated in every frame;
  `Tools/Build-Check.ps1` finds only the runtime assembly in the player, no call to the dashboard in it, and 0 B
  allocated.
- Before the dashboard, `Tools/Benchmark.ps1` run twice: 50 NavMesh enemies cost 0.044 / 0.045 ms per frame on
  average (p95 0.058 to 0.059 ms), and the update allocated 0 B on every one of the 3,840 frames of each run, warm-up
  included.
- `Tools/Build-Check.ps1`: in a Windows development player the enemy reacts to a noise and the update allocates 0 B.

[0.1.0]: https://github.com/Nekuzaky/Unity-Horror-AI-Manager/releases/tag/v0.1.0
