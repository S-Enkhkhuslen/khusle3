# Эхлэх цэс

`Assets/Scenes/StartMenu.unity` нь Build Settings-ийн эхний scene. Unity-ийн Play эхлэх scene-ийг ч үүн дээр тохируулсан.

- **Start** — `SampleScene` тоглоомыг ачаална.
- **Options** — Music, Sound Effects-ийн хэмжээ, Mute, Test Sound. Тохиргоо автоматаар хадгалагдаж, тоглоомын pause цэстэй ижил утга хэрэглэнэ.
- **Help** — Controls болон Mission & Score гэсэн хоёр хуудастай. Доош scroll хийж бүх дүрмийг уншина.
- **Exit** — build дээр тоглоомыг хаана. Editor дээр Play-г зогсооно.

`Esc` нь Options/Help-ээс үндсэн цэс рүү буцаана. `M` нь дууг хааж/нээдэг.

## Файлууд

| Файл | Үүрэг |
| --- | --- |
| StartMenu.cs | Цэсний товч, хуудас, Help, scene ачаалах |
| MenuAudio.cs | Цэсний хөгжим, товчны дуу, дууны тохиргоо |
| MenuBackdrop.cs | Хадгалсан тоосны Image объектуудын хөдөлгөөн |
| GameUI.cs | Тоглоомын нийтлэг өнгөнүүд |

Нэр болон тайлбарыг солих бол Hierarchy дахь **Start Menu UI → Main Page → Main Menu** доторх Text объектуудыг засна. Start өөр scene ачаалах ёстой бол Start Menu UI объектын Inspector дахь **Game Scene** утгыг сольж, тэр scene-ээ Build Settings-д нэмнэ. Дэлгэрэнгүйг хажуугийн `README_UI_MN.md`-ээс үзнэ.

