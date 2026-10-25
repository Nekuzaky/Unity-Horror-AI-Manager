# Behavior

Each enemy runs a deterministic state machine every frame, from what it perceived and its memory.

```
Patrol ─(suspicious)→ Alert ─(stared long enough)→ Investigate ─(reached the spot)→ Search ─(time over)→ Patrol
   │                    │                              │                               │
   └────────────(spotted: gauge full while seeing the target)──────────────────→ Chase ─(lost sight)→ Lost Track
                                                                                  ↑                        │
                                                                                  └───(saw it again)───────┤
                                                                                       Search ←(reached / time over)
```

| State | What it does | Leaves when |
|---|---|---|
| **Patrol** | Walks its route at *Patrol Speed*, waiting at each waypoint; without a route, guards its home. | Suspicion ≥ *Alert Threshold* → Alert. Spotted → Chase. |
| **Alert** | Stops and turns toward the last known position. | After *Alert Duration*: Investigate if it still remembers something and suspicion ≥ *Calm Threshold*, else Patrol (*calmed down*). Spotted → Chase. |
| **Investigate** | Walks to the last known position at *Investigate Speed* (it follows the memory if it moves). | Arrived → Search. After *Investigate Timeout* → Search. Spotted → Chase. |
| **Search** | Visits random points within *Search Radius* of where it lost you, waiting *Search Point Wait* at each. | A new noise or glimpse above *Alert Threshold* → Investigate. After *Search Duration* → Patrol (*gave up*, forgets everything). Spotted → Chase. |
| **Chase** | Runs at *Chase Speed* to the target while it sees it, then to its last known position. Within *Catch Distance*: **caught**. | *Lose Sight Grace* seconds without seeing the target → Lost Track. |
| **Lost Track** | Rushes to the last place it saw the target. | Spotted again → Chase (*saw the target again*). Arrived, or after *Lost Track Timeout* → Search. |

Every change records a `HorrorTransition` (from, to, reason, time). `HorrorTransition.Explanation` turns the reason
into words: *heard a noise*, *spotted the target*, *lost sight of the target*, *gave up the search*... The inspector
and the Horror AI window show it.

The search points come from a small random generator seeded with the enemy's *Search Seed*: the same inputs give the
same search.

## Events

| Unity Event | C# | Raised when |
|---|---|---|
| On State Changed (HorrorState) | `HorrorEnemy.StateChanged`, `HorrorAI.StateChanged` | Every change, with the transition in C#. |
| On Alerted | | It enters Alert. |
| On Investigate | | It enters Investigate. |
| On Spotted | | It enters Chase because it spotted the target (not when it sees it again during Lost Track). |
| On Chase Started | | It enters Chase for any reason. |
| On Caught | `HorrorEnemy.Caught` | It reaches the target during a chase (at most once per *Catch Cooldown*). |
| On Lost Track | | It enters Lost Track. |
| On Search Started | | It enters Search. |
| On Gave Up | | It gives up the search and goes back to Patrol. |
| On Noise Heard | `HorrorEnemy.NoiseHeard` | It hears a noise above its threshold. |