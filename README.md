# VisionAssist — CS2 server plugini

O'zingiz admin bo'lgan CS2 serveri uchun **ko'rinuvchanlik va qulaylik** plugini.
Ko'zi yaxshi ko'rmaydigan o'yinchi uchun yozilgan: modellarni ajratib turadigan
rangga bo'yaydi, kontur chizadi, katta HUD chiqaradi va radarni to'ldiradi.

**Devor ortidan ko'rish (wallhack) yo'q.** Kontur faqat ko'zingizga allaqachon
ko'rinib turgan modelning chetiga chiziladi, model rangi esa shunchaki bo'yoq —
o'yinchi ilgari ko'rinmagan joyda ko'rinib qolmaydi. Bu ataylab shunday: through-walls
rejimi kodda bloklangan (`OutlineController.ThroughWallsGlowType`).

Hammasi **CounterStrikeSharp** orqali server tomonida ishlaydi:

- o'yin fayllariga, xotirasiga yoki mijozga tegilmaydi — VAC bilan aloqasi yo'q;
- faqat siz plugin o'rnatgan serverda ishlaydi;
- effektlar serverdagi hamma uchun bir xil — bitta o'yinchiga ustunlik beradigan
  "faqat menga ko'rinsin" rejimi yozilmagan.

---

## Funksiyalar

### Ko'rinuvchanlik

| Funksiya | Tavsif |
|---|---|
| **Model rangi** | O'yinchi modelini butunlay bo'yaydi. T va CT uchun alohida rang, shaffoflik (alpha) sozlanadi |
| **Kontur** | Model chetiga rangli chiziq — silueti fon bilan qo'shilib ketmaydi. Masofasi sozlanadi |
| **Jon bo'yicha rang** | Jon kamayganda model rangi qizilga surilib boradi — yaradorni bir qarashda bilasiz |
| **Nishonlarni belgilash** | Bomba, defuse kit va garovga olinganlarni ajratib turadigan rangga bo'yaydi |
| **To'liq radar** | Barcha o'yinchilar radarda ko'rinadi |

### 7 ta tayyor rang to'plami (theme)

Rang tanlash — eng qiyin qismi: rang ham bir-biridan, ham xarita fonidan
(qum, beton) ajralib turishi kerak. Tayyor to'plamlar:

| Nom | Ranglar | Kimga |
|---|---|---|
| `highcontrast` | sariq / moviy | eng yorqin, standart |
| `neon` | pushti / yashil | maksimal ajralish |
| `deuteranopia` | to'q sariq / ko'k | qizil-yashil daltonizm |
| `protanopia` | qahrabo / osmoniy | qizilni sust ko'radiganlar |
| `tritanopia` | qizil / firuza | ko'k-sariq daltonizm |
| `soft` | pastel pushti / och ko'k | yorug'likka sezgirlik |
| `mono` | oq / qora | faqat yorqinlik kontrasti |

### Katta HUD (har bir o'yinchi uchun alohida)

Ekran o'rtasida yirik yozuv: **jon, zirh, raund vaqti, bomba taymeri, tirik
o'yinchilar soni**. Har kim o'zi uchun yoqadi/o'chiradi va uch xil o'lchamdan
birini tanlaydi (`s` / `m` / `l`).

### Uch til

O'zbekcha, ruscha, inglizcha. Har bir o'yinchi o'zi tanlaydi (`!lang uz`), tanlovi
SteamID bo'yicha saqlanadi.

### Menyu

`!vision` — chatda menyu ochiladi. Adminlarga qo'shimcha bo'lim ko'rinadi.

### Mijoz sozlamalari

`!bigradar` — radar va HUD kattalashtiruvchi client cvar'larni qo'llaydi.
`!cfg` — tavsiya etilgan barcha sozlamalarni (radar + prisel) konsolga chiqaradi,
`autoexec.cfg` ga nusxalash uchun.

---

## Nima qila olmaydi

