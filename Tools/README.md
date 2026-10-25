# Tools

Scripts used to develop Horror AI Manager. They need Windows PowerShell 5.1 or PowerShell 7, and the Unity version
written in `ProjectSettings/ProjectVersion.txt` (set `UNITY_EDITOR` to the path of `Unity.exe` if it is installed
elsewhere). On every push and pull request, `.github/workflows/tests.yml` checks the package's `.meta` files and runs
the EditMode tests on GitHub (the Unity steps need the license secrets described in [CONTRIBUTING](../CONTRIBUTING.md)).
The other scripts run only here: run them locally before a pull request or a release. Results and logs go to
`Tools/Output`, which is not committed.

| Script | What it does | Time |
|---|---|---|
| `Compile.ps1 [-Full]` | Compiles in batch mode and fails on any error or warning. `-Full` rebuilds every assembly instead of the changed ones. | ~1 min |
| `Test.ps1 [-Filter Name]` | Runs the EditMode tests (and the integration tests when Resonance and Hindsight are installed) and prints the failures. | ~1 min |
| `Check-Rules.ps1` | Checks the code rules: functions of 60 lines at most, no recursion, no `goto`, no `unsafe`, no comments, no coroutines, no `while` loops, no warning silenced with `#pragma`, warnings as errors for the assemblies under `Assets`, and never in the shipped package. | seconds |
| `Benchmark.ps1` | Plays 50 NavMesh enemies around one target in a lit and a dark scene and writes `Output/benchmark/benchmark.md`. Garbage is read every frame from Unity's profiler (`GC.Alloc` on the main thread, `GC Allocated In Frame`). Fails above 0.1 ms per frame on average, on any byte allocated after the warm-up, or if it cannot be measured. | ~1 min |
| `Build-Check.ps1` | Builds the demo as a Windows development player (release players have no profiler counters to measure garbage), checks that only the runtime assembly ships (no editor, test, development or integration assembly) and that it holds no call to the editor dashboard, runs the player with a probe that makes a noise next to the enemy, and checks that it reacts, without exceptions and with 0 B allocated by the update. | ~1 min |
| `Build-Demo.ps1` | Regenerates the Patroller preset assets and the demo scene in `Assets/Dev/Demo`, NavMesh included. | ~1 min |
| `Sync-Sample.ps1` | Copies `Assets/Dev/Demo` into the package sample `Samples~/Demo`. | seconds |
| `Capture-Media.ps1` | Opens the editor (not in batch mode, keep its windows visible) and records the README media into `docs/images`: the inspectors, the icons, every dashboard tab, the Scene view gizmos, and a scripted run of the demo seen from above, one game frame at a time, turned into `horror-ai-demo.gif`, `horror-ai-demo.mp4` and `dashboard.gif` with ffmpeg. Views are rendered into textures, never read from the screen. | ~2 min |
| `Render-Brand.ps1 [-Only icons\|brand]` | Renders the component and asset icons from the SVG glyphs (`docs/brand/render_icons.py`, Python and Pillow) and the banner and social preview from the HTML sources in `docs/brand` (opened once in a browser, which posts the image back). | seconds |

The scripts that start Unity refuse to run while the project is open in the editor, because Unity cannot open a project twice.