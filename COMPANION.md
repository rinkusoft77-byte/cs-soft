# VisionAssist Companion — istalgan serverda ishlaydigan overlay

`src/VisionAssist/` dagi plugin faqat **siz admin bo'lgan serverda** ishlaydi.
Bu loyiha esa boshqa masalani yechadi: **istalgan serverga kirsangiz ham**
ishlaydi — rasmiy matchmaking, community server, offline, farqi yo'q.

Buning sababi oddiy: bu dastur serverga ham, o'yin jarayoniga ham tegmaydi.
U ikki xil ma'lumot bilan ishlaydi va ikkalasi ham allaqachon sizga tegishli:

1. **O'yinning o'zi yuboradigan holat** — Valve'ning rasmiy
   *Game State Integration* (GSI) interfeysi orqali. O'yin sizning jonini,
   zirhini, o'qini, raund taymerini `127.0.0.1` ga HTTP bilan yuboradi.
   Bu Valve tomonidan striming overlaylari uchun ochilgan interfeys.
2. **Karnayingizdan chiqayotgan ovoz** — tizim ovozini o'lchab, tovush
   stereo maydonda chapdami yoki o'ngdami ekanini ko'rsatadi.

```
CS2  ──GSI (HTTP)──▶  VisionAssist.Companion  ──SSE──▶  brauzerdagi overlay
                                                            ▲
Windows audio  ──ekran ulashish / Stereo Mix────────────────┘
```

---

## Nima qiladi

### Katta, yuqori kontrastli HUD

| Ko'rsatiladi | Manba |
|---|---|
| Jon (kam bo'lganda rangi o'zgaradi) | `player_state` |
| Zirh va kaska | `player_state` |
| Qo'ldagi qurol, o'q (magazin + zapas) | `player_weapons` |
| Raund taymeri, freezetime, **bomba fitili** | `phase_countdowns` |
| Bomba holati: qo'yildi / zararsizlantirildi / portladi | `round` |
| Pul, granatalar, defuse kit | `player_state`, `player_weapons` |
| Xarita, raund raqami, ikki tomon hisobi | `map` |
| K/D, MVP | `player_match_stats` |

Butun ekran bo'ylab ogohlantirishlar: **BOMBA QO'YILDI**, **YONAYAPSIZ**,
**KO'ZINGIZ KO'RMAYDI**, **TUTUN ICHIDA**.

