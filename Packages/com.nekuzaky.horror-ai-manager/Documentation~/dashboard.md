# The dashboard

**Tools > Horror AI > Dashboard** (Ctrl+Shift+Alt+H, Cmd+Shift+Alt+H on macOS) opens one window with five tabs.
**Setup** and **Archetypes** work in Edit Mode; **Overview**, **Enemies** and **Noises** show live data in Play Mode.
It follows the editor skin (dark or light) and reflows from a narrow docked panel to a full screen.

![The dashboard in Play Mode](../../../docs/images/dashboard.gif)

The header holds the tabs, a pill that says **Edit Mode**, **Play Mode** or **Paused**, and four buttons:

| Button | What it does |
|---|---|
| Pause | Freezes the display. The data keeps being recorded, so nothing is missing when you resume. |
| Follow the selection | Selecting an enemy in the Hierarchy or the Scene view picks it in the Enemies tab. |
| Book | Opens this page. |
| GitHub | Opens the repository. |

## Overview

![Overview](../../../docs/images/dashboard-overview.png)

- **Enemies by state**: how many enemies patrol, are alert, investigate, search, chase or lost track, in the state
  colors of the Scene view gizmos.
- **Highest suspicion**: the fullest gauge and whose it is.
- **AI cost per frame**: the average and the 95th percentile of the Horror AI update over the last 600 frames
  (about ten seconds), on the main thread.
- **Rays per frame**: sight, light and hearing raycasts, against the budget (`HorrorAI.RaycastBudget`).
- **Perceptions per frame**: how many enemies looked and listened this frame (each one does ten times a second,
  spread over frames).
- **Garbage per frame**: the bytes the update allocated in the last frame and the worst of the last 600, read from
  Unity's profiler counters (`HorrorAI.Stats.AllocatedBytes`). It says *n/a* where those counters do not exist (a
  release player). In the Editor, while an Inspector shows a `Horror Enemy` whose Unity Events have persistent
  listeners (the demo enemy has some), Unity marks those listeners dirty and rebuilds them by reflection
  (`UnityEventBase.RebuildPersistentCallsIfNeeded`) the next time the enemy raises an event. Each change of mind of the
  selected enemy then shows 616 to 684 B. It is Unity's editor work, not the dashboard (it happens with the window
  closed too), it stops when the enemy is deselected, and it does not exist in players: the Windows development player
  checked by `Tools/Build-Check.ps1` allocates 0 B.
- **Enemies running**, with the number of targets and lights (*Enemies in the scene* in Edit Mode).
- Two graphs of the **last 30 seconds**: the AI cost per frame and the highest suspicion.

In Edit Mode the page counts the enemies, targets and lights of the open scenes.

## Enemies

![Enemies](../../../docs/images/dashboard-enemies.png)

One row per enemy: its icon, name, state, suspicion bar, target, distance to it and its last change of mind in plain
words (*heard a noise*, *spotted the target*, *lost sight of the target*). Click a row to select the enemy and frame it
in the Scene view.

The panel on the right shows the picked enemy:

- **What it perceives**: how much of the target it sees, how long the gauge would take to fill at this rate, the
  light on the target, whether the target crouches, the distance, and how long ago it last heard a noise.
- **What it remembers**: the last known position, how long ago, how sure it is and how it knows (saw it, heard it,
  told by a script).
- **Suspicion, last 30 seconds**.
- **Changes of mind**: the last 16 transitions, newest first, with how long ago, from and to, and why.

In Edit Mode the list shows the enemies of the open scenes (*Not running*) and how many things their inspector asks
you to check; it follows the scene when you open, create or switch scenes. Below about 600 px wide, the list and the
panel stack vertically.

## Noises

![Noises](../../../docs/images/dashboard-noises.png)

The last 64 noises, newest first: when, what made it (*a script* when there is no source), its loudness in meters,
where, and which enemies heard it, with the level left after distance and walls (*Ghoul 26% (1 wall)*), and how many
were in reach but found it too faint. The six loudest hearers are named, then *and 12 more*: the summary is kept
for each of the 64 noises, so it holds with fifty enemies. Click a noise to show it in the Scene view: the view moves to it, rings pulse
there for two seconds and its source is pinged in the Hierarchy.

## Setup

![Setup](../../../docs/images/dashboard-setup.png)

Checks the open scenes and fixes what it can in one click. Every fix can be undone.

| Check | Fix |
|---|---|
| There is at least one Horror Enemy | Add an enemy |
| A NavMesh is baked and every enemy with an agent stands on it | Bake a NavMesh |
| There is a Horror Target | Add a Horror Target to the player: the object tagged `Player`, else the root of the main camera, else a new `Player` object |
| No Horror Target sits on level geometry (static), the NavMesh Surface or an enemy | Move it to the player |
| Every point and spot light is marked for the AI (lights carried by an enemy are skipped) | Mark every light |
| Every Horror Light Source on a Light is linked to it | Link them |
| Every enemy has an archetype | Use the Patroller preset |
| Every enemy has a NavMesh Agent | Add the agents |
| Every enemy's eye is part of it | |
| Patrol routes have two waypoints or more, all set and on the NavMesh | Create the routes, Bake a NavMesh |

It also notes the render pipeline (light is estimated from the Horror Light Sources, so every pipeline behaves the
same), that input is yours (your player script sets `HorrorTarget.Crouching` and emits footsteps), and whether the ray
budget suits the number of enemies. The checks run again when the hierarchy changes, after a fix, or with
**Check again**. The fixes are the same as in the inspectors.

## Archetypes

![Archetypes](../../../docs/images/dashboard-archetypes.png)

Every `Horror Archetype`, `Perception Profile` and `Behavior Profile` of the project, with a summary in plain words
(*a standing target in plain view is spotted in 0.5 s when lit, 3.2 s in the dark close by...*) and how many enemies of
the open scenes use each archetype. Click one to select it. **New archetype from the Patroller** copies the preset and
its two profiles next to the file you choose; **New perception profile** and **New behavior profile** create a profile
with the default values.

## What it costs

Nothing when the window is closed, and nothing in a build.

- The runtime calls the dashboard at three places: after each update, on each change of mind and on each noise an
  enemy evaluates. These calls carry `[Conditional("UNITY_EDITOR")]`: the compiler removes them from player builds
  (`Tools/Build-Check.ps1` builds the demo, and its runtime assembly contains no call to them). In the Editor, with the
  window closed, each call is one check of a null field.
- The window lives in the editor assembly, which is never part of a build.
- While it is open, it records into buffers allocated once: 600 frames of cost, rays, perceptions and garbage,
  30 seconds of history at ten samples a second, the last 16 changes of mind and 30 seconds of suspicion per enemy,
  the last 256 hearings, and a summary of who heard each of the last 64 noises. Recording a frame allocates nothing; the tests check it, and check that the update still
  allocates 0 B with the window open.
- The display refreshes ten times a second, only the visible tab, only while the window is visible. Lists reuse their
  rows; labels change only when their value does, and numbers come from cached strings, so a refresh where nothing
  changed allocates nothing (tested).
