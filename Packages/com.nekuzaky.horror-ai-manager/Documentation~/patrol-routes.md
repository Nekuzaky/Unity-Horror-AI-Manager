# Patrol routes

What an enemy does while nothing is wrong. A `Patrol Route` is an object whose waypoints are child transforms, each
with a wait in seconds.

## Creating one

- From an enemy's inspector: **Create a patrol route** adds a route with two waypoints in front of it and assigns it.
- **GameObject > Horror AI > Patrol Route** creates one at the Scene view's pivot.
- **Add a waypoint** in the route inspector adds one four meters past the last and selects it, so you can move it
  right away.

Several enemies can share a route; each one walks it on its own.

## Modes

| Mode | What the enemy does |
|---|---|
| Loop | Goes back to the first waypoint after the last. |
| Ping Pong | Walks back and forth: 1, 2, 3, 2, 1... |
| Once | Stops at the last waypoint and stands guard there. |

With a single waypoint, or at the end of a *Once* route, the enemy waits there and checks the route again every half
second (or every wait, if longer), so waypoints added at runtime are followed. Without a route, it guards the spot
where it started and comes back there after a search.

## Checks

The route inspector and the dashboard's **Setup** tab warn when a route has fewer than two waypoints, a waypoint has no
transform, or a waypoint is not on the NavMesh (the enemy could not reach it), with **Bake a NavMesh** next to it.

In the Scene view, a route draws its waypoints, their waits and the path between them in green. The patrol speed is
*Patrol Speed* in the enemy's Behavior Profile.

```csharp
var point = route.AddWaypoint(new Vector3(4f, 0f, 2f), wait: 1.5f);
```
