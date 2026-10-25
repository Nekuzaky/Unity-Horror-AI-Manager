# Horror AI Manager

<p align="center">
  <a href="https://github.com/Nekuzaky/Unity-Horror-AI-Manager/actions/workflows/tests.yml"><img alt="Tests" src="https://github.com/Nekuzaky/Unity-Horror-AI-Manager/actions/workflows/tests.yml/badge.svg"></a>
  <img alt="Unity 6000.3 or newer" src="https://img.shields.io/badge/Unity-6000.3%2B-1b1d24?logo=unity&logoColor=white">
  <img alt="Version 0.1.0" src="https://img.shields.io/badge/version-0.1.0-8b7cff">
  <img alt="50 enemies: 0.044 ms per frame" src="https://img.shields.io/badge/50%20enemies-0.044%20ms-3ddc84">
  <img alt="GC per frame: 0 B, measured" src="https://img.shields.io/badge/GC%20per%20frame-0%20B%20measured-3ddc84">
  <img alt="Tests: 96 EditMode" src="https://img.shields.io/badge/tests-96%20EditMode-38bdf8">
  <a href="LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-8b92a2"></a>
</p>

<p align="center">
  <a href="https://github.com/sponsors/Nekuzaky"><img alt="Sponsor on GitHub" src="https://img.shields.io/badge/Sponsor-GitHub-ea4aaa?logo=githubsponsors&logoColor=white"></a>
  <a href="https://www.patreon.com/Nekuzaky"><img alt="Patreon" src="https://img.shields.io/badge/Patreon-Nekuzaky-f96854?logo=patreon&logoColor=white"></a>
  <a href="https://buymeacoffee.com/nekuzaky"><img alt="Buy me a coffee" src="https://img.shields.io/badge/Buy%20me%20a%20coffee-Nekuzaky-ffdd00?logo=buymeacoffee&logoColor=black"></a>
</p>

**Horror AI Manager** is enemy AI for horror games in Unity. Your monsters see with a view cone and a suspicion
gauge, hear your footsteps through walls, remember where you were, and patrol, investigate, search and chase you.
**Light matters:** a player in the dark is noticed slowly, a player under a lamp or holding a flashlight is spotted
at once, and crouching behind a crate actually hides you.

**Hide in the dark. Hold your breath. Do not run.**

## Why

Every horror game needs the same enemy: one that walks its round, stops when it hears a bottle fall, comes to check,
searches the room, and gives up just as you were about to scream. Writing it is easy; making it fair is not. An enemy
that sees you through the dark the instant you enter its cone is not scary, it is frustrating. Horror AI Manager gives
you that enemy, tuned for stealth horror, in a few components, with events for every scare and a debug window that
tells you *why* the monster did what it did.

## Features

- **Sight with a suspicion gauge.** A view cone (angle, range, eye height) and up to four rays to points on the
  target's body. The gauge fills over time instead of a yes/no: fast when the target is close, lit and fully visible,
  slowly when it is far, in the dark or half hidden. It decays once the target is out of sight.
- **Light matters, without reading the render.** Mark lamps and flashlights with a `Horror Light Source`. A target's
  exposure is estimated from the marked lights (distance, beam cone, an occlusion ray), so it works the same with the
  built-in pipeline, URP and HDRP. A marked sun or moon lights every target under the open sky. A flashlight makes its holder visible from afar while it is on.
- **Crouching.** `HorrorTarget.Crouching` lowers the points enemies look at (low cover hides you) and slows the gauge.
- **Hearing through walls.** `HorrorAI.EmitNoise(position, loudness, source)` from code, or a `Noise Emitter` for
  props that clatter when they fall, floors that creak when you step on them, or any Unity Event and animation
  event. Noise fades with distance and each wall in the way damps it.
- **Memory.** The last known position, when, and how sure the enemy is. Certainty fades over time.
- **A deterministic behavior state machine.** Patrol, Alert, Investigate, Search (around the last known position),
  Chase, Lost Track, then back to Patrol. Every threshold, duration, speed and the search radius live in assets.
- **Events for every scare.** C# events and Unity Events on *spotted*, *chase started*, *caught*, *lost track*,
  *search started*, *gave up*, *alerted*, *noise heard*: hook sounds, music and jump scares without code.
- **Data in ScriptableObjects.** `Perception Profile`, `Behavior Profile` and `Horror Archetype` (both together).
  Ships with the **Patroller** preset.
- **Plain-word inspectors with one-click fixes.** *There is no NavMesh under this enemy* → **Bake a NavMesh**.
  *There is no Horror Target in the scene* → **Add a Horror Target to the player**. Missing archetype, collider,
  Rigidbody, patrol route or unmarked lights, each explained with its fix.
