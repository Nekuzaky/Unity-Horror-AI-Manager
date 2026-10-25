# Debugging

![Scene view gizmos during a chase](../../../docs/images/scene-gizmos.png)

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

## The dashboard

**Tools > Horror AI > Dashboard** (Ctrl+Shift+Alt+H; Cmd+Shift+Alt+H on macOS) shows every enemy live: its state,
suspicion, what it perceives and remembers, the history of its changes of mind, the noises and who heard them, the cost
of the update, and a Setup tab that checks the scene. See [The dashboard](dashboard.md).
**Tools > Horror AI > Documentation** and **GitHub Repository** open these pages.

## The inspectors

While playing, the `Horror Enemy` inspector shows its state, suspicion, what it sees and its memory, live. The
`Horror Target` inspector shows how lit it is.