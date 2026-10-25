# Integrations

Both integrations live in their own assembly and compile only when the other package is installed
(`versionDefines` in the assembly definition). Nothing is needed otherwise.

## Resonance: enemies hear your sound events

With [Resonance](https://github.com/Nekuzaky/Resonance) installed, add a **Resonance Noise Bridge**
(**Add Component > Horror AI > Resonance Noise Bridge**) to any object in the scene. It listens to Resonance's
`SoundStarted` event and turns each sound that starts into a Horror AI noise at the sound's position, with the
emitter as the source (so a monster ignores its own growls).

Loudness comes from a **Resonance Noise Map** (**Create > Horror AI > Resonance Noise Map**), or from a default when
there is no map:

- **Default:** `loudness = volume × max distance × scale` (scale 0.5): a full-volume footstep that fades out at 20 m
  is heard up to 10 m. 2D sounds (max distance 0: music, UI) and sounds quieter than *Minimum Volume* are not heard.
- **Map entries** give a sound its own loudness in meters, or mark it **silent** (the monster's voice, the music).
  Sounds not listed use the default, unless *Use Default* is off.

Resonance stays AI-agnostic: it only raises `SoundStarted`; the mapping lives in this package. The bridge needs
**Resonance 0.6.0 or newer**, which adds `ResonanceEngine.SoundStarted` (`SoundStartInfo`: handle, event, emitter,
position, volume, max distance). With an older Resonance the integration is not compiled.

To try it in the development project, add Resonance to `Packages/manifest.json`
(`"com.nekuzaky.resonance": "file:<path to Resonance>/Packages/com.nekuzaky.resonance"` or its Git URL), press Play
in a scene with a bridge, and watch the noise rings in the Scene view.

## Hindsight: rewind what the enemy was thinking

With [Hindsight](https://github.com/Nekuzaky/Hindsight) installed, nothing to set up:

- every change of mind becomes a **bookmark** on Hindsight's timeline, for example
  `Ghoul: Investigate > Search (reached the last known position)`;
- every enemy gets a **Hindsight Probe** in the editor, whose tracked values are drawn in Hindsight's graph:
  *AI state*, *Suspicion*, *Sees target*, *Target lit*, *Memory certainty* and *Last known position*.

Bookmark labels are built once per enemy when it is enabled (with the name it has then) and reused, so a change of
mind allocates nothing. The integration installs itself after the Play Mode reset, so it also works with domain reload
disabled. Turn either off from code with `HorrorHindsight.Bookmarks = false` or `HorrorHindsight.TrackValues = false`. The
bookmark calls are removed from release builds like every `Hindsight.Bookmark` call.