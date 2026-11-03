# Game audio

34 selected assets are included. No Inspector clip assignments are needed:
GameAudio is created when PlayerHealth wakes and loads these Resources.

## Controls

- **Esc** opens audio settings and pauses gameplay. Music continues quietly;
  movement, aiming, camera look, jetpack and oxygen alerts pause. Esc/Resume
  returns to gameplay.
- The settings panel has independent **Music** and **Sound Effects** sliders.
- **M** toggles all audio, including the suit warning. Settings persist locally
  through PlayerPrefs, including scene restarts.
- Default music/effects levels are 22% / 80%. The Game Audio runtime Inspector
  additionally exposes Warning Volume and Ambience Volume.

## Sounds and music

| Cue | Source |
| --- | --- |
| Exploration music | yd's My Very Own Dead Ship, a 226-second space background track |
| Desert ambience | Original synthesized wind |
| Generator battle music | Ville Nousiainen’s Fast fight / battle music (XCVG loop edit), CC0; driving action track with a smooth crossfade |
| Player / enemy shooting | Kenney laserLarge / laserSmall variants; enemy shots are positional |
| Projectile impact | Kenney impactMetal variants |
| Player damage | Kenney impactPunch_medium variants |
| Dust / hard-surface footsteps | Kenney footstep_snow / footstep_concrete variants |
| Landing | Lower-pitched hard-surface boot impact |
| Melee windup | Original short air-swish |
| Enemy death | Kenney low-frequency explosion |
| Grounded sprint | Continuous friction/gravel scrape adapted from CC0 boot foley; no player footfalls during sprint; fades on stop, takeoff and pause |
| Jetpack / sprint | Kenney thruster ignition plus an engine loop, with fades and sprint pitch |
| Low oxygen | Original two-pulse suit alarm: below 25%, then faster below 10% |
| Oxygen / efficiency pickup | Kenney energy/latch cues; higher pitch for an upgrade |
| Generator / rocket ready | Kenney energy and mechanism cues |
| Rocket launch | NASA STS-131 real launch roar, gently EQ’d with a seamless loop and unscaled fades |
| Death / successful escape | Original descending / ascending musical cues |

Music crossfades to the battle track when generator-spawned enemies are nearby,
returns after all tracked enemies die or the player escapes beyond 70 m, and
re-enters battle inside 55 m. Multiple waves are tracked together. Empty shooting
does not trigger battle music. Pausing keeps the current mix quietly playing;
launch ducks music and endings fade both tracks out.
The background track has a 1.25 output gain (about +1.9 dB); saved slider and mute settings are respected.
Player footsteps follow the actual walk Animator contacts at 25% and 75% of each
cycle, require grounded travel, and are about 28% quieter. Sprint suppresses both
new footfalls and the last footfall tail. Enemy footsteps retain their distance
cadence and are culled for distant enemies. Metal/stone props use boot
foley without emitting desert dust on their upper surfaces.

20 effect voices are pooled; seven dedicated channels handle music, ambience,
battle music, jetpack, sprint ground scrape, oxygen warning and rocket audio. Important cues have
higher priority than enemy footsteps/impacts. Sources have no Doppler shift.
Audio variation uses System.Random so it cannot change gameplay spawn randomness.
Long music streams; short cues are preloaded and decoded for quick playback.

Sources belong to the scene-local audio service, so projectile/generator/pickup
destruction and boarding do not cut off their cues. DSP/unscaled timing keeps
launch audio working while the world is frozen. Restart replaces the service
and stops its old loops.

## Sources and licenses

- **NASA — Discovery STS-131: Sound of Launch:** https://www.nasa.gov/historical-sounds/
  Edited real launch recording; NASA media usage guidelines apply. Credit and edits in Rocket/Source.txt.

- **Kenney — Sci-fi Sounds:** https://kenney.nl/assets/sci-fi-sounds
  16 unmodified OGG selections; CC0. Original license in SciFi/License.txt.
- **Kenney — Impact Sounds:** https://kenney.nl/assets/impact-sounds
  9 unmodified OGG selections; CC0. Original license in Foley/License.txt.
- **yd — Background space track / My Very Own Dead Ship:**
  https://opengameart.org/content/background-space-track
  Unmodified OGG from projects.zip; CC0. Source/license record in Music/License.txt.
- **Battle music — Ville Nousiainen, loop edit by XCVG:**
  https://opengameart.org/content/fast-fight-battle-music-looped
  CC0; source and license in Original/combat_music.License.txt.
- **Project-generated cues:** five synthesized WAV files in Original. sprint_scrape.wav is an additional edited/granulated CC0
  Kenney Impact Sounds derivative. The suit warning is synthesized by GameAudio.
  See Original/README.txt for provenance.

CC0 reference: https://creativecommons.org/publicdomain/zero/1.0/
Credits above are retained voluntarily for the downloaded CC0 works.