Plugindagi **o'sha 7 rang to'plami** (`highcontrast`, `neon`, `deuteranopia`,
`protanopia`, `tritanopia`, `soft`, `mono`), 4 o'lcham (S / M / L / XL) va uch
til (o'zbek, rus, ingliz).

### Ovoz yo'nalishi ko'rsatkichi

Eshitishi past bo'lgan o'yinchi uchun. Karnayga ketayotgan ovozni tinglaydi,
90–1200 Hz oralig'ini ajratib oladi (qadam tovushlari shu yerda), va:

- **jonli ko'rsatkich** — tovush stereo maydonda qayerda turgani;
- **hodisa belgilari** — har bir keskin tovush (qadam, o'q) chap/o'ng o'qida
  nuqta bo'lib chiqadi va sekin so'nadi, shuning uchun qadamlarning **ritmi**
  ko'rinadi;
- qattiq tovushlar (o'q, portlash) boshqa rangda belgilanadi.

> ### Chapdan/o'ngdan — ha. Oldindan/orqadan — yo'q.
>
> Old va orqa farqi HRTF filtrida bo'ladi va loopback yozib olishda u
> allaqachon yo'q. Buni **hech qanday dastur** ovozdan chiqarib bera olmaydi.
> Kim aksini aytsa — o'yin xotirasini o'qiyapti, ya'ni bu cheat.
>
> Ovoz sifati yaxshi bo'lishi uchun o'yinda **Settings → Audio → HRTF** yoqilgan
> bo'lsin va Windows'da chiqish **stereo** qilib qo'yilgan bo'lsin.

---

## Nima qilmaydi

| Narsa | Holati |
|---|---|
| O'z joni, o'qi, taymeri — kattalashtirilib | ✅ |
| Ovozni ko'rinadigan qilish (chap/o'ng) | ✅ |
| Istalgan serverda ishlash | ✅ |
| **Dushman joylashuvi, radar nuqtalari** | ❌ **yo'q** |
| **Devor ortidan ko'rish, model glow** | ❌ **yo'q** |
| Old/orqa yo'nalish | ❌ ovozda bunday ma'lumot yo'q |
| Boshqa o'yinchilarning joni | ❌ GSI bermaydi |

Buni aniq aytib qo'yaman: **dushman joylashuvini beradigan versiyasi yo'q va
bo'lmaydi.** GSI faqat sizning holatingizni beradi (`allplayers_*` bloklari
faqat kuzatuvchi va GOTV uchun to'ldiriladi, shuning uchun cfg'da ular
so'ralmagan ham). Uni olishning yagona yo'li — o'yin xotirasini o'qish yoki
DLL inject qilish. Bu:

- raqiblar ko'rmagan ma'lumotni beradi, ya'ni cheat;
- VAC ban keltiradi;
- va reponing butun mantig'iga qarshi — `OutlineController.ThroughWallsGlowType`
  aynan shuning uchun bloklangan.

---

## O'rnatish

### 1. Qurish

Visual Studio'da `VisionAssist.sln` → **F6**. Yoki buyruq satridan:

```bash
dotnet build VisionAssist.sln -c Release
```

Natija: `src/VisionAssist.Companion/bin/Release/net8.0/VisionAssist.Companion.exe`
(yonida `wwwroot/` va `cfg/` papkalari bilan).

### 2. `.cfg` fayllarini o'yinga qo'yish

```bash
VisionAssist.Companion --install
```

Steam kutubxonasini o'zi topadi va tasdiq so'ragandan keyin ikki fayl yozadi:

| Fayl | Vazifasi |
|---|---|
| `gamestate_integration_visionassist.cfg` | o'yin holatni shu dasturga yuboradi |
| `visionassist_accessibility.cfg` | radar/prisel/HUD sozlamalari (o'zingiz `exec` qilasiz) |

Topa olmasa yo'lni qo'lda ko'rsatasiz:

```bash
VisionAssist.Companion --install --cs2-cfg "D:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\game\csgo\cfg"
```

Yo'lni bilmasangiz: Steam → CS2 ustiga o'ng tugma → **Manage → Browse local
files** → `game\csgo\cfg`. Yoki `--locate` bilan tekshirasiz.

### 3. CS2 ni qayta ishga tushirish

GSI fayli **faqat o'yin startida** o'qiladi. Qayta ishga tushirmasangiz hech
narsa kelmaydi.

### 4. Dasturni ishga tushirish

`VisionAssist.Companion.exe` — ikki marta bosing va ochiq qoldiring.
Brauzer o'zi ochiladi:

```
http://127.0.0.1:47474/
```

### 5. O'yin ichidagi sozlamalar (ixtiyoriy)

O'yin konsolida:

```
exec visionassist_accessibility
```

Har safar avtomatik bo'lishi uchun `game\csgo\cfg\autoexec.cfg` ga shu qatorni
yozib qo'yasiz.

---

## Overlay'ni qanday ko'rish

Rostini aytish kerak: brauzer oynasi **exclusive fullscreen** o'yin ustiga
chiqa olmaydi. Uch amaliy variant:

| Variant | Qanday |
|---|---|
| **Ikkinchi monitor** (eng ishonchli) | Brauzer oynasini ikkinchi monitorga tashlang. Hech qanday moslashtirish kerak emas. |
| **Fullscreen Windowed** | O'yinda Settings → Video → Display Mode → *Fullscreen Windowed*. Keyin brauzerni kichik oyna qilib chetga qo'yasiz va sozlamalarda **Shaffof fon** ni yoqasiz. |
| **App rejimi** | `chrome.exe --app=http://127.0.0.1:47474/ --window-size=460,900` — manzil satri va tablar yo'q, faqat overlay. |

> **Muhim:** manzil sifatida aynan `127.0.0.1` ishlatilsin. Ovoz olish
> (`getDisplayMedia`) faqat *secure context* da ishlaydi va `127.0.0.1` shunga
> kiradi, lokal tarmoq IP'si (`192.168.x.x`) esa kirmaydi.

---

## Ovozni yoqish

Sozlamalarda (⚙) ikki manba bor:

### 1. Ekran ulashish (tavsiya etiladi)

**Yoqish** tugmasini bosasiz → brauzer nima ulashishni so'raydi →
**Entire screen** ni tanlab, pastdagi **"Also share system audio"** /
**"Tizim ovozini ham ulashish"** belgisini **albatta** qo'yasiz.

Belgi qo'yilmasa dastur "Ovoz ulashilmadi" deb aytadi. Video oqimi
ishlatilmaydi — Chrome tizim ovozini faqat video bilan birga beradi, shuning
uchun so'raladi va tashlab qo'yiladi.

### 2. Stereo Mix

Realtek va shunga o'xshash kartalarda **Stereo Mix** kirish qurilmasi bo'ladi
(Windows → Sound → Recording → o'ng tugma → *Show Disabled Devices* → Stereo
Mix → Enable). Yoki VB-CABLE kabi virtual qurilma. Bu yo'lda har safar ekran
ulashishni tasdiqlash kerak bo'lmaydi.

**Sezgirlik** slayderi — asosiy sozlama. Juda ko'p nuqta chiqsa kamaytiring,
qadamlar sezilmasa oshiring. Chegara doim **joriy shovqin darajasiga nisbatan**
hisoblanadi, shuning uchun ovoz balandligini o'zgartirsangiz ham qayta
sozlash kerak bo'lmaydi.

---

## `companion.json`

Dastur birinchi ishga tushganda `.exe` yonida yaratiladi.

| Kalit | Standart | Izoh |
|---|---|---|
| `Port` | `47474` | GSI va overlay uchun port. O'zgartirsangiz `.cfg` ni ham qayta o'rnatish kerak |
| `Token` | `visionassist-local` | `.cfg` dagi token bilan bir xil bo'lishi shart |
| `RequireToken` | `true` | Tokenni tekshirish |
| `DefaultTheme` | `highcontrast` | Rang to'plami |
| `DefaultSize` | `l` | `s` / `m` / `l` / `xl` |
| `LowHealthThreshold` | `35` | Shu qiymatdan pastda jon qizil bo'ladi |
| `SoundIndicatorEnabled` | `true` | Ovoz panelini ko'rsatish |
| `SoundSensitivity` | `0.5` | Boshlang'ich sezgirlik |
| `TransparentBackground` | `false` | Shaffof fon |
| `OpenBrowserOnStart` | `true` | Startda brauzerni ochish |

Brauzerda qilingan tanlov (til, rang, o'lcham, sezgirlik) `localStorage` ga
yoziladi va JSON'dagi standartdan ustun turadi.

---

## Buyruq satri

```
VisionAssist.Companion [options]

  --install            .cfg fayllarini CS2 papkasiga yozish
  --locate             CS2 cfg papkasi qayerda ekanini ko'rsatish
  --cs2-cfg <path>     cfg papkasini qo'lda ko'rsatish
  -y, --yes            --install da tasdiq so'ramaslik
  --port <n>           boshqa port
  --web <folder>       overlay sahifasini boshqa papkadan berish
  --open / --no-open   startda brauzerni ochish / ochmaslik
  --verbose            har bir kelgan holatni log qilish
  -h, --help           yordam
```

---

## Muammolar

| Belgi | Sabab / yechim |
|---|---|
| `kutilmoqda` yozuvi ketmaydi | `.cfg` qo'yilgandan keyin CS2 **qayta ishga tushirilmagan** |
| `o'yin yopiq` | 25 soniya davomida hech narsa kelmadi. CS2 ochiqmi? |
| Konsolda "token does not match" | `companion.json` dagi `Token` va `.cfg` dagi token boshqa. `--install` ni qayta bajaring |
| `cannot listen on port 47474` | Dastur allaqachon ishlab turgan, yoki port band. `--port 47500` |
| `menyu` deb turadi | Jonli raundda emassiz — menyu yoki kuzatuvda. Bu normal |
| Taymer `–` | `phase_countdowns` bloki cfg'dan o'chib ketgan |
| "Ovoz ulashilmadi" | Ekran tanlashda tizim ovozi belgisi qo'yilmagan |
| Ovoz mono deb turadi | Windows'da chiqish qurilmasi stereo emas (masalan mono qilib qo'yilgan) |
| Ovoz paneli bo'sh | Manzil `127.0.0.1` emasmi? Boshqa IP'da brauzer ovoz olishga ruxsat bermaydi |
| Overlay o'yin ustida ko'rinmaydi | Exclusive fullscreen. *Fullscreen Windowed* ga o'tkazing yoki ikkinchi monitor |

---

## Ichkarida qanday ishlaydi

**HTTP server.** `HttpListener` emas, to'g'ridan-to'g'ri `TcpListener`
(`Web/HttpServer.cs`). Sababi: Windows'da `HttpListener` http.sys orqali
ketadi va ixtiyoriy port uchun administrator huquqi bilan URL reservation
kerak bo'ladi. Bu dastur esa oddiy ikki marta bosish bilan ishga tushishi
kerak. Faqat `127.0.0.1` ga bog'lanadi — tarmoqqa chiqmaydi.

**Holatni yetkazish.** GSI POST kelganda `StateStore` uni yassi
`OverlaySnapshot` ga aylantiradi va `EventStream` barcha ochiq
`text/event-stream` ulanishlariga yuboradi. Brauzer tomonida `EventSource` —
u o'zi qayta ulanadi. Agar SSE ishlamasa, sahifa `/state` ni yarim soniyada
bir so'rab turadigan rejimga o'tadi.

**Taymer.** GSI sekundda 10 marta keladi, taymer esa silliq ketishi kerak.
Shuning uchun sahifa oxirgi qiymatni va uni qabul qilgan vaqtni saqlab,
o'zi hisoblab ko'rsatadi.

**"O'yin yopiq" holati.** `.cfg` da `heartbeat 10.0` — o'yin hech narsa
o'zgarmasa ham 10 soniyada bir yuboradi. 25 soniya jim bo'lsa dastur
overlay'ga "ulanish yo'q" deb aytadi, aks holda CS2 yopilgandagi jon
ko'rsatkichi ekranda muzlab qolardi.

**Ovoz o'lchash.** `requestAnimationFrame` emas, **AudioWorklet**
(`wwwroot/sound-worklet.js`). Sababi: brauzer oynasi o'yin ortida qolganda
`requestAnimationFrame` sekinlashtiriladi, audio oqimi esa sekinlashtirilmaydi.
Worklet har ~10 ms da kanal energiyasini hisoblab yuboradi.

Zanjir: `MediaStreamSource → highpass 90Hz → lowpass 1200Hz → worklet`.
`ctx.destination` ga hech narsa ulanmaydi — ovoz allaqachon karnayda,
qayta ulash echo berardi.

Hodisa aniqlash ikkita eksponensial o'rtachaga tayanadi: tez (~30 ms, tovush
hujumi) va sekin (~1.8 s, "hozirgi jimlik" darajasi). Tez o'rtacha sekinidan
bir necha barobar oshsa — hodisa. Nisbat ustiga qurilganini sababi: mutlaq
chegara har xil ovoz balandligida ishlamaydi.

---

## Loyiha tuzilishi

```
src/VisionAssist.Companion/
├── VisionAssist.Companion.csproj
├── Program.cs                  CLI, ishga tushirish, --install / --locate
├── CompanionOptions.cs         buyruq satri argumentlari
├── CompanionConfig.cs          companion.json
├── Gsi/
│   ├── GsiPayload.cs           o'yin yuboradigan JSON (faqat o'z holati)
│   ├── OverlaySnapshot.cs      brauzerga ketadigan yassi holat
│   ├── StateStore.cs           payload → snapshot, eskirishni kuzatish
│   └── WeaponNames.cs          weapon_ak47 → AK-47
├── Web/
│   ├── HttpServer.cs           TcpListener asosidagi kichik HTTP server
│   ├── HttpExchange.cs         so'rovni o'qish, javob yozish, SSE boshlash
│   ├── OverlayRoutes.cs        /gsi, /state, /events, /config, /health
│   ├── EventStream.cs          server-sent events tarqatuvchi
│   └── StaticContent.cs        wwwroot dan fayl berish
├── Setup/
│   ├── SteamLocator.cs         CS2 cfg papkasini topish (libraryfolders.vdf)
│   └── CfgInstaller.cs         .cfg fayllarini yozish
├── cfg/
│   ├── gamestate_integration_visionassist.cfg
│   └── visionassist_accessibility.cfg
└── wwwroot/
    ├── index.html
    ├── overlay.css             7 rang to'plami, 4 o'lcham
    ├── overlay.js              SSE, render, sozlamalar, uz/ru/en
    ├── sound.js                ovoz olish va hodisa aniqlash
    └── sound-worklet.js        audio oqimida energiya o'lchash
```

---

## VAC va qoidalar

Bu dastur:

- o'yin jarayoniga inject qilinmaydi;
- o'yin xotirasini o'qimaydi va yozmaydi;
- o'yin fayllarini o'zgartirmaydi (faqat `cfg/` ga ikki fayl qo'shadi —
  bu Valve'ning o'zi kutgan joy);
- serverga hech narsa yubormaydi;
- sizga raqiblar ko'rmagan hech qanday ma'lumot bermaydi.

GSI — Valve'ning striming va turnir overlaylari uchun rasman ochgan
interfeysi. Ovoz esa karnayingizdan chiqayotgan, allaqachon eshitilgan tovush.
