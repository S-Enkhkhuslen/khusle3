# Planet scenery

The placed scenery is saved in `Assets/Scenes/SampleScene.unity`, under
`Planet Wreckage`: Crashed Ships, Station Ruins, Rock Fields and Scattered Debris.

Move, rotate or remove whole prefab instances in that group to adjust the map.
Drag more copies from `Assets/Scenery/Prefabs` into the scene when needed.
The positions stay fixed between Play sessions.

Each prefab uses a combined mesh, shared materials, solid colliders and a
stationary carving NavMeshObstacle. Moving its whole root keeps its visible
mesh and colliders aligned. These objects use the Default layer, already
included in the current weapon and enemy sight masks. There are no scenery
scripts running during gameplay.

The Editor-only source is `Assets/Editor/WorldSceneryBuilder.cs`. Its menu is
`Tools > World > Add Planet Wreckage`. It refuses to add a duplicate group,
and requires a saved SampleScene. Placement protects the player start,
existing enemies, generator spawn zones, oxygen spawn zones and NavMesh
corridors between the start and generators.

Meshes and materials are generated locally for this project; no external art
downloads or packages are needed.
