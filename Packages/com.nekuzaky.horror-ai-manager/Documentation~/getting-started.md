# Getting started

## 1. An enemy

**GameObject > Horror AI > Enemy** creates a capsule with a `NavMesh Agent` and a `Horror Enemy` using the
**Patroller** archetype. Replace the capsule's mesh with your monster later; keep the components.

Its inspector lists what is missing, in plain words, each with a button:

- *There is no NavMesh under this enemy* → **Bake a NavMesh** adds a `NavMesh Surface` to the scene and bakes it.
- *There is no Horror Target in the scene* → **Add a Horror Target to the player** puts one on the object tagged
  `Player` (or the root of the main camera).
- *No patrol route* → **Create a patrol route** adds two waypoints in front of the enemy. Move them, add more with
  **Add a waypoint**.
- *No light is marked for the AI* → **Mark every light** adds a `Horror Light Source` to every point and spot light.

## 2. The player

The `Horror Target` on the player is what enemies look for. Set `Crouching` from your controller:

```csharp
GetComponent<HorrorTarget>().Crouching = Input.GetKey(KeyCode.C);
```

Make noise on footsteps, louder when running:

```csharp
HorrorAI.EmitNoise(transform.position, running ? 10f : 4f, gameObject);
```

Loudness is in meters: how far the noise carries in open space for an enemy with normal hearing.

## 3. Light

Add a `Horror Light Source` to every lamp that should make the player easier to spot, and to the player's
flashlight with **Reveals Its Holder** on: while the flashlight is on, the player can be seen from far away, even in
the dark. Turn lights on and off with `SetOn(bool)` or `Toggle()`, or by enabling the Unity `Light` it follows.

## 4. Scares

Open the enemy's **Events** foldout and hook your sounds, music and jump scares to **On Spotted**,
**On Chase Started**, **On Caught**, **On Lost Track**, **On Gave Up**...

## 5. Watch it think

Press Play and open **Window > Horror AI**. Select the enemy to see its view cone, its suspicion bar, its last known
position and its search area in the Scene view.

## The demo

Import **Greybox Demo** from **Package Manager > Horror AI Manager > Samples** and open `Horror AI Demo.unity`.
Click in the Game view to look around. WASD to move, Shift to run (loud), C to crouch (quiet and low), F for the
flashlight. The enemy turns green on patrol, yellow when alert, orange when it checks something, purple when it
searches and red when it chases. The demo uses the built-in pipeline's Standard material and the Input Manager
(set **Active Input Handling** to **Both** if your project uses the Input System only).