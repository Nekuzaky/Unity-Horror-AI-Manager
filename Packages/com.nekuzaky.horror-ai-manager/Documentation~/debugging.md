# Debugging

## Scene view

Every enemy draws its **view cone** in the color of its state: green (Patrol), yellow (Alert), orange
(Investigate), purple (Search), red (Chase), pink (Lost Track). Select it to also see:

- the **proximity radius** (it feels you inside it, in any direction);
- in blue, how far a **footstep** of loudness 6 is heard in the open;
- in Play Mode, its **last known position** (a thick red cross with a dark outline and a ring, brighter when it is
  surer), a line to its current
  **destination**, and while searching, the **search area** and the **path** of its last search points.

In Play Mode, a **suspicion bar** and the state name, on a dark label underlined in the state color, float above each
enemy, and every noise draws a blue ring that
grows to its loudness. Patrol routes draw their waypoints, waits and loop; selected lights draw their range and beam;
targets draw their body points, brighter when lit.

## The Horror AI window

Open it with **Tools > Horror AI > Open Horror AI** (Ctrl+Shift+Alt+H; Cmd+Shift+Alt+H on macOS).
**Tools > Horror AI > Documentation** and **GitHub Repository** open these pages. In Play Mode, the window lists every running enemy with:

- its **state** and **suspicion**;
- the **last change**: from, to, why and how long ago (*Alert → Investigate because it went to check, 0.4 s ago*);
- what it **sees and hears** now (*sees 67% of the target, lit 85%; heard a noise 1.2 s ago*);
- its **memory** (*Sight, 92% sure, 0.3 s old*).

The first line shows the cost of the last frame: milliseconds, perceptions, raycasts against the budget, deferred
perceptions and the bytes the update allocated (from Unity's profiler counters; *allocation not measured* in a
release player, where they do not exist). Click an enemy's name to select it.

Outside Play Mode, the window lists the enemies of the open scenes and how many problems each inspector reports. It
refreshes when the hierarchy changes or another scene is opened.

## The inspectors

While playing, the `Horror Enemy` inspector shows its state, suspicion, what it sees and its memory, live. The
`Horror Target` inspector shows how lit it is.