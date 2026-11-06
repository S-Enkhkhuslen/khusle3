# UI-г Unity дотор хаанаас засах вэ?

Бүх харагдац нь scene эсвэл prefab-д хадгалсан **Canvas / Image / Text / Button / Slider / Scroll View** объект болсон. `OnGUI`-ээр зурахгүй. Скриптүүд нь эдгээр объектын утга, харагдах төлөвийг л шинэчилнэ.

## Start Menu

`Assets/Scenes/StartMenu.unity`-г нээгээд Hierarchy дотор **Start Menu UI**-г дэлгэнэ.

- **Backdrop** → Landscape: арын зураг. Drifting Dust: тоосны Image объектууд.
- **Main Page → Main Menu**: нэр, тайлбар, Menu Buttons дотор Start / Options / Help / Exit.
- **Options Page → Audio Options**: дууны slider, Mute, Test Sound, Back.
- **Help Page → Help**: Controls / Mission & Score таб, тексттэй scroll хэсгүүд.
- **Footer**: доод мөрийн тайлбар.

Нуугдсан хуудсыг харахдаа Inspector-ын дээд талын checkbox-оор идэвхжүүлж болно. Play эхлэхэд StartMenu скрипт үндсэн хуудсыг харуулж, бусдыг нууна.

## Тоглоомын UI

`Assets/Scenes/SampleScene.unity` → **Game UI**. Энэ нь `Assets/Resources/UI/GameInterface.prefab`-ийн instance.

- **HUD**: Health Bar, Oxygen Bar, Score Panel, Combo Timer.
- **Pause UI → Pause Screen**: dim background, Card, Audio Controls, Resume.
- **Rocket UI → Rocket Prompt**: E заавар, пуужингийн зай, launch progress.
- **Results UI → Mission Report**: Title, Message, Total Panel, Breakdown, Top 10, Next Replay Button.

Pause болон Mission Report нь анх нуугдсан байдаг. Тэдгээрийг түр идэвхжүүлж харагдацыг засаж болно. Тоглоомын өгөгдөл, цэсний нээлттэй эсэхийг Play дээр скрипт удирдана.

## Дайсны HP болон generator-ийн popup

`Assets/Resources/UI/EnemyHealthBar.prefab` болон `GeneratorPrompt.prefab`-ийг давхар дарж Prefab Mode дээр нээнэ. Spawn хийхдээ эдгээр хадгалсан prefab-ийг clone хийдэг. Бүх enemy/generator-ийн UI-д нэг өөрчлөлт үйлчилнэ.

## Өөрчлөх зүйлс

- Байрлал, хэмжээ: **Rect Transform**.
- Өнгө, зураг: **Image / Raw Image**.
- Бичиг, font, хэмжээ: **Text**.
- Товчны үйлдэл: **Button → On Click ()**.
- Slider-ийн утга холбох: **Slider → On Value Changed (Single)**.

HP/oxygen bar-ийн хэвийн өнгө нь **Status Bar UI → Normal Color** талбарт байна. Fill-ийн хэмжээ болон тоон утгыг скрипт тоглоомын бодит өгөгдлөөс шинэчилдэг.

Inspector дээр холбоотой component талбаруудыг устгахгүйгээр харагдацыг өөрчилнө. Дууны тохиргоо, score хадгалалт, combo болон oxygen бонусын дүрэм хэвээр.
