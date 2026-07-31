# FPS — nimalar haqiqatan yordam beradi

Avval rostini aytib qo'yaman: **CS2 da `.cfg` bilan FPS ko'tarish deyarli
ishlamaydi.** Valve CS:GO davridagi "booster" convar'larning ko'pini olib
tashlagan yoki qulflab qo'ygan. Internetdagi "bu cfg bilan +200 FPS" degan
narsalar — yo eski CS:GO uchun, yo shunchaki yolg'on.

Haqiqiy foyda quyidagi tartibda keladi:

| # | Nima | Taxminiy foyda |
|---|---|---|
| 1 | O'yin ichidagi **Video sozlamalari** | o'nlab FPS |
| 2 | **Drayver va Windows** sozlamalari | o'nlab FPS |
| 3 | **Launch options** | bir necha FPS + tez ochilish |
| 4 | **Convar'lar** (`visionassist_performance.cfg`) | bir necha FPS |

Ya'ni vaqtingizni 1 va 2 ga sarflang.

---

## 0. Avval o'lchang

O'lchamasdan sozlamang — aks holda nima yordam berganini bilmaysiz.

Konsolda:

```
cl_showfps 1
```

Bir xil joyda (masalan o'lik holatda bir xil nuqtaga qarab) o'zgarishdan oldin
va keyin raqamni yozib oling. Tugagach `cl_showfps 0`.

Muhimi **o'rtacha FPS emas**, balki **eng past FPS** (1% low) — o'yin aynan shu
paytlarda "tutib qoladi". Shuning uchun otishma paytida qaraganingiz ma'qul.

---

## 1. Video sozlamalari (eng katta foyda)

**Settings → Video → Advanced Video**

| Sozlama | Tavsiya | Izoh |
|---|---|---|
| **Multisampling Anti-Aliasing** | `2x MSAA` yoki `None` | **Eng qimmat sozlama.** 8x dan 2x ga tushirish katta farq qiladi |
| **Global Shadow Quality** | `Low` | Ikkinchi eng qimmat |
| **Dynamic Shadows** | `Sun only` | Barcha soyalar juda qimmat |
| **Model / Texture Detail** | `Low` | Videokarta xotirasi kam bo'lsa muhim |
| **Shader Detail** | `Low` | |
| **Particle Detail** | `Low` | Tutun va portlashlarda FPS tushishini kamaytiradi |
| **Ambient Occlusion** | `Disabled` | |
| **High Dynamic Range** | `Performance` | |
| **Texture Filtering Mode** | `Bilinear` yoki `Anisotropic 2x` | Deyarli tekin, xohlagancha qoldiring |
| **Wait for Vertical Sync** | `Disabled` | V-Sync input lag qo'shadi |
| **NVIDIA Reflex Low Latency** | `Enabled` | FPS bermaydi, lekin **kechikishni kamaytiradi** |
| **Boost Player Contrast** | `Enabled` | FPS emas — **ko'rinuvchanlik uchun**, o'yinchilar fondan ajralib turadi |

**Resolution.** Eng katta ta'sir shu yerda. FPS juda kam bo'lsa pastroq
ruxsatga tushish har qanday sozlamadan ko'p beradi. Lekin bu rasmni bulut
qiladi — ko'zi yaxshi ko'rmaydigan odam uchun bu yomon savdo. Shuning uchun
oxirgi variant sifatida ko'ring.

**Laptop Power Savings** — noutbukda bo'lsa `Disabled`.

**FidelityFX Super Resolution** — FPS beradi, lekin rasmni yumshatadi.
Ko'rinuvchanlik muhim bo'lsa `Disabled` qoldiring.

---

## 2. Drayver va Windows

### Videokarta drayveri

Eng birinchi ish — **drayverni yangilash**. Eski drayver bilan CS2 (Source 2,
Vulkan/DX11) sezilarli sekin ishlaydi.

- NVIDIA: [nvidia.com/drivers](https://www.nvidia.com/Download/index.aspx)
- AMD: [amd.com/support](https://www.amd.com/en/support)

### NVIDIA Control Panel → Manage 3D settings → Program Settings → `cs2.exe`

| Sozlama | Qiymat |
|---|---|
| Power management mode | `Prefer maximum performance` |
| Low Latency Mode | `On` (o'yinda Reflex bo'lsa `Off` qoldiring) |
| Texture filtering - Quality | `High performance` |
| Vertical sync | `Off` |
| Image Sharpening | `Off` |

### Windows

1. **Game Mode**: Settings → Gaming → Game Mode → **On**
2. **Hardware-accelerated GPU scheduling**: Settings → System → Display →
   Graphics → Change default graphics settings → **On**
3. **Power plan**: Control Panel → Power Options → **High performance**
   (noutbukda quvvatga ulangan holda)
4. **Fullscreen optimizations o'chirish**: `cs2.exe` ustiga o'ng tugma →
   Properties → Compatibility → **Disable fullscreen optimizations** ✔
5. **Fonda ishlayotgan narsalarni yopish** — brauzer tablari, Discord stream,
   OBS, Chrome'dagi YouTube. Bu ro'yxatdagi eng arzon va eng ta'sirli qadam.

> **Diskda joy.** Tizim diskida 10–15 GB bo'sh joy qolmasa Windows'ning o'zi
> sekinlashadi.

---

## 3. Launch options

Steam → CS2 ustiga o'ng tugma → **Properties → Launch Options**:

```
-novid -nojoy -high
```

| Parametr | Nima qiladi |
|---|---|
| `-novid` | Boshlanishdagi Valve videosini o'tkazib yuboradi |
| `-nojoy` | Joystick qo'llab-quvvatlashini o'chiradi, ozgina xotira bo'shatadi |
| `-language english` | Ingliz tili (interfeys ruscha bo'lishi kerak bo'lmasa) |
| `-fullscreen` | Doim to'liq ekranda ochiladi |

> ### `-high` haqida ogohlantirish
>
> `-high` protsess ustuvorligini oshiradi. Ba'zi kompyuterlarda yordam beradi,
> ba'zilarida esa **teskari** — ovoz uzilishi va mikro-lag paydo bo'ladi,
> chunki o'yin drayver va audio jarayonlarini "bosib" qo'yadi.
>
> Qo'shib ko'ring, o'lchang, yomonlashsa **olib tashlang**.

**Ishlatmang:** `-threads`, `-processheap`, `-d3d9ex` va shunga o'xshash eski
CS:GO parametrlari — CS2 da ta'siri yo'q yoki zarar qiladi.

---

## 4. Convar'lar

```
exec visionassist_performance
```

Ichida nima borligi izohlar bilan yozilgan:
[`src/VisionAssist.Companion/cfg/visionassist_performance.cfg`](src/VisionAssist.Companion/cfg/visionassist_performance.cfg)

Asosiylari:

| Convar | Qiymat | Izoh |
|---|---|---|
| `fps_max` | `0` | Cheklovsiz. Videokarta qizib ketsa, monitor Hz'idan ozgina yuqori qiymat qo'ying (144 Hz → `160`) |
| `fps_max_ui` | `120` | Menyu 400 FPS'da ishlashi kerak emas |
| `cl_disablefreezecam` | `1` | O'lgandan keyingi kamera — FPS ham oladi, jonli raundni ham yashiradi |
| `r_drawtracers_firstperson` | `0` | O'z o'qingizning izi |
| `cl_hide_avatar_images` | `1` | Scoreboard'dagi avatarlar |

Har safar avtomatik bo'lishi uchun `game\csgo\cfg\autoexec.cfg` ga:

```
exec visionassist_performance
exec visionassist_accessibility
```

---

## Overlay o'zi qancha FPS oladi?

Halol javob: **oladi.** Brauzer oynasi ham CPU, ham GPU ishlatadi. Odatda
bir necha FPS, lekin sezilishi mumkin.

Kamaytirish yo'llari:

- **Ikkinchi monitorda** ishlatsangiz eng arzon;
- overlay sozlamalarida o'lchamni **S** yoki **M** qiling — kichik oyna kamroq
  piksel chizadi;
- ovoz paneli kerak bo'lmasa **O'chirish** bosing — AudioWorklet ham,
  ekran ulashish ham resurs oladi;
- brauzerda **faqat shu bir tab** ochiq bo'lsin;
- Chrome'ni `--app=` rejimida ochsangiz interfeys elementlari chizilmaydi:
  ```
  chrome.exe --app=http://127.0.0.1:47474/ --window-size=460,900
  ```

Agar FPS juda tor bo'lsa, tanlov sizda: overlay'ning foydasi bir necha FPS'ga
arziydimi yoki yo'q.

---

## Nima FPS bermaydi

Vaqtingizni bularga sarflamang:

- **"Optimizer" / "booster" / "game turbo" dasturlari** — yaxshi holatda hech
  narsa qilmaydi, yomon holatda reklama va zararli kod olib keladi;
- **RAM cleaner'lar** — Windows xotirani o'zi boshqaradi;
- **Registry tweak'lar** — CS2 ga ta'siri yo'q;
- **Internetdan olingan begona `autoexec.cfg`** — ichida nima borligini
  bilmaysiz, ko'pi eski CS:GO convar'lari, ba'zilari sozlamalaringizni buzadi;
- **`-threads N`** — Source 2 o'zi taqsimlaydi.
