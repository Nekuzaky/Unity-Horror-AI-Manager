# Scripting

Everything lives in the `Nekuzaky.HorrorAI` namespace.

## HorrorAI

| Member | Meaning |
|---|---|
| `EmitNoise(Vector3 position, float loudness, GameObject source = null)` | A noise enemies can hear. Loudness in meters. Enemies ignore their own noises. |
| `Enemies`, `Targets`, `Lights` | The running components (`IReadOnlyList`; loop with `for` to avoid an enumerator). |
| `Stats` | The last frame: enemies, perceptions, raycasts, deferred perceptions, milliseconds, and `AllocatedBytes`, the managed memory the update allocated on its own thread, read from Unity's profiler (`GC.Alloc` marker and `GC Allocated In Frame` counter). -1 in a release player, where these do not exist: measure in a development build. |
| `RaycastBudget` | Raycasts allowed per frame for all enemies (128 by default, 24 at least). |
| `AmbientLight` | Exposure of a target no light reaches (0.04 by default). |
| `GetExposure(HorrorTarget)` | How lit a target is, 0 to 1. |
| `CopyRecentNoises(HorrorNoise[] buffer)` | The most recent noises, newest first. |
| `StateChanged`, `NoiseEmitted`, `EnemyAdded`, `EnemyRemoved` | Global events. Cleared when Play Mode starts, also with domain reload disabled: subscribe in `OnEnable`, or from a `RuntimeInitializeOnLoadMethod` at `AfterAssembliesLoaded` or later. |
| `Time` | The clock the AI runs on (seconds since it started). |

## HorrorEnemy

| Member | Meaning |
|---|---|
| `State`, `StateTime`, `LastTransition` | The current state, for how long, and the last change with its reason. |
| `Suspicion` | The gauge, 0 to 1. |
| `Perception` | `Sees`, `VisibleFraction`, `Distance`, `Exposure`, `GainRate`, `HeardLoudness`, `HeardPosition`, `HeardTime`. |
| `Memory` | `Position`, `Time`, `Certainty`, `Source`, `HasMemory`. |
| `Target` | The target it is watching. |
| `Investigate(Vector3 position)` | Lure it: it goes to check that position (a thrown bottle, a radio). |
| `Calm()` | Back to Patrol; it forgets everything. |
| `StateChanged`, `Caught`, `NoiseHeard` | C# events. The Unity Events are listed in [Behavior](behavior.md#events). |
| `SetMover(IHorrorMover)` | Replace how it moves (your own locomotion, root motion). |

## Examples

Play a stinger when any enemy spots the player:

```csharp
void OnEnable() => HorrorAI.StateChanged += OnChange;
void OnDisable() => HorrorAI.StateChanged -= OnChange;

void OnChange(HorrorEnemy enemy, HorrorTransition change)
{
    if (change.Reason == TransitionReason.Spotted)
        stinger.Play();
}
```

A thrown bottle that lures enemies where it lands: put a `Noise Emitter` on it (Collision Enter), or

```csharp
void OnCollisionEnter(Collision hit) => HorrorAI.EmitNoise(transform.position, 14f, gameObject);
```

A locker that hides the player:

```csharp
target.visibilityMultiplier = hidden ? 0f : 1f;
```

## Custom movement

`IHorrorMover` is what the state machine drives: `MoveTo`, `Stop`, `HasArrived`, `Reachable` (false when the last
destination cannot be reached, so the enemy searches where it stopped), `TryProject` (snap a search point to the
walkable area), `Turn` and `Tick`. The default uses the `NavMesh Agent` on the enemy, or moves the transform in
straight lines when there is none. Call `SetMover` in `Awake` to use yours.