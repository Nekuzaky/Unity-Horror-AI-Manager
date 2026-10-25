# Contributing to Horror AI Manager

Thanks for helping! Bug reports, ideas and pull requests are all welcome.

## Getting started

1. Install **Unity 6000.3** (6000.3.13f1 is the tested version).
2. Add the repository root in Unity Hub (*Add project from disk*). The tool is the embedded package in
   `Packages/com.nekuzaky.horror-ai-manager` (`Runtime` for the components and the AI, `Editor` for inspectors,
   gizmos and the window, `Integrations` for Resonance and Hindsight, `Tests/Editor` for the tests). Development-only
   code lives in `Assets/Dev`: the demo in `Assets/Dev/Demo`, the scene and preset builders and the benchmark in
   `Assets/Dev/Editor`, the player-build probe in `Assets/Dev/BuildProbe`.
3. Open `Assets/Dev/Demo/Horror AI Demo.unity`, press Play and open **Window > Horror AI**.

Every development task is a script in [`Tools/`](Tools/README.md). Those that start Unity need the project to be
closed in the editor.

| Script | What it does |
|---|---|
| `Tools/Compile.ps1` | Compiles every assembly and lists errors and warnings. |
| `Tools/Test.ps1` | Runs the EditMode tests. |
| `Tools/Check-Rules.ps1` | Checks the code rules listed below. |
| `Tools/Benchmark.ps1` | Measures 50 enemies in real Play Mode, lit and dark. |
| `Tools/Build-Check.ps1` | Builds the demo for Windows, runs it and checks the AI there. |
| `Tools/Build-Demo.ps1` | Regenerates the Patroller preset and the demo scene with its NavMesh. |
| `Tools/Sync-Sample.ps1` | Copies `Assets/Dev/Demo` into the package sample. |

## Rules the code follows

Horror AI Manager follows the spirit of NASA's *Power of 10* rules, adapted to C#, plus a few of its own:

- No comments of any kind in C#: names, small functions and the documentation explain the code.
- No coroutines (`IEnumerator`, `yield`, `StartCoroutine`, `[UnityTest]`): time is simulated and passed explicitly,
  so every test is deterministic.
- No recursion and no `goto`; every loop is a `for` with a fixed upper bound (no `while`).
- No managed allocation per frame once warmed up (tests check it); buffers are reserved up front.
- Functions stay short, under 60 lines.
- No `unsafe` code.
- Zero compiler warnings. `Tools/Compile.ps1` fails on any warning (`-Full` rebuilds everything), and the development
  assemblies under `Assets` build with `-warnaserror+`. The package itself does not force warnings as errors: a
  deprecation in a future Unity version must never stop someone else's project from compiling.

`Tools/Check-Rules.ps1` checks all of these that a text scan can. Style: 4 spaces, `_camelCase` private fields,
`#region` blocks like the existing files, English everywhere.

## Integrations

The Resonance and Hindsight integrations, and their tests in `Tests/Integrations`, compile only when those packages
are installed. The development project does not reference them, so it opens on any machine and on CI. To work on
them, add the packages to `Packages/manifest.json` locally (by Git URL or `file:` path), run `Tools/Test.ps1`, and do
not commit that change.

## Continuous integration

`.github/workflows/tests.yml` checks on every push and pull request that every package file has its `.meta` and that
none is a Git LFS pointer, then runs the package tests with [GameCI](https://game.ci). The Unity steps need three
repository secrets:

| Secret | Value |
| --- | --- |
| `UNITY_LICENSE` | Contents of your `Unity_lic.ulf` file (Personal licenses work) |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

See [GameCI activation](https://game.ci/docs/github/activation) for how to get the license file. Without these secrets
the workflow skips the Unity steps and reports a notice instead of failing.

## Pull requests

- One topic per pull request, with a short description of what changed and how you tested it.
- Run `Tools/Test.ps1` and `Tools/Check-Rules.ps1`; for changes to perception or behavior, also run
  `Tools/Benchmark.ps1` and compare.
- Update `Packages/com.nekuzaky.horror-ai-manager/CHANGELOG.md` under an `Unreleased` section.
- Inspector, gizmo or window changes: include a screenshot.