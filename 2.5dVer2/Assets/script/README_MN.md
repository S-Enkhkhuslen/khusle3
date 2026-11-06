# Кодоо хаанаас унших вэ?

Файл бүр нэг үндсэн ажилтай. Эхлээд `Update`, `LateUpdate` эсвэл `Start`-аас нь уншаад, тэндээс дуудагдсан жижиг функц рүү орно.

| Хэсэг | Гол файл | Юу хариуцдаг вэ? |
| --- | --- | --- |
| Тоглогч | Player/Controller.cs | Алхах, эргэх, jetpack болон sprint-ийг хамт тооцох |
| Буудах | Gun/PlayerShoot.cs, Bullet.cs | Input, projectile үүсгэх болон онох damage |
| Oxygen pickup | O2/Tank.cs, ChangeO2.cs | Oxygen нөхөх, зарцуулалтын upgrade авах |
| Нисэх / sprint | Player/Jetpack.cs, Sprint.cs | Input болон oxygen зарцуулалт |
| Хөдөлгөөний effect | Player/CharacterEffects.cs | Алхалтын тоос, jet effect, анимацтай таарсан хөлийн дуу |
| Улаан анивчилт | Player/DamageFlash.cs | Цохилтын өнгийг түр өөрчлөөд буцаах |
| Enemy | enemy/MeleeEnemyAI.cs, RangedEnemyAI.cs, FlyingEnemyAI.cs | Тухайн enemy-ийн дайрах шийдвэр |
| Газрын enemy-ийн хөдөлгөөн | enemy/EnemyNavigation.cs | Давтагдсан хөөх, зогсох, хэвтээ эргэлт |
| Generator | enemy/EnemyGenerator.cs | E дарахад нэг удаа enemy үүсгэх |
| Generator-ийн харагдац | UI/GeneratorDisplay.cs | Хүрээний тойрог болон E popup |
| Газрын өндөр | GroundUtility.cs | Terrain-ийн өндөр, шаардлагатай үед raycast |
| Дуу | GameAudio.cs | Effect, хөдөлгөөний дуу, хөгжим, oxygen анхааруулга |
| Дууны файлууд | GameSounds.cs | Audio clip-үүдийг ачаалах, suit alarm бэлдэх |
| Pause | UI/GamePauseMenu.cs, AudioOptionsUI.cs | Хадгалсан pause panel, audio slider-уудын утга болон үйлдэл |
| Pause-ээс буцах | UI/PauseState.cs | Өмнөх input, camera, cursor, хугацааны тохиргоог сэргээх |
| UI загвар | UI/GameUI.cs | Нийтлэг өнгөнүүд; харагдац нь scene/prefab дотор хадгалагдана |
| HUD | UI/GameInterface.cs, PlayerHUDView.cs | Scene доторх UI объектуудын холбоос, утга шинэчлэх |
| Амь / oxygen | UI/StatusBarUI.cs, PlayerHUD.cs | Хадгалсан Image болон Text объектод үзүүлэлт дамжуулах |
| Оноо | UI/GameScore.cs | Enemy, combo, oxygen бонусын тооцоо |
| Төгсгөлийн дэлгэц | UI/GameEndScreen.cs | Задаргаа → NEXT → Top 10 |
| Хадгалалт | UI/ScoreLeaderboard.cs | Хамгийн өндөр 10 оноог энэ компьютерт хадгалах |

`GameAudio.LateUpdate()`-ийн жижиг функцүүдийг дарааллаар нь уншихад дууны урсгал харагдана. UI-ийн өнгийг солих бол `GameUI`-ийн эхний өнгөнүүдийг өөрчилнө.

`?.` гэдэг нь объект байгаа үед нь үйлдлийг дуудах богино бичлэг. `[SerializeField]` талбарууд Inspector-оос утгаа авдаг тул prefab болон scene дээрх тохиргоонуудтай холбоотой. Үзэгдэх байдлыг цэгцлэхдээ эдгээр талбарын нэрийг хадгалсан.

Projectile-ийн мөргөлдөөн, flying enemy-ийн саад тойрох шалгалтууд нь хана нэвтрэх болон terrain-д шигдэхээс хамгаалдаг тул тусдаа функцүүд хэвээр байгаа.

UI-г Unity дотор засах замуудыг `UI/README_UI_MN.md`-ээс үзнэ. Товч, slider-ийн үйлдлүүд Inspector дээр хадгалагдсан.
