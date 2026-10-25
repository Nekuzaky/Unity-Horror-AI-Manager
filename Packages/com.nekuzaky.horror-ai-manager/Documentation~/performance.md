# Performance and how it works

## How it works

A hidden `Horror AI` object updates every enemy once per frame, after your scripts' `Update`:

1. **Light.** Each target's exposure is recomputed every 0.1 s: one ray per marked light that reaches it (eight at
   most).
2. **Perception, time-sliced.** Each enemy perceives at its own interval (0.1 s by default), and enemies are spread
   over frames from the start. A perception costs up to four sight rays per target and one ray per new noise in reach.
3. **Raycast budget.** Before each perception, the worst-case number of rays is checked against what is left of
   `HorrorAI.RaycastBudget` (128 per frame by default). When the budget is spent, the remaining enemies wait for the
   next frame, and the first one that had to wait goes first. Nobody is skipped for long (with 60 enemies and a
   budget of 30 rays, the longest wait measured by the tests is 8 frames), and the budget is never exceeded.
4. **Behavior.** Every enemy's state machine runs every frame (no ray), so timers, turning and movement stay smooth.
   NavMesh destinations are only set again when they move by more than 35 cm.

## Zero allocation

Everything is reserved when components are created: fixed arrays for noises and ray hits, lists sized for 64 enemies,
structs for perception, memory and transitions, non-allocating physics queries (`Linecast`, `RaycastNonAlloc`), no
LINQ, no closures, no boxing, no string built per frame. Rays recognise targets and enemies by collider id, so no
collider object is created the first time a ray hits it.

Some costs only happen once, the first time a path runs: the runtime compiles a method the first time it is called,
and a Unity Event prepares its listeners the first time it fires. So that they never land in the middle of a game:

- the first enemy that registers compiles every method of the package and runs each state of the behavior once, on a
  private brain that does not move anything;
- each enemy prepares its Unity Events when it is enabled. Listeners added from code after that are prepared the first
  time the event fires.

`HorrorAI.Stats.AllocatedBytes` reports what the update allocated, from Unity's profiler: the `GC.Alloc` marker counts
the allocations made on the update's thread, and `GC Allocated In Frame` gives their size (memory allocated by other
threads at the same time is not counted). It is shown in the [dashboard](dashboard.md), and is -1 in a release player, where
these counters do not exist. The tests check that a deliberate 4 KB allocation is seen, that another thread's is not,
and that perception, every first change of mind and Unity Events with persistent listeners allocate nothing
(`Is.Not.AllocatingGCMemory()`). NavMesh Agents are not placed on the NavMesh in Edit Mode (a test checks it), so the
allocation of the update with real agents is measured by the benchmark, in Play Mode.

## Benchmark

`Tools/Benchmark.ps1` plays two scenarios in real Play Mode: 50 enemies with NavMesh Agents around one target that
walks in a circle among walls, with 8 lamps and a flashlight on, then all lights off. 1,800 frames each at 60 fps,
after 120 warm-up frames. Two runs before the dashboard was added:

| Scenario | Run | Horror AI per frame (avg / p95 / max) | Raycasts per frame | Perceptions per frame | GC allocated by the update | States at the end |
|---|---|---:|---:|---:|---:|---|
| 50 enemies, lit scene (8 lamps + flashlight) | 1 | 0.044 / 0.059 / 0.224 ms | 12.1 | 8.3 | 0 B in 1,800 frames | 13 Patrol, 1 Alert, 4 Investigate, 27 Search, 5 Chase |
| | 2 | 0.044 / 0.058 / 0.259 ms | 12.6 | 8.3 | 0 B in 1,800 frames | 14 Patrol, 3 Investigate, 29 Search, 4 Chase |
| 50 enemies, dark scene (lights off) | 1 | 0.044 / 0.059 / 0.162 ms | 12.1 | 8.3 | 0 B in 1,800 frames | 23 Patrol, 2 Investigate, 24 Search, 1 Chase |
| | 2 | 0.045 / 0.059 / 0.471 ms | 12.0 | 8.3 | 0 B in 1,800 frames | 20 Patrol, 3 Investigate, 25 Search, 2 Chase |

Run three times again for the release, with the dashboard (its calls are in the update, the window closed, as in a
game):

| Scenario | Run | Horror AI per frame (avg / p95 / max) | Raycasts per frame | Perceptions per frame | GC allocated by the update | States at the end |
|---|---|---:|---:|---:|---:|---|
| 50 enemies, lit scene (8 lamps + flashlight) | 1 | 0.045 / 0.059 / 0.301 ms | 12.4 | 8.3 | 0 B in 1,800 frames | 12 Patrol, 2 Investigate, 31 Search, 5 Chase |
| | 2 | 0.046 / 0.064 / 0.284 ms | 11.9 | 8.3 | 0 B in 1,800 frames | 14 Patrol, 3 Investigate, 28 Search, 5 Chase |
| | 3 | 0.045 / 0.062 / 0.264 ms | 12.0 | 8.3 | 0 B in 1,800 frames | 12 Patrol, 3 Investigate, 30 Search, 5 Chase |
| 50 enemies, dark scene (lights off) | 1 | 0.044 / 0.057 / 0.096 ms | 12.6 | 8.3 | 0 B in 1,800 frames | 17 Patrol, 5 Investigate, 26 Search, 2 Chase |
| | 2 | 0.050 / 0.079 / 0.347 ms | 12.8 | 8.3 | 0 B in 1,800 frames | 24 Patrol, 2 Investigate, 23 Search, 1 Chase |
| | 3 | 0.047 / 0.064 / 1.520 ms | 12.4 | 8.3 | 0 B in 1,800 frames | 18 Patrol, 1 Alert, 4 Investigate, 26 Search, 1 Chase |

So 0.044 to 0.050 ms per frame on average, 0.057 to 0.079 ms at the 95th percentile. The maximum is a single frame
(the editor's own work landing in the measured span) and varies most from run to run.

The warm-up frames allocated 0 B too. Intel Core i7-14700KF, Unity 6000.3.13f1, Editor Play Mode in batch mode. The
script fails if the average reaches 0.1 ms, if any byte is allocated after the warm-up, or if the allocations cannot
be measured; it logs the frame and the changes of mind of any frame that allocates. `Tools/Build-Check.ps1` runs the
demo in a Windows development player and checks that the update allocates 0 B there too.

## Tuning

- More enemies: raise *Update Interval* in the Perception Profile (0.2 s halves the rays), or lower the budget.
- Fewer rays per perception: give the target fewer body points.
- Very large levels: put walls on a layer and set *Occluders* so that props and triggers are ignored.