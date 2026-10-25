# Archetypes and presets

An enemy's tuning lives in assets, so several enemies share it and you can tweak it in Play Mode.

- **Perception Profile** (**Create > Horror AI > Perception Profile**): sight, light, hearing, memory and the
  perception interval. Its inspector summarises the result: how long a lit or dark target takes to be spotted, how far
  a footstep is heard in the open and through a wall.
- **Behavior Profile** (**Create > Horror AI > Behavior Profile**): thresholds, durations, speeds, search radius and
  catch distance. It warns when *Calm Threshold* is not below *Alert Threshold*.
- **Horror Archetype** (**Create > Horror AI > Archetype**): one of each, plus a description. This is what a
  `Horror Enemy` uses.

Every field has a tooltip in plain words. See [Perception](perception.md) and [Behavior](behavior.md) for what they
do.

## Patroller

`Packages/com.nekuzaky.horror-ai-manager/Runtime/Presets/Patroller.asset`: walks its route at a steady pace,
notices a lit player quickly and a player in the dark slowly, goes to check noises, searches the area for 18 seconds,
then gives up and resumes its round. The all-round enemy to start with.

To change it, duplicate the three Patroller assets into your project (the package folder is read-only when installed
from Git) and edit the copies.

More archetypes are planned: Stalker, Blind, Statue and Ambusher.