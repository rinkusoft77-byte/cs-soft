# O'rnatish qo'llanmasi (noldan)

`VisionAssist.dll` — bu mustaqil dastur emas. Uni CS2 **serveri** yuklaydi, buning
uchun serverda ikkita narsa turishi kerak:

```
CS2 dedicated server
  └── Metamod:Source          ← o'yin dvigateliga ulanadi
        └── CounterStrikeSharp ← C# plugin'larni yuklaydi
              └── VisionAssist.dll  ← bizning plugin
```

Bu zanjirning birortasi yo'q bo'lsa — **hech nima ko'rinmaydi**, xato ham
chiqmaydi. Quyidagi tartibda qiling.

---

## 0. Muhim: o'yin klientiga o'rnatmang

Metamod'ni CS2 o'yiningizning o'z papkasiga (`steamapps\common\Counter-Strike
Global Offensive`) o'rnatib, "offline with bots" rejimida sinash **tavsiya
etilmaydi** — u holda uchinchi tomon kutubxonasi `cs2.exe` jarayoniga
yuklanadi, bu esa VAC nuqtai nazaridan xavfli.

To'g'ri yo'l: **alohida dedicated server** o'rnatish va unga `connect` bilan
ulanish. O'yin klientingiz toza qoladi. Quyida shu yo'l yozilgan.

---

## 1. Dedicated server o'rnatish (SteamCMD)

[SteamCMD](https://developer.valvesoftware.com/wiki/SteamCMD) ni yuklab oling,
so'ng:

```bat
steamcmd +force_install_dir C:\cs2-server +login anonymous +app_update 730 validate +quit
```

Yuklanish ~40 GB, biroz vaqt oladi.

Serverni ishga tushirish (hozircha sinov uchun):

```bat
C:\cs2-server\game\bin\win64\cs2.exe -dedicated +map de_dust2 +sv_lan 1
```

Konsol oynasi ochilib, xarita yuklanishi kerak.

---

## 2. Metamod:Source

1. [sourcemm.net](https://www.sourcemm.net/downloads.php/?branch=dev) dan
   **CS2 uchun dev build** ni yuklang (Windows versiyasi).
2. Arxiv ichidagi `addons` papkasini `C:\cs2-server\game\csgo\` ichiga ko'chiring.
   Natijada `C:\cs2-server\game\csgo\addons\metamod\` bo'lishi kerak.
3. **Eng ko'p unutiladigan qadam:** `C:\cs2-server\game\csgo\gameinfo.gi`
   faylini bloknotda oching, `SearchPaths` blokini toping va `Game_LowViolence`
   qatoridan keyin, `Game csgo` dan **oldin** shu qatorni qo'shing:

```
			Game	csgo/addons/metamod
```

Natijada shunday ko'rinadi:

```
		SearchPaths
		{
			Game_LowViolence	csgo_lv
			Game				csgo/addons/metamod
			Game				csgo
			Game				core
		}
```

4. Serverni qayta ishga tushiring va konsolga yozing:

```
meta list
```

Metamod ro'yxatni chiqarsa — ishladi. `Unknown command` desa — 3-qadam
bajarilmagan yoki papka noto'g'ri joyda.

---

## 3. CounterStrikeSharp

1. [Relizlar sahifasi](https://github.com/roflmuffin/CounterStrikeSharp/releases)
   dan **`counterstrikesharp-with-runtime-build-XXX-windows.zip`** ni yuklang.

   > `with-runtime` versiyasini oling — u .NET 8 runtime'ni o'zi bilan olib
   > keladi. Oddiy (`without-runtime`) versiya kompyuterda .NET 8 o'rnatilgan
   > bo'lishini talab qiladi.

2. Arxivdagi `addons` papkasini yana `C:\cs2-server\game\csgo\` ichiga ko'chiring
   (mavjud `addons` bilan qo'shilib ketadi). Natijada
   `C:\cs2-server\game\csgo\addons\counterstrikesharp\` paydo bo'ladi.

3. Serverni qayta ishga tushirib, konsolga:

```
css_plugins list
```

Ro'yxat chiqsa — CounterStrikeSharp ishlayapti.

---

## 4. VisionAssist

Visual Studio'da **Ctrl+Shift+B** bosgach quyidagi papkada 4 ta fayl paydo
bo'ladi:

```
src\VisionAssist\bin\Release\net8.0\
    VisionAssist.dll
    VisionAssist.deps.json
    VisionAssist.runtimeconfig.json
    VisionAssist.pdb
