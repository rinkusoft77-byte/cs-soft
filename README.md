# VisionAssist — CS2 server plugini

O'zingiz admin bo'lgan CS2 serveri uchun **server tomonida** ishlaydigan
ko'rinuvchanlik plugini. Uchta ish qiladi:

1. **Glow (kontur)** — har bir o'yinchi atrofida rangli chiziq. Rangini
   o'zingiz xohlagancha o'zgartirasiz (T va CT uchun alohida).
2. **Model rangi (tint)** — o'yinchi modelining o'zini bo'yaydi (rasmdagi
   pushti modelka effekti). Shaffofligi ham sozlanadi.
3. **To'liq radar** — barcha o'yinchilar radarda ko'rinadi.

Hammasi **CounterStrikeSharp** orqali server tomonida ishlaydi. Ya'ni:

- o'yin fayllariga, xotirasiga yoki mijozga hech narsa qilinmaydi — VAC bilan
  aloqasi yo'q;
- faqat **siz plugin o'rnatgan serverda** ishlaydi, boshqa serverda emas;
- effekt **serverdagi hamma o'yinchi uchun bir xil** — bitta o'yinchiga
  ustunlik beradigan "faqat menga ko'rinsin" rejimi ataylab yozilmagan.

---

## Nima qila olmaydi

Buni oldindan bilib qo'ying, chunki 1-rasmdagi hamma narsa server tomonidan
chiqmaydi:

| Narsa | Holati |
|---|---|
| Rangli kontur (glow) | ✅ bor |
| Model rangini o'zgartirish | ✅ bor |
| Devor ortidan ko'rinishi | ✅ bor (`GlowThroughWalls`) |
| Hamma radarda ko'rinishi | ✅ bor |
| 2D quti (box) va ism yozuvi | ❌ yo'q — bu mijoz tomonida chiziladi, server buni chiza olmaydi |
| Faqat boshni alohida bo'yash | ❌ yo'q — model bir butun bo'yaladi, tana qismlarini ajratib bo'lmaydi |

---

## Talablar

