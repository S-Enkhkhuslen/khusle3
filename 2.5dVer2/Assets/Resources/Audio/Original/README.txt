Project audio source record — updated 2026-11-03.

Five original synthesized cues (no third-party samples):
escape_success.wav, mission_failed.wav, desert_wind.wav, combat_pulse.wav,
melee_swing.wav.
The old combat_pulse.wav is retained as a legacy cue; combat now uses combat_music.wav.
combat_music.wav: Fast fight / battle music by Ville Nousiainen, loop edit by XCVG.
CC0; replaces the previous synthesized 112 BPM cue. Peak gain and 3 ms loop-edge
de-click only; no speed/pitch edit.
See combat_music.License.txt for credits and source links.

sprint_scrape.wav: continuous four-second friction/gravel bed derived from
Kenney Impact Sounds footstep_snow_000/001/002. CC0; originals and license in ../Foley.
Source: https://kenney.nl/assets/impact-sounds
Edits: overlapping 85 ms grains, randomized grain placement/levels, frequency
shaping and soft limiting. No periodic footfall rhythm or tonal motor layer.
This replaces the prior synthesized sprint scrape.

Generators archived in the Codex workspace:
work/audio-polish-20261102/prepare-assets.py (initial synthesized cues)
work/movement-combat-audio-20261103/prepare-audio.py (previous synthesized combat music and revised scrape).
work/intense-battle-20261103/replace-battle.py (current downloaded battle music).