```

**To'rttasini ham** shu papkaga ko'chiring:

```
C:\cs2-server\game\csgo\addons\counterstrikesharp\plugins\VisionAssist\
```

> Faqat `.dll` ni ko'chirsangiz plugin yuklanmaydi — `.deps.json` va
> `.runtimeconfig.json` ham kerak.

Har safar qo'lda ko'chirmaslik uchun repo ildizida `Local.props` fayli yarating
(`Local.props.example` dan nusxa oling):

```xml
<Project>
  <PropertyGroup>
    <CS2ServerPath>C:\cs2-server</CS2ServerPath>
  </PropertyGroup>
</Project>
```

Shundan keyin Ctrl+Shift+B fayllarni o'zi kerakli joyga tashlaydi.

Server konsolida:

```
css_plugins load VisionAssist
css_vision_status
```

`css_vision_status` holatni chiqarsa — plugin ishlayapti.

---

## 5. Serverga ulanish

O'yin klientida konsolni yoqing (Settings > Game > Enable Developer Console),
so'ng `~` bosib:

```
connect 127.0.0.1:27015
```

Spawn bo'lgach modellar rangli bo'lishi kerak.

---

## 6. Admin huquqi (buyruqlar uchun)

Ranglar, kontur, radar va HUD **avtomatik** ishlaydi — admin bo'lish shart emas.
Lekin `!theme`, `!tintcolor` kabi sozlash buyruqlari uchun admin flagi kerak.

`C:\cs2-server\game\csgo\addons\counterstrikesharp\configs\admins.json`:

```json
{
  "Rinku": {
    "identity": "76561198000000000",
    "flags": ["@css/root"],
    "immunity": 100
  }
}
```

`identity` — sizning SteamID64 raqamingiz ([steamid.io](https://steamid.io) dan
oling). Keyin `css_admins_reload` yoki serverni qayta ishga tushiring.

---

## Muammoni topish (tartib bilan)

Har bir qadamni server konsolida tekshiring — qayerda to'xtasa, muammo o'sha
yerda.

| # | Konsolga yozing | Kutilgan natija | Chiqmasa nima qilish |
|---|---|---|---|
| 1 | `meta list` | Metamod ro'yxati | `gameinfo.gi` tahrirlanmagan (2-qadam, 3-band) |
| 2 | `css_plugins list` | Plugin'lar ro'yxati | CounterStrikeSharp o'rnatilmagan yoki `without-runtime` versiya olingan |
| 3 | ro'yxatda `VisionAssist` bormi | bor | Fayllar noto'g'ri papkada, yoki `.deps.json` / `.runtimeconfig.json` ko'chirilmagan |
| 4 | `css_plugins load VisionAssist` | `Loaded plugin` | Chiqqan xatoni o'qing |
| 5 | `css_vision_status` | Sozlamalar ro'yxati | Plugin yuklanmagan |
| 6 | o'yinda spawn bo'ling | Modellar rangli | `css_vision_status` da `Model rangi: off` bo'lsa — `css_tint on` |

### Loglar

Eng aniq ma'lumot shu yerda:

```
C:\cs2-server\game\csgo\addons\counterstrikesharp\logs\log-YYYYMMDD.txt
```

Faylning oxirini oching — plugin yuklanmagan bo'lsa sababi yozilgan bo'ladi.

### Tez-tez uchraydigan sabablar

**`gameinfo.gi` tahrirlanmagan.** Eng ko'p uchraydigani. Metamod umuman
yuklanmaydi, `meta list` ishlamaydi.

**`without-runtime` versiya olingan.** CounterStrikeSharp yuklanmaydi, chunki
kompyuterda .NET 8 runtime yo'q. `with-runtime` ni oling.

**CounterStrikeSharp versiyasi eski.** Plugin `MinimumApiVersion 305` talab
qiladi. Server konsolida CSSharp o'z versiyasini yuklanishda yozadi; log faylida
`requires API version` degan xabar bo'lsa — CounterStrikeSharp'ni yangilang.

**Faqat `.dll` ko'chirilgan.** `.deps.json` va `.runtimeconfig.json` ham kerak.

**Papka nomi noto'g'ri.** Papka aynan `VisionAssist` deb nomlanishi kerak:
`plugins\VisionAssist\VisionAssist.dll`.

**Boshqa serverga ulangansiz.** Plugin faqat siz o'rnatgan serverda ishlaydi.
Rasmiy matchmaking yoki begona serverlarda hech qanday effekt bo'lmaydi.
