# VisionAssist Overlay — haqiqiy dastur, brauzer emas

Bu Companion'ning brauzersiz varianti. Bitta `.exe`, o'z oynasi bilan:

- **o'yin ustida turadi** — doim eng yuqorida, alohida oyna sifatida;
- **sichqonchani o'tkazib yuboradi** — HUD ustiga bosilsa, bosish o'yinga tushadi,
  ya'ni o'ynashga xalaqit bermaydi;
- **Alt bosilsa menyu chiqadi** — rang, o'lcham, joylashuv, ovoz sozlamalari;
- **ovozni o'zi oladi** — WASAPI loopback orqali, hech qanday ruxsat so'ramaydi,
  ekran ulashish kerak emas;
- tray ikonkasi bor, Alt+Tab'da ko'rinmaydi, taskbar'ni band qilmaydi.

Ma'lumot manbasi o'sha — Valve'ning **Game State Integration** interfeysi.
Xotira o'qish, inject, o'yin fayllarini o'zgartirish yo'q.

---

## Companion bilan farqi

| | Companion | Overlay |
|---|---|---|
| Ko'rinishi | brauzer sahifasi | haqiqiy oyna |
| O'yin ustida | faqat qo'lda joylashtirsangiz | avtomatik, doim yuqorida |
| Sichqoncha | oynani bosadi | o'yinga o'tadi |
| Menyu | sahifadagi ⚙ | **Alt** yoki tray |
| Ovoz | ekran ulashish + belgi qo'yish | avtomatik, ruxsat so'ramaydi |
| Ikkinchi monitor | juda mos | ham mos |

Ikkalasi bir xil portni (`47474`) ishlatadi, shuning uchun **bir vaqtda faqat
bittasi ishlaydi.**

---

## Ishga tushirish

### 1. Build

```powershell
dotnet build VisionAssist.sln -c Release
```

Natija:
`src\VisionAssist.Overlay\bin\Release\net8.0-windows\VisionAssist.Overlay.exe`

> Bu loyiha **faqat Windows'da** quriladi (WinForms). Linux serverda faqat
> plugin kerak bo'lsa:
> `dotnet build src/VisionAssist/VisionAssist.csproj -c Release`

### 2. `.cfg` (bir marta)

Agar Companion bilan allaqachon qilgan bo'lsangiz, bu qadam kerak emas — bir xil
fayl ishlatiladi. Aks holda:

```powershell
cd src\VisionAssist.Companion\bin\Release\net8.0
.\VisionAssist.Companion.exe --install
```

Keyin **CS2 ni to'liq yopib qayta ochish** (GSI fayli faqat startda o'qiladi).

### 3. Ishga tushirish

`VisionAssist.Overlay.exe` ustiga ikki marta bosing. Konsol oynasi chiqmaydi —
darhol HUD paydo bo'ladi va tray'da ikonka turadi.

Standart holatda HUD **faqat CS2 oldinda bo'lganda** ko'rinadi. Sozlashda
ko'rish uchun menyudan bu belgini o'chirib qo'ying.

### 4. O'yinni Fullscreen Windowed qilish

**Settings → Video → Display Mode → Fullscreen Windowed**

Buni qilmasa HUD o'yin ustida ko'rinmaydi. Bu Windows'ning qoidasi: *exclusive
fullscreen* rejimida o'yin ekranni butunlay egallaydi va boshqa oyna ustiga
chiqa olmaydi. Chiqarishning yagona yo'li — o'yinning grafik qatlamiga
ulanish (hook/inject), bu dastur esa buni qilmaydi.

Fullscreen Windowed'da FPS farqi zamonaviy Windows'da deyarli yo'q.

---

## Alt menyusi

**Alt ni bir marta bosib qo'yib yuborsangiz** menyu ochiladi va yopiladi.
Yopish uchun yana Alt, yoki Esc, yoki "Yopish".

Alt **bosib turilsa** hech narsa bo'lmaydi — Alt+Tab, Alt+F4 va o'yindagi Alt
bindlari ishlashda davom etadi. Menyu faqat *tez bosib qo'yib yuborish* ga
javob beradi: Alt tushdi, oradan boshqa tugma bosilmadi, va 400 ms ichida
qo'yib yuborildi. Hook hech qanday tugmani ushlab qolmaydi.