| Narsa | Holati |
|---|---|
| Model rangini o'zgartirish | ✅ |
| Kontur (ko'rinib turgan modelga) | ✅ |
| Devor ortidan ko'rish | ❌ **ataylab yo'q** |
| Hamma radarda | ✅ |
| 2D quti (box) va ism yozuvi | ❌ mijoz tomonida chiziladi, server chiza olmaydi |
| Faqat boshni alohida bo'yash | ❌ model bir butun bo'yaladi |
| Faqat bitta o'yinchiga ko'rinadigan effekt | ❌ ataylab yo'q |

---

## Talablar

- CS2 dedicated server (o'zingizniki)
- [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master) (CS2 uchun dev build)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) **v1.0.305+** (`with-runtime` versiyasi)
- Qurish uchun: **Visual Studio 2026** (".NET desktop development" workload bilan)
  yoki .NET 8/9/10 SDK

## Visual Studio 2026 da ishlash

1. `VisionAssist.sln` faylini oching (ikki marta bosing).
2. **Build > Build Solution** yoki **F6**.

Boshqa hech narsa sozlash shart emas — NuGet paketi avtomatik yuklanadi,
`.NET 8` maqsad platformasi `Directory.Build.props` da yozilgan. Visual
Studio 2026 bilan kelgan `.NET 10 SDK` bu loyihani muammosiz quradi.

`Solution Items` papkasida `README.md`, `.editorconfig` va sozlama fayllari
ko'rinib turadi.

### Build qilgach avtomatik serverga ko'chirish

`Local.props.example` faylidan nusxa olib, uni **`Local.props`** deb nomlang va
o'z yo'lingizni yozing:

```xml
<Project>
  <PropertyGroup>
    <CS2ServerPath>C:\cs2-server</CS2ServerPath>
  </PropertyGroup>
</Project>
```

Endi har safar **F6** bosganingizda DLL to'g'ridan-to'g'ri
`...\addons\counterstrikesharp\plugins\VisionAssist\` papkasiga tushadi —
qo'lda ko'chirish kerak emas.

To'g'ridan-to'g'ri plugin papkasini ko'rsatmoqchi bo'lsangiz `CS2ServerPath`
o'rniga `CS2PluginsPath` ishlating. Yo'l ko'rsatilmasa, ko'chirish qadami
o'tkazib yuboriladi va build baribir muvaffaqiyatli tugaydi.

`Local.props` git'ga tushmaydi — kompyuteringizdagi yo'llar repozitoriyga
yozilmaydi.

> **Muhim:** server ishlab turganda DLL band bo'ladi va ko'chirish o'tmaydi.
> Avval `css_plugins unload VisionAssist` qiling, keyin build qiling, so'ng
> `css_plugins load VisionAssist`.

## Qurish (buyruq satridan)

```bash
dotnet build VisionAssist.sln -c Release
```

Serverga bir marta ko'chirish uchun:

```bash
dotnet build VisionAssist.sln -c Release -p:CS2ServerPath=/path/to/cs2-server
```

Qo'lda ko'chirmoqchi bo'lsangiz, `src/VisionAssist/bin/Release/net8.0/` ichidagi
`.dll`, `.deps.json` va `.runtimeconfig.json` fayllarini quyidagi papkaga
tashlang:

```
csgo/addons/counterstrikesharp/plugins/VisionAssist/
```

Keyin serverni qayta ishga tushiring yoki konsolda:

```
css_plugins load VisionAssist
```

Birinchi yuklanishda konfiguratsiya yaratiladi:

```
csgo/addons/counterstrikesharp/configs/plugins/VisionAssist/VisionAssist.json
```

O'yinchilarning shaxsiy sozlamalari `plugins/VisionAssist/player_prefs.json`
faylida saqlanadi.

---

## Buyruqlar

Chatda `!`, konsolda `css_` bilan (`!theme` = `css_theme`).

### Hamma uchun

| Buyruq | Vazifasi |
|---|---|
| `!vision` | Sozlamalar menyusi |
| `!hud [on/off]` | Katta HUD'ni yoqish/o'chirish (o'zingiz uchun) |
| `!hudsize <s/m/l>` | HUD yozuv o'lchami |
| `!lang <uz/ru/en>` | Til |
| `!bigradar` | Radarni kattalashtiruvchi sozlamalarni qo'llash |
| `!cfg` | Tavsiya etilgan sozlamalarni konsolga chiqarish |
| `!colors` | Rang nomlari ro'yxati |
| `!themes` | Rang to'plamlari ro'yxati |
| `!vision_status` | Hozirgi holat |

### Admin (`@css/generic`)

| Buyruq | Vazifasi | Misol |
|---|---|---|
| `!theme <nom>` | Rang to'plamini qo'llash | `!theme neon` |
| `!tint <on/off>` | Model bo'yash | `!tint on` |
| `!tintcolor <t/ct/all> <rang>` | Model rangi | `!tintcolor t #FFEB3C` |
| `!tintalpha <1-255>` | Model shaffofligi | `!tintalpha 255` |
| `!tinthealth <on/off>` | Jon bo'yicha rang | `!tinthealth on` |
| `!outline <on/off>` | Kontur | `!outline on` |
| `!outlinecolor <t/ct/all> <rang>` | Kontur rangi | `!outlinecolor all cyan` |
| `!outlinerange <son>` | Kontur masofasi | `!outlinerange 4000` |
| `!radar <on/off>` | To'liq radar | `!radar on` |
| `!highlight <on/off>` | Bomba/garov belgilash | `!highlight on` |
| `!serverhud <on/off>` | HUD'ni butun server uchun | `!serverhud on` |
| `!vision_reload` | JSON'ni diskdan qayta o'qish | |

Buyruq bilan qilingan o'zgarish JSON'ga yoziladi — map o'zgarsa ham saqlanadi.

### Rang formatlari

- Nom: `magenta`, `pink`, `red`, `orange`, `yellow`, `lime`, `green`, `cyan`,
  `blue`, `purple`, `white`, `black`
- HEX: `#FF2ED1`
- RGB: `255,46,209`

---

## Konfiguratsiya

Asosiy kalitlar (to'liq ro'yxat JSON faylning o'zida):

| Kalit | Standart | Izoh |
|---|---|---|
| `DefaultTheme` | `highcontrast` | Rang to'plami. **`custom`** qilinsa quyidagi ranglar ishlatiladi |
| `TintEnabled` / `TintColorT` / `TintColorCT` | `true` / sariq / moviy | Model rangi |
| `TintAlpha` | `255` | 1 — deyarli ko'rinmas, 255 — qattiq |
| `TintFollowsHealth` / `TintHurtColor` | `false` / `#FF2020` | Jon bo'yicha rang |
| `OutlineEnabled` / `OutlineColorT` / `OutlineColorCT` | `true` | Kontur |
| `OutlineRange` | `4000` | Kontur ko'rinadigan masofa |
| `OutlineGlowType` | `2` | `3` (devor ortidan) kiritilsa avtomatik `2` ga qaytariladi |
| `HighlightEnabled` + ranglar | `true` | Bomba / defuse kit / garov |
| `RadarEnabled` / `RadarUpdateInterval` | `true` / `0.35` | Radar |
| `HudEnabled` / `HudUpdateInterval` / `HudDefaultSize` | `true` / `0.2` / `l` | HUD |
| `HudShow*` | `true` | HUD'da nima ko'rsatilsin |
| `HudLowHealthThreshold` / `HudLowHealthColor` | `35` / `#FF3B30` | Kam jon ogohlantirishi |
| `DefaultLanguage` | `uz` | Yangi o'yinchilar uchun til |
| `AdminFlag` | `@css/generic` | Admin buyruqlari uchun flag |

> `!tintcolor` yoki `!outlinecolor` bilan rang qo'lda o'zgartirilsa, `DefaultTheme`
> avtomatik `custom` ga o'tadi — shunda sizning ranglaringiz ustuvor bo'ladi.

---

## Ichkarida qanday ishlaydi

**Model rangi.** `CBaseModelEntity::m_clrRender` + `kRenderTransColor`. Modelning
o'zi bo'yaladi, ya'ni faqat model ko'rinadigan joyda ko'rinadi.

**Kontur.** CS2 da "shu o'yinchiga kontur chiz" degan server buyrug'i yo'q,
shuning uchun o'yinchining o'z modelidan ikkita `prop_dynamic` nusxa yaratiladi:
*relay* (render rejimi `kRenderNone`, pawn'ni kuzatadi) va *glow* (relay'ni
kuzatadi, `CGlowProperty` maydonlarini olib yuradi). Ikkalasi ham
`SF_DYNAMICPROP_NO_VPHYSICS` (256) bilan — to'qnashuvi yo'q, o'q ham, harakat
ham to'silmaydi. `m_iGlowType` faqat `2` (devor ortidan emas) qiymatida
ishlatiladi; konfiguratsiyaga `3` yozilsa ham kod uni `2` ga qaytaradi.

Raund qayta boshlanganda dvigatel bu proplarni o'chiradi, shuning uchun plugin
eski indekslarni "unutadi" — indeks qayta ishlatilib, boshqa entity o'chib
ketmasligi uchun (`ForgetAll`).

**Radar.** `EntitySpottedState_t` ichidagi `m_bSpotted` va `m_bSpottedByMask`
to'ldiriladi. Mask 64 bit: 0-indeks 0–31 slotlar, 1-indeks 32–63. Ikkalasi
`0xFFFFFFFF` — "hamma ko'rgan". Dvigatel bu holatni doim qayta hisoblagani
uchun timer bilan yangilanadi. Tarmoq o'zgarishi
`CCSPlayerPawn::m_entitySpottedState` orqali belgilanadi —
`m_bSpotted` ichma-ich struktura a'zosi bo'lgani uchun to'g'ridan-to'g'ri emas.

**HUD.** `PrintToCenterHtml` — Panorama faqat `fontSize-s/m/l` sinflarini
qabul qiladi, ixtiyoriy piksel o'lchamini emas, shuning uchun o'lcham uchta
qiymatdan iborat.

**Nishonlar.** `planted_c4` va `hostage_entity` doim bo'yaladi; `weapon_c4` va
defuse kit faqat yerda yotganda (`OwnerEntity` bo'sh bo'lganda) — qo'lda
ko'tarilgan bombani bo'yash uni dushmanga oshkor qilib qo'yardi.

---

## Loyiha tuzilishi

```
VisionAssist.sln              Visual Studio yechimi
Directory.Build.props         Umumiy build sozlamalari (net8.0, nullable, deploy yo'li)
Local.props.example           Serverga avtomatik ko'chirish uchun namuna
NuGet.config                  nuget.org manbasi
.editorconfig                 Kod uslubi (VS avtomatik qo'llaydi)

src/VisionAssist/
├── VisionAssist.csproj       Loyiha + serverga ko'chirish target'i
├── VisionAssistPlugin.cs     Yuklash, event'lar, timer'lar
├── Config/
│   ├── VisionAssistConfig.cs JSON konfiguratsiya
│   └── Theme.cs              7 ta rang to'plami
├── Core/
│   ├── ColorParser.cs        Rang o'qish (#hex, r,g,b, nom)
│   ├── Chat.cs               Chat rang teglari
│   ├── Lang.cs               uz / ru / en matnlari
│   ├── PlayerPreferenceStore.cs  SteamID bo'yicha shaxsiy sozlamalar
│   └── ClientTips.cs         Tavsiya etilgan client cvar'lari
├── Features/
│   ├── TintController.cs     Model rangi
│   ├── OutlineController.cs  Kontur (through-walls bloklangan)
│   ├── RadarController.cs    To'liq radar
│   ├── HighlightController.cs Bomba / garov / defuse kit
│   └── HudController.cs      Katta markaziy HUD
└── Commands/
    ├── VisionAssistPlugin.Commands.cs  Barcha buyruqlar
    └── VisionAssistPlugin.Menu.cs      !vision menyusi
```