- CS2 dedicated server (o'zingizniki)
- [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master) (CS2 uchun dev build)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.305 yoki yuqori (**with-runtime** versiyasi)
- Qurish uchun: .NET 8 SDK

## Qurish

```bash
dotnet build -c Release src/VisionAssist/VisionAssist.csproj
```

Natija: `src/VisionAssist/bin/Release/net8.0/VisionAssist.dll`

## O'rnatish

`VisionAssist.dll` faylini serverga quyidagi papkaga tashlang:

```
csgo/addons/counterstrikesharp/plugins/VisionAssist/VisionAssist.dll
```

Serverni qayta ishga tushiring yoki konsolda:

```
css_plugins load VisionAssist
```

Birinchi yuklanishda konfiguratsiya fayli avtomatik yaratiladi:

```
csgo/addons/counterstrikesharp/configs/plugins/VisionAssist/VisionAssist.json
```

---

## Buyruqlar

Chatda `!` bilan, konsolda `css_` bilan yoziladi (masalan `!glowcolor` yoki
`css_glowcolor`). Standart holatda admin flagi `@css/generic` kerak — buni
konfiguratsiyadagi `AdminFlag` orqali o'zgartirasiz. Server konsoli har doim
ruxsatga ega.

| Buyruq | Vazifasi | Misol |
|---|---|---|
| `!vision` | Hozirgi sozlamalarni ko'rsatadi | `!vision` |
| `!glow <on/off>` | Konturni yoqadi/o'chiradi | `!glow on` |
| `!glowcolor <t/ct/all> <rang>` | Kontur rangi | `!glowcolor all magenta` |
| `!tint <on/off>` | Model bo'yashni yoqadi/o'chiradi | `!tint on` |
| `!tintcolor <t/ct/all> <rang>` | Model rangi | `!tintcolor t #FF2ED1` |
| `!glowrange <son>` | Kontur qancha masofagacha ko'rinadi | `!glowrange 5000` |
| `!walls <on/off>` | Kontur devor ortidan ko'rinsinmi | `!walls on` |
| `!radar <on/off>` | To'liq radarni yoqadi/o'chiradi | `!radar on` |
| `!colors` | Tayyor rang nomlarini ko'rsatadi | `!colors` |
| `!vision_reload` | JSON faylni diskdan qayta o'qiydi | `!vision_reload` |

Buyruq bilan qilingan o'zgarish JSON faylga ham yozib qo'yiladi, shuning uchun
map o'zgarganda yoki server qayta ishga tushganda saqlanib qoladi.

### Rang formatlari

Uchala usul ham ishlaydi:

- Nom: `magenta`, `pink`, `red`, `orange`, `yellow`, `lime`, `green`, `cyan`,
  `blue`, `purple`, `white`, `black`
- HEX: `#FF2ED1`
- RGB: `255,46,209`

---

## Konfiguratsiya (`VisionAssist.json`)

| Kalit | Standart | Izoh |
|---|---|---|
| `GlowEnabled` | `true` | Kontur yoqilganmi |
| `GlowThroughWalls` | `true` | `true` — devor ortidan ham ko'rinadi, `false` — faqat ko'z oldida |
| `GlowColorT` / `GlowColorCT` | `#FF2ED1` / `#00D9FF` | Kontur ranglari |
| `GlowRange` | `5000` | Kontur ko'rinadigan masofa (unit) |
| `GlowRangeMin` | `0` | Shu masofadan yaqinda kontur chizilmaydi |
| `TintEnabled` | `true` | Model bo'yash yoqilganmi |
| `TintColorT` / `TintColorCT` | `#FF2ED1` / `#00D9FF` | Model ranglari |
| `TintAlpha` | `255` | 1 — deyarli ko'rinmas, 255 — to'liq qattiq |
| `RadarEnabled` | `true` | Hamma radarda ko'rinsinmi |
| `RadarUpdateInterval` | `0.35` | Radar holati necha soniyada yangilanadi (0.1–5.0) |
| `RadarIncludeDead` | `false` | O'lgan o'yinchilar ham radarda qolsinmi |
| `AnnounceOnJoin` | `true` | Kirgan o'yinchiga server rejimi haqida xabar berish |
| `ChatPrefix` | `[Vision]` | Chat prefiksi |
| `AdminFlag` | `@css/generic` | Buyruqlar uchun kerakli flag |

### Ko'zga qulay bo'lishi uchun tavsiya

Ko'rish qiyin bo'lsa quyidagi kombinatsiya eng kontrastli chiqadi:

```json
"GlowColorT":  "#FFEB3C",
"GlowColorCT": "#00D9FF",
"TintColorT":  "#FF2ED1",
"TintColorCT": "#00D9FF",
"TintAlpha":   255,
"GlowRange":   6000
```

Sariq/pushti va moviy juftligi Dust2 va Mirage'ning qumli fonida bir-biridan
eng yaxshi ajralib turadi.

Bundan tashqari o'yinning o'zida radarni kattalashtirish foydali (bu plugin
emas, oddiy client sozlamasi, `autoexec.cfg` ga yozing):

```
cl_radar_scale 0.4
cl_radar_always_centered 0
cl_hud_radar_scale 1.3
cl_radar_icon_scale_min 0.6
```

---

## Ichkarida qanday ishlaydi

**Glow.** CS2 da "shu o'yinchiga kontur chiz" degan server buyrug'i yo'q.
Shuning uchun o'yinchining o'z modelidan ikkita `prop_dynamic` nusxa
yaratiladi:

- *relay* — render rejimi `kRenderNone`, o'yinchi pawn'ini kuzatib boradi;
- *glow* — relay'ni kuzatadi va `CGlowProperty` maydonlarini
  (`m_glowColorOverride`, `m_iGlowType`, `m_nGlowRange`) olib yuradi.

Ikkalasi ham `SF_DYNAMICPROP_NO_VPHYSICS` (256) bilan yaratiladi, ya'ni
to'qnashuvi yo'q — o'q ham, harakat ham to'sib qolinmaydi. Raund qayta
boshlanganda dvigatel bu proplarni o'chiradi, shuning uchun plugin eski
indekslarni "unutadi" (`ForgetAll`) — indeks qayta ishlatilib, boshqa
entity o'chib ketmasligi uchun.

`GlowThroughWalls: true` bo'lganda `m_iGlowType = 3`, `false` bo'lganda `2`
qo'yiladi. `3` — yaxshi sinalgan qiymat; `2` ba'zi o'yin build'larida boshqacha
xulq ko'rsatishi mumkin, chunki bu qiymatlarni Valve hujjatlashtirmagan.

**Radar.** `EntitySpottedState_t` ichidagi `m_bSpotted` va
`m_bSpottedByMask` maydonlari to'ldiriladi. Mask 64 bit: 0-indeks 0–31
slotlar, 1-indeks 32–63 slotlar uchun. Ikkalasi `0xFFFFFFFF` bo'lsa — "hamma
ko'rgan" degani, natijada blip hammaning radarida chiqadi. Dvigatel bu holatni
doim qayta hisoblaydi, shuning uchun timer bilan yangilanib turiladi.

Tarmoq o'zgarishi `CCSPlayerPawn::m_entitySpottedState` maydoni orqali
belgilanadi — `m_bSpotted` ichma-ich joylashgan struktura a'zosi bo'lgani
uchun uni to'g'ridan-to'g'ri belgilab bo'lmaydi.
