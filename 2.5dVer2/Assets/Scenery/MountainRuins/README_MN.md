# Уулын балгас

`SampleScene > Mountain Outpost Ruins` дотор дөрвөн генераторын тусдаа outpost байна.
Бүтэн `Outpost` объектыг зөөх/эргүүлэхэд дүрс, collider, NavMeshObstacle хамт шилжинэ.
Генератороос тусдаа объект тул генераторыг ашигласан ч балгас үлдэнэ.

- Prefab-ууд: `Assets/Scenery/MountainRuins/Prefabs/`.
- Тасарсан ханын mesh, нэгтгэсэн барилгын mesh: `Meshes/`.
- Хоёр бетон material: `Materials/`. Төмөр, зэв, улбар тэмдэглэгээ нь хуучин Scenery material-уудыг ашиглана.
- Дээвэр ихэнхдээ нээлттэй; төвийн generator/rocket хэсэг болон өргөн орцууд чөлөөтэй.
- Хана, том хэлтэрхий нь тусдаа collider болон stationary carving NavMeshObstacle-тай. Барилгын төвийг бүхэлд нь бөглөх obstacle байхгүй.
- Нэг outpost-ийн дүрс нэг MeshRenderer-т нэгтгэгдсэн. Тусдаа ханын collider-ийг зөөх нь нэгтгэсэн дүрсийг өөрчлөхгүй; хэсгүүдийн загварыг authoring source-оор өөрчилнө.

Editor source: `Assets/Editor/OutpostRuinsBuilder.cs`. `Tools > World > Add Mountain Outpost Ruins`
нь балгасгүй scene-д үүсгэнэ; давхар нэмж үүсгэхгүй. Эдгээр барилгыг үүсгэдэг runtime script байхгүй.