- **Gizmos and a live debug window.** The view cone, the hearing radius, the suspicion bar, the last known
  position, the search area and path, noise rings, all colored by state. **Tools > Horror AI > Open Horror AI**
  (Ctrl+Shift+Alt+H) lists every enemy
  with its state, suspicion, what it sees and hears, its memory and the reason of its last change of mind.
- **Built for many enemies.** Perception is spread over frames, raycasts per frame are capped by a global budget,
  and nothing is allocated per frame, as measured with Unity's profiler counters. Fifty enemies cost 0.044 ms per
  frame.
- **Optional integrations.** [Resonance](https://github.com/Nekuzaky/Resonance) 0.6.0 or newer: its sounds can be
  heard by enemies;
  [Hindsight](https://github.com/Nekuzaky/Hindsight) records each enemy's state and suspicion and bookmarks every
  change of mind. Each integration compiles only when that package is installed.

## Install

Requires **Unity 6000.3** or newer (Unity 6.3 LTS) and the **AI Navigation** package (installed automatically).
Works with the built-in pipeline, URP and HDRP.

In Unity: **Window > Package Manager > + > Install package from git URL...** and paste:

```
https://github.com/Nekuzaky/Unity-Horror-AI-Manager.git?path=/Packages/com.nekuzaky.horror-ai-manager
```

Pin a release by adding its tag, for example `...?path=/Packages/com.nekuzaky.horror-ai-manager#v0.1.0`.

## Quick start

1. **GameObject > Horror AI > Enemy.** It comes with a NavMesh Agent and the Patroller archetype.
2. Follow its inspector: **Bake a NavMesh**, **Add a Horror Target to the player**, **Create a patrol route**.
3. Put a **Horror Light Source** on your lamps (**GameObject > Horror AI > Mark Every Light For The AI**) and on the
   player's flashlight with **Reveals Its Holder** on.
4. Make noise: `HorrorAI.EmitNoise(transform.position, 6f, gameObject)` on each footstep, or a **Noise Emitter** on
   props.
5. Press Play and open **Tools > Horror AI > Open Horror AI** (Ctrl+Shift+Alt+H).

To try it first, import the **Greybox Demo** from the package's **Samples** tab: a dark level, three lamps, bottles
that clatter, a creaky floor, and one patrolling enemy. WASD, Shift to run, C to crouch, F for the flashlight. It
reads the Input System (or the Input Manager when that is the only one active) and swaps its materials for the active
pipeline's Lit shader under URP and HDRP.

## Components

| Component | Put it on | What it does |
|---|---|---|
| `Horror Enemy` | Each monster (with a `NavMesh Agent`) | Sees, hears, remembers and runs the state machine. Holds the events. |
| `Horror Target` | The player | What enemies look for. `Crouching`, visibility multiplier, body points. |
| `Horror Light Source` | Lamps, torches, the flashlight, the moon | Reveals targets in its range and beam (or under the sky). Follows a `Light`, or set by hand. |
| `Noise Emitter` | Props, floors, triggers | Makes a noise on collision, trigger, start, or when `Emit()` is called. |
| `Patrol Route` | An empty object with waypoint children | Loop, ping-pong or once, with a wait per waypoint. |
| `Horror Archetype` | Asset | A kind of enemy: a `Perception Profile` plus a `Behavior Profile`. |

## Scripting

```csharp
using Nekuzaky.HorrorAI;
using UnityEngine;

public class Footsteps : MonoBehaviour
{
    public HorrorTarget target;

    void OnStep(bool running) =>
        HorrorAI.EmitNoise(transform.position, target.Crouching ? 1.5f : running ? 10f : 4f, gameObject);
}

public class ChaseMusic : MonoBehaviour
{
    void OnEnable() => HorrorAI.StateChanged += OnStateChanged;
    void OnDisable() => HorrorAI.StateChanged -= OnStateChanged;

    void OnStateChanged(HorrorEnemy enemy, HorrorTransition change)
    {
        if (change.To == HorrorState.Chase) Debug.Log($"{enemy.name} {change.Explanation}");   // "spotted the target"
    }
}
```

`HorrorEnemy` exposes `State`, `Suspicion`, `Memory`, `Perception`, `LastTransition`, `Target`, and
`Investigate(position)` (lure it somewhere) and `Calm()` (send it back to its patrol). See
[Scripting](Packages/com.nekuzaky.horror-ai-manager/Documentation~/scripting.md).

## Performance

Measured in real Play Mode with `Tools/Benchmark.ps1`, run twice: 50 NavMesh enemies around one moving target,
1,800 frames per scenario at 60 fps after 120 warm-up frames, Intel Core i7-14700KF, Unity 6000.3.13f1.

| Scenario | Run | Horror AI per frame (avg / p95 / max) | Raycasts per frame | Perceptions per frame | GC allocated by the update |
|---|---|---:|---:|---:|---:|
| 50 enemies, lit scene (8 lamps + flashlight) | 1 | 0.044 / 0.059 / 0.224 ms | 12.1 | 8.3 | 0 B in 1,800 frames |
| | 2 | 0.044 / 0.058 / 0.259 ms | 12.6 | 8.3 | 0 B in 1,800 frames |
| 50 enemies, dark scene (lights off) | 1 | 0.044 / 0.059 / 0.162 ms | 12.1 | 8.3 | 0 B in 1,800 frames |
| | 2 | 0.045 / 0.059 / 0.471 ms | 12.0 | 8.3 | 0 B in 1,800 frames |

The time covers the whole Horror AI update on the main thread: light exposure, sight and hearing raycasts, the
state machines and the NavMesh destinations they set (Unity's own agent movement is not included). Each enemy
perceives ten times per second, spread over frames, and the default budget caps raycasts at 128 per frame.

The garbage is read every frame from Unity's profiler: the `GC.Alloc` marker on the update's thread and the
`GC Allocated In Frame` counter. The update allocated 0 B on every frame, the 120 warm-up frames included: the first
enemy that registers compiles the package's code and runs each behavior path once, so nothing is compiled or
prepared in the middle of a game. A Windows development player of the demo, checked by `Tools/Build-Check.ps1`,
also allocates 0 B (release players have no profiler counters, so `HorrorAI.Stats.AllocatedBytes` is -1 there).
More in
[Performance](Packages/com.nekuzaky.horror-ai-manager/Documentation~/performance.md).

## Documentation

- [Getting started](Packages/com.nekuzaky.horror-ai-manager/Documentation~/getting-started.md)
- [Components](Packages/com.nekuzaky.horror-ai-manager/Documentation~/components.md)
- [Perception: sight, light, hearing, memory](Packages/com.nekuzaky.horror-ai-manager/Documentation~/perception.md)
- [Behavior: the state machine and its events](Packages/com.nekuzaky.horror-ai-manager/Documentation~/behavior.md)
- [Archetypes and presets](Packages/com.nekuzaky.horror-ai-manager/Documentation~/archetypes.md)
- [Debugging: gizmos and the Horror AI window](Packages/com.nekuzaky.horror-ai-manager/Documentation~/debugging.md)
- [Scripting](Packages/com.nekuzaky.horror-ai-manager/Documentation~/scripting.md)
- [Resonance and Hindsight integrations](Packages/com.nekuzaky.horror-ai-manager/Documentation~/integrations.md)
- [Performance and how it works](Packages/com.nekuzaky.horror-ai-manager/Documentation~/performance.md)
- [Limitations and troubleshooting](Packages/com.nekuzaky.horror-ai-manager/Documentation~/troubleshooting.md)

## Limitations

- Light exposure is estimated from the lights you mark, not read from the rendered image: baked lighting, emissive
  materials and light probes do not count unless a `Horror Light Source` stands for them. A directional light counts
  as fully on or blocked (one ray toward it); raise `HorrorAI.AmbientLight` for light that is everywhere.
- Each enemy keeps one suspicion gauge and watches at most four targets; it chases the most visible one. Built for
  single-player games.
- Sight and light use raycasts against colliders: a target needs no collider, but walls and cover do.
- Hearing is straight-line with a damping per wall, not sound propagation through doors and corridors.
- Enemies move with Unity's NavMesh Agent (or straight lines without one). No off-mesh link or climbing logic of its own.

## Roadmap

- More archetypes: **Stalker** (follows you at a distance and hides when you look), **Blind** (hearing only, very
  sharp), **Statue** (moves only when not seen), **Ambusher** (waits in a hiding spot for you to pass).
- A **Scare Director** that paces encounters: tension over time, cool-down after a chase, music cues.
- Doors and hiding spots (lockers, under beds) that enemies understand.
- Sound propagation through rooms and doors.

## Contributing

Bug reports, ideas and pull requests are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md): the repository is the package
plus a development project (Input System only), and every development task is a script in [`Tools/`](Tools/README.md). The code follows
the spirit of NASA's *Power of 10* rules, has no comments and no coroutines, and builds with zero warnings.

## Support

Horror AI Manager is free and MIT licensed. If it made your monster scarier, you can support its development on
[GitHub Sponsors](https://github.com/sponsors/Nekuzaky), [Patreon](https://www.patreon.com/Nekuzaky) or
[Buy Me a Coffee](https://buymeacoffee.com/nekuzaky).

## License

[MIT](LICENSE) © Nekuzaky. See [Third Party Notices](Packages/com.nekuzaky.horror-ai-manager/Third%20Party%20Notices.md).
