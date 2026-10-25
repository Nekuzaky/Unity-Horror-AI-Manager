# Performance and how it works

## How it works

A hidden `Horror AI` object updates every enemy once per frame, after your scripts' `Update`:

1. **Light.** Each target's exposure is recomputed every 0.1 s: one ray per marked light that reaches it (eight at
   most).
2. **Perception, time-sliced.** Each enemy perceives at its own interval (0.1 s by default), and enemies are spread
   over frames from the start. A perception costs up to four sight rays per target and one ray per new noise in reach.
3. **Raycast budget.** Before each perception, the worst-case number of rays is checked against what is left of
   `HorrorAI.RaycastBudget` (128 per frame by default). When the budget is spent, the remaining enemies wait for the
   next frame, first in line. Nobody is ever skipped forever, and the budget is never exceeded.
4. **Behavior.** Every enemy's state machine runs every frame (no ray), so timers, turning and movement stay smooth.
   NavMesh destinations are only set again when they move by more than 35 cm.

## Zero allocation

Everything is reserved when components are created: fixed arrays for noises and ray hits, lists sized for 64 enemies,
structs for perception, memory and transitions, non-allocating physics queries (`Linecast`, `RaycastNonAlloc`), no
LINQ, no closures, no boxing, no string built per frame. Unity Events are only raised on a change of mind. The tests
check it with Unity's `Is.Not.AllocatingGCMemory()` over hundreds of frames, including state changes and events, and
`HorrorAI.Stats.AllocatedBytes` reports it live (the Horror AI window shows it).

## Benchmark

`Tools/Benchmark.ps1` plays two scenarios in real Play Mode: 50 enemies with NavMesh Agents around one target that
walks in a circle among walls, with 8 lamps and a flashlight on, then all lights off. 1,800 frames each at 60 fps,
after 120 warm-up frames.

| Scenario | Horror AI per frame (avg / p95 / max) | Raycasts per frame | Perceptions per frame | GC per frame | States at the end |
|---|---:|---:|---:|---:|---|
| 50 enemies, lit scene (8 lamps + flashlight) | 0.044 / 0.058 / 0.446 ms | 13.5 | 8.3 | 0 B | 10 Patrol, 1 Alert, 3 Investigate, 24 Search, 9 Chase, 3 Lost Track |
| 50 enemies, dark scene (lights off) | 0.045 / 0.057 / 0.138 ms | 14.4 | 8.3 | 0 B | 13 Patrol, 11 Investigate, 22 Search, 4 Chase |

Intel Core i7-14700KF, Unity 6000.3.13f1, Editor Play Mode in batch mode. The script fails if the average reaches
0.1 ms or if any byte is allocated. `Tools/Build-Check.ps1` runs the demo in a Windows player and checks that the
update allocates 0 B there too.

## Tuning

- More enemies: raise *Update Interval* in the Perception Profile (0.2 s halves the rays), or lower the budget.
- Fewer rays per perception: give the target fewer body points.
- Very large levels: put walls on a layer and set *Occluders* so that props and triggers are ignored.