Menyu ochilganda HUD vaqtincha sichqonchani qabul qiladi (aks holda menyuni
bosib bo'lmasdi), yopilganda yana o'tkazib yuborishga qaytadi.

### Menyuda nima bor

| Sozlama | Izoh |
|---|---|
| **Til** | O'zbekcha / Русский / English |
| **Rang to'plami** | O'sha 7 to'plam, daltonizm variantlari bilan |
| **O'lcham** | S / M / L / XL — butun HUD kattalashadi, faqat matn emas |
| **Joylashuv** | To'rt burchakdan biri |
| **Shaffoflik** | 35–100%. Pastroq bo'lsa o'yin ko'proq ko'rinadi |
| **Ovoz yo'nalishi** | Yoqish / o'chirish |
| **Sezgirlik** | Ovoz hodisalari qanchalik oson belgilanadi |
| **Faqat CS2 oldinda** | Boshqa oynada ishlaganda HUD yashirinadi |
| **Ekran chetida yorug'lik** | Eshitmaydigan odam uchun — pastda batafsil |
| **Ovozli e'lonlar** | Ko'zi ko'rmaydigan odam uchun — pastda batafsil |
| **Nutq tezligi** | Sekin va tushunarli ↔ tez |

Har bir o'zgarish darhol qo'llanadi va `overlay.json` ga yoziladi.

Tray ikonkasiga ikki marta bosish ham menyuni ochadi — Alt hook o'rnatilmasa
(antivirus bloklashi mumkin) shu yo'l qoladi.

---

## HUD'da nima ko'rsatiladi

Yuqori qatorda: ulanish holati / xarita nomi va hisob.

To'rt katta raqam: **JON**, **ZIRH**, **QUROL/O'Q**, **TAYMER**.

Ogohlantirishlar butun kenglik bo'ylab: **BOMBA QO'YILDI**, **YONAYAPSIZ**,
**KO'ZINGIZ KO'RMAYDI**, **TUTUN ICHIDA**, **ZARARSIZLANTIRILDI**.

Bomba qo'yilganda va siz CT bo'lsangiz: **YETADI +2.3s** yoki **YETMAYDI** —
qolgan vaqtni kit borligiga solishtiradi (kit bilan 5 s, kitsiz 10 s).

Pastda: pul, K/D, granatalar, kit, bomba sizdami.

Oxirida: **ovoz yo'nalishi** chizig'i.

---

## Ovoz yo'nalishi

Companion'dagi mantiq bir xil, lekin manba boshqa: **WASAPI loopback** —
Windows'ning standart chiqish qurilmasidan to'g'ridan-to'g'ri o'qiydi.

Bu brauzer variantidan yaxshi:

- ruxsat so'ramaydi, ekran ulashish oynasi chiqmaydi;
- "tizim ovozini ham ulashish" belgisini esdan chiqarib bo'lmaydi;
- o'yin o'rtasida to'xtab qolmaydi.

Zanjir: `loopback → highpass 90 Hz → lowpass 1200 Hz → energiya (har ~10 ms)`.
Keyin ikki eksponensial o'rtacha: tez (~30 ms, tovush hujumi) va sekin
(~1.8 s, "hozirgi jimlik"). Tez o'rtacha sekinidan bir necha barobar oshsa —
hodisa. Nisbat ustiga qurilgani uchun ovoz balandligini o'zgartirsangiz qayta
sozlash kerak bo'lmaydi.

Chizma: chap↔o'ng o'qi, jonli ko'rsatkich (balandligi — tovush kuchi), va
so'nib boradigan nuqtalar (qadamlar ritmi ko'rinadi). Qattiq tovushlar
(o'q, portlash) qizil rangda.

> **Chapdan/o'ngdan — ha. Oldindan/orqadan — yo'q.** Old va orqa farqi HRTF
> filtrida bo'ladi va chiqish miksiga yetganda u allaqachon qo'llanib bo'lgan.
> Hech qanday dastur uni ovozdan qaytarib chiqara olmaydi.
>
> Ovoz aniqligi uchun: o'yinda **Settings → Audio → HRTF** yoqilgan bo'lsin,
> Windows'da chiqish **stereo** bo'lsin (mono bo'lsa menyu shuni aytadi).

---

## Eshitmaydigan odam uchun: ekran chetida yorug'lik

HUD'dagi kichik ovoz paneli ishlaydi, lekin unga **qarab turish** kerak — o'ynayotganda esa odam priselga qaraydi. Shuning uchun alohida rejim bor: butun ekranni qoplaydigan shaffof oyna, ovoz kelgan **chetni yoritadi**.

Uch zona, chunki stereo signal aynan uchta halol javobni ko'taradi:

| Zona | Qayerda yonadi |
|---|---|
| Chap | Ekranning chap cheti |
| O'ng | Ekranning o'ng cheti |
| O'rta | Yuqori chetning o'rtasi (uchdan bir kenglikda) |

O'rta zona **"oldinda yoki orqada"** degani — qaysi biri ekanini aytmaydi, chunki bu ma'lumot ovozda yo'q. Ataylab shunday: yolg'on aniqlik yolg'on qarorga olib keladi.

Yorqinligi tovush kuchiga bog'liq, ~400 ms da so'nadi. O'q va portlashlar qizil, qadamlar rang to'plamining rangida. Chetdan ichkariga qarab so'nib boradi — bu "yorug'lik" bo'lib ko'rinadi, o'yin ustidagi qattiq blok emas.

Hech narsa bo'lmasa oyna butunlay yashiriladi — shaffof to'liq ekran oynasini doim chizib turish resurs oladi.

**Muhim:** bu ovoz o'lchagichga tayanadi, ya'ni menyuda "Ovoz yo'nalishi" o'chirilgan bo'lsa ham o'lchagich ishlaydi (`NeedsSoundMeter`). Ikkalasini ham o'chirsangiz o'lchagich to'xtaydi.

---

## Ko'zi ko'rmaydigan odam uchun: ovozli e'lonlar

Raqamni katta qilib chizish har doim yetarli emas — HUD'ni topib, unga ko'z tikish kerak. Eshitish tezroq. Windows SAPI orqali dastur o'z holatini gapiradi:

| Nima | Qachon |
|---|---|
| Jon raqami | 5 va undan ko'p jon ketganda |
| "Mало здоровья / low health" + raqam | Chegaradan (standart 35) pastga tushganda, boshqa gaplarni uzib |
| "Патроны / ammo" + raqam | Magazin choragidan kam qolganda, magazinda bir marta |
| "Нет патронов / out of ammo" | O'q tugaganda |
| "Бомба установлена" | Bomba qo'yilganda |
| 20, 10, 5 | Bomba fitili shu soniyalarda — qaror o'zgaradigan nuqtalar |
| "Разминирована" / "Взрыв" | Bomba zararsizlantirilganda / portlaganda |

Gaplar **qisqa** — uzun jumla o'qib bo'lguncha vaziyat o'zgarib ketadi. Ustuvorlik bor: shoshilinch gap oddiy gapni uzadi, teskarisi emas.

### Til haqida rostini aytish

Windows'da **o'zbek tili uchun TTS ovozi yo'q**. Shuning uchun o'zbek tili tanlanganda e'lonlar **ruscha** aytiladi — raqamlar va ikki-uch so'z, tushunarli bo'lishi uchun. Inglizcha tanlansa inglizcha aytadi. Ovoz umuman o'rnatilmagan bo'lsa dastur tray orqali xabar beradi va HUD baribir ishlashda davom etadi.

Ovoz o'rnatish: **Settings → Time & Language → Speech → Manage voices → Add voices**.

### Bitta texnik nozik joy

E'lonlar o'sha karnaydan chiqadi, ovoz o'lchagich esa aynan o'sha karnayni tinglaydi. Shuning uchun har bir gapdan oldin o'lchagichning **hodisa aniqlash** qismi vaqtincha to'xtatiladi (`SoundMeter.SuppressFor`). Bo'lmasa har bir e'lon o'rtadan kelgan qattiq tovush bo'lib belgilanardi va bir e'lon ikkinchisini chaqirardi. Daraja o'lchash to'xtamaydi — faqat hodisa aniqlash.

### `overlay.json` dagi qo'shimcha kalitlar

Menyuda hammasi yo'q, chunki menyu cho'zilib ketardi. JSON'da:

| Kalit | Standart |
|---|---|
| `AnnounceHealth` | `true` |
| `AnnounceAmmo` | `true` |
| `AnnounceBomb` | `true` |
| `AnnounceRound` | `false` — raund boshlanishini ham aytadi |
| `SpeechVolume` | `0.9` |

---

## Nima qilmaydi

| Narsa | Holati |
|---|---|
| O'z joni / o'qi / taymeri, kattalashtirilib | ✅ |
| O'yin ustida turish, Alt menyusi | ✅ |
| Ovozni ko'rinadigan qilish (chap/o'ng) | ✅ |
| Ekran chetida yorug'lik (kar odam uchun) | ✅ |
| Ovozli e'lonlar (ko'zi ko'rmas odam uchun) | ✅ |
| **Xarita rangini o'zgartirish, fonni qoraytirish** | ❌ **yo'q** |
| **Dushman modelini bo'yash, kallani ajratish** | ❌ **yo'q** |
| **Dushmanlarni radarda ko'rsatish** | ❌ **yo'q** |
| Exclusive fullscreen ustida chizish | ❌ Fullscreen Windowed kerak |
| Old/orqa yo'nalish | ❌ ovozda bunday ma'lumot yo'q |

Dushman joylashuvi va model rangi bu dasturda yo'q va qo'shilmaydi. GSI ularni
bermaydi; olishning yagona yo'li — o'yin xotirasini o'qish yoki DLL inject
qilish. Bu raqiblar ko'rmagan ma'lumotni beradi (ya'ni cheat), VAC ban
keltiradi, va reponing butun mantig'iga qarshi.

Model rangi va konturi **o'zingiz admin bo'lgan serverda** ishlaydi —
`src/VisionAssist/` plugini aynan shuni qiladi va effekt serverdagi hamma
uchun bir xil bo'ladi. Batafsil: [INSTALL.md](INSTALL.md).

---

## Muammolar

| Belgi | Sabab / yechim |
|---|---|
| HUD umuman ko'rinmaydi | Menyuda "Faqat CS2 oldinda" yoqilgan, CS2 esa ochilmagan. Tray → Sozlamalar → belgini o'chiring |
| O'yin ustida ko'rinmaydi | Exclusive fullscreen. **Fullscreen Windowed** ga o'tkazing |
| "KUTILMOQDA" ketmaydi | `.cfg` qo'yilgandan keyin CS2 qayta ishga tushirilmagan |
| "O'YIN YOPIQ" | 25 soniya hech narsa kelmadi |
| Alt menyuni ochmaydi | Hook o'rnatilmadi (antivirus). Tray ikonkasiga ikki marta bosing |
| Alt+Tab menyuni ochib yuboradi | Bo'lmasligi kerak — boshqa tugma bosilsa tap hisoblanmaydi. Bo'lsa aytingiz |
| "Port band" | Companion ochiq. Uni yopib, buni qayta ishga tushiring |
| "OVOZ KELMAYAPTI" | Windows'da standart chiqish qurilmasi boshqa (masalan HDMI). Karnay/naushnikni standart qilib qo'ying |
| "OVOZ MONO" | Chiqish stereo emas. Windows → Sound → qurilma xossalari → stereo |
| HUD sichqonchani ushlab qoladi | Menyu ochiq qolgan. Esc bosing |
| Dasturdan chiqish | Tray → "Dasturdan chiqish", yoki menyudagi qizil tugma |
| Ekran cheti yonmaydi | Ovoz o'lchagich ishlamayapti. HUD'dagi ovoz panelida `dB` raqami o'zgarib turishi kerak |
| Ovozli e'lonlar jim | Windows'da TTS ovozi o'rnatilmagan. Settings → Time & Language → Speech → Add voices |
| E'lonlar o'zini qayta ushlaydi | Bo'lmasligi kerak — `SuppressFor` buni to'sadi. Bo'lsa aytingiz |

---

## Loyiha tuzilishi

```
src/VisionAssist.Overlay/
├── VisionAssist.Overlay.csproj   net8.0-windows, WinForms, NAudio
├── Program.cs                    bitta nusxa, tray ikonka, ishga tushirish
├── OverlayForm.cs                shaffof, doim yuqorida, click-through HUD
├── SettingsForm.cs               Alt menyusi
├── OverlaySettings.cs            overlay.json
├── GsiHost.cs                    GSI endpoint (Companion kodini qayta ishlatadi)
├── Native/
│   ├── Win32.cs                  oyna uslublari, z-order, hook, foreground
│   └── AltTapHook.cs             Alt "tap" ni aniqlash (bosib turish emas)
├── SoundFlashForm.cs             to'liq ekran, chetdagi yorug'lik (kar odam uchun)
├── Audio/
│   ├── SoundMeter.cs             WASAPI loopback + hodisa aniqlash
│   └── Biquad.cs                 highpass / lowpass filtrlar
├── Speech/
│   └── Announcer.cs              Windows SAPI e'lonlari (ko'zi ko'rmas odam uchun)
└── UI/
    ├── Palette.cs                7 rang to'plami
    └── Strings.cs                uz / ru / en
```

HTTP server, GSI modeli va holat saqlagichi `VisionAssist.Companion` dan
`ProjectReference` orqali olinadi — ikkinchi nusxa yozilmagan.
