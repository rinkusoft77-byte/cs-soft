/*
  Overlay page.

  Two independent inputs:
    - /events, a server-sent-events stream of the state the game reports about
      you through Game State Integration;
    - the sound panel, which measures audio already coming out of your speakers.

  Neither one knows anything about other players. Everything drawn here is
  either your own HUD data or a property of sound you can already hear.
*/

(function () {
    'use strict';

    // ------------------------------------------------------------------- i18n

    const STRINGS = {
        uz: {
            waiting: 'kutilmoqda', connected: 'ulandi', noGame: "o'yin yopiq", menu: 'menyu',
            health: 'JON', armor: 'ZIRH', weapon: 'QUROL', round: 'RAUND',
            money: 'PUL', score: 'HISOB', utility: 'GRANATA', defusekit: 'KIT',
            hasbomb: 'BOMBA SIZDA', helmet: 'kaska bor', noWeapon: "qurol yo'q",
            freezetime: 'TAYYORLANISH', live: 'RAUND', roundOver: 'RAUND TUGADI',
            warmup: 'QIZDIRISH', paused: "TO'XTATILDI", bomb: 'BOMBA', defuse: 'ZARARSIZLANTIRISH',
            alertBomb: "BOMBA QO'YILDI", alertBurn: 'YONAYAPSIZ', alertFlash: "KO'ZINGIZ KO'RMAYDI",
            alertSmoke: 'TUTUN ICHIDA', alertDefused: 'ZARARSIZLANTIRILDI', alertExploded: 'BOMBA PORTLADI',
            soundTitle: "Ovoz yo'nalishi", soundStart: 'Yoqish', soundStop: "O'chirish", soundOff: "O'CHIRILGAN",
            soundIdle: "O'chirilgan. Yoqilganda tizim ovozini eshitib, chap/o'ng yo'nalishini ko'rsatadi.",
            soundRunning: "Ishlayapti. Chap/o'ng ko'rsatiladi; old va orqa farqi stereo ovozda yo'q.",
            soundMono: "Ishlayapti, lekin ovoz mono keldi — yo'nalish aniqlanmaydi. Windows'da ovoz chiqishini stereo qilib qo'ying.",
            soundDenied: 'Ruxsat berilmadi. Brauzer so\'raganda ruxsat bering.',
            soundNoAudio: 'Ovoz ulashilmadi. Ekran tanlashda "Tizim ovozini ham ulashish" belgisini qo\'ying.',
            soundUnsupported: 'Brauzer bu funksiyani qo\'llab-quvvatlamaydi. Chrome yoki Edge ishlatib ko\'ring.',
            soundError: 'Ovozni yoqib bo\'lmadi',
            settings: 'Sozlamalar', language: 'Til', theme: "Rang to'plami", size: "O'lcham",
            transparent: "Shaffof fon (o'yin ustiga qo'yish uchun)", soundSource: 'Ovoz manbasi',
            sourceDisplay: 'Ekran ulashish (tizim ovozi)', sourceDevice: 'Kirish qurilmasi (Stereo Mix)',
            soundDevice: 'Qurilma', sensitivity: 'Sezgirlik',
            soundHelp: "Chap/o'ng aniqlanadi. Old va orqa farqi stereo signalda yo'q — buni hech qanday dastur ovozdan chiqarib bera olmaydi.",
            about: "Faqat o'zingizning holatingiz ko'rsatiladi. Dushman joylashuvi yo'q — GSI uni bermaydi va bu dastur o'yin xotirasini o'qimaydi.",
            left: 'CHAP', right: "O'NG",
            offline: "O'yin ma'lumot yubormayapti. CS2 ochiqmi? gamestate_integration_visionassist.cfg joyidami?",
            spectating: "Jonli raundda emassiz (menyu yoki kuzatuv).",
        },
        ru: {
            waiting: 'ожидание', connected: 'подключено', noGame: 'игра закрыта', menu: 'меню',
            health: 'ЗДОРОВЬЕ', armor: 'БРОНЯ', weapon: 'ОРУЖИЕ', round: 'РАУНД',
            money: 'ДЕНЬГИ', score: 'СЧЁТ', utility: 'ГРАНАТЫ', defusekit: 'НАБОР',
            hasbomb: 'БОМБА У ВАС', helmet: 'шлем есть', noWeapon: 'нет оружия',
            freezetime: 'ПОДГОТОВКА', live: 'РАУНД', roundOver: 'РАУНД ОКОНЧЕН',
            warmup: 'РАЗМИНКА', paused: 'ПАУЗА', bomb: 'БОМБА', defuse: 'РАЗМИНИРОВАНИЕ',
            alertBomb: 'БОМБА УСТАНОВЛЕНА', alertBurn: 'ВЫ ГОРИТЕ', alertFlash: 'ВЫ ОСЛЕПЛЕНЫ',
            alertSmoke: 'В ДЫМУ', alertDefused: 'РАЗМИНИРОВАНА', alertExploded: 'БОМБА ВЗОРВАЛАСЬ',
            soundTitle: 'Направление звука', soundStart: 'Включить', soundStop: 'Выключить', soundOff: 'ВЫКЛЮЧЕНО',
            soundIdle: 'Выключено. После включения показывает направление звука влево/вправо.',
            soundRunning: 'Работает. Показывает лево/право; спереди/сзади в стереосигнале не различить.',
            soundMono: 'Работает, но звук пришёл моно — направление определить нельзя. Включите стерео в настройках звука Windows.',
            soundDenied: 'Доступ не разрешён. Разрешите, когда браузер спросит.',
            soundNoAudio: 'Звук не был передан. Отметьте «Поделиться системным звуком» при выборе экрана.',
            soundUnsupported: 'Браузер это не поддерживает. Попробуйте Chrome или Edge.',
            soundError: 'Не удалось включить звук',
            settings: 'Настройки', language: 'Язык', theme: 'Цветовая схема', size: 'Размер',
            transparent: 'Прозрачный фон (поверх игры)', soundSource: 'Источник звука',
            sourceDisplay: 'Захват экрана (системный звук)', sourceDevice: 'Устройство ввода (Stereo Mix)',
            soundDevice: 'Устройство', sensitivity: 'Чувствительность',
            soundHelp: 'Определяется только лево/право. Спереди/сзади в стереосигнале отсутствует — этого не может извлечь никакая программа.',
            about: 'Показывается только ваше собственное состояние. Позиций противников нет — GSI их не даёт, и эта программа не читает память игры.',
            left: 'ЛЕВО', right: 'ПРАВО',
            offline: 'Игра ничего не присылает. CS2 запущен? gamestate_integration_visionassist.cfg на месте?',
            spectating: 'Вы не в живом раунде (меню или наблюдение).',
        },
        en: {
            waiting: 'waiting', connected: 'connected', noGame: 'game closed', menu: 'menu',
            health: 'HEALTH', armor: 'ARMOR', weapon: 'WEAPON', round: 'ROUND',
            money: 'MONEY', score: 'SCORE', utility: 'UTILITY', defusekit: 'KIT',
            hasbomb: 'YOU HAVE THE BOMB', helmet: 'helmet', noWeapon: 'no weapon',
            freezetime: 'FREEZE TIME', live: 'ROUND', roundOver: 'ROUND OVER',
            warmup: 'WARMUP', paused: 'PAUSED', bomb: 'BOMB', defuse: 'DEFUSING',
            alertBomb: 'BOMB PLANTED', alertBurn: 'YOU ARE BURNING', alertFlash: 'YOU ARE BLIND',
            alertSmoke: 'IN SMOKE', alertDefused: 'DEFUSED', alertExploded: 'BOMB EXPLODED',
            soundTitle: 'Sound direction', soundStart: 'Start', soundStop: 'Stop', soundOff: 'OFF',
            soundIdle: 'Off. Once started it listens to system audio and shows left/right direction.',
            soundRunning: 'Running. Left/right only; front and back are not present in a stereo signal.',
            soundMono: 'Running, but the audio arrived as mono, so direction cannot be worked out. Set your Windows output to stereo.',
            soundDenied: 'Permission refused. Allow it when the browser asks.',
            soundNoAudio: 'No audio was shared. Tick "Also share system audio" in the picker.',
            soundUnsupported: 'This browser does not support it. Try Chrome or Edge.',
            soundError: 'Could not start audio',
            settings: 'Settings', language: 'Language', theme: 'Colour preset', size: 'Size',
            transparent: 'Transparent background (for stacking over the game)', soundSource: 'Audio source',
            sourceDisplay: 'Screen share (system audio)', sourceDevice: 'Input device (Stereo Mix)',
            soundDevice: 'Device', sensitivity: 'Sensitivity',
            soundHelp: 'Left and right only. Front versus back is not in a stereo signal - no program can recover it from audio.',
            about: 'Only your own state is shown. No enemy positions: GSI does not provide them and this program does not read game memory.',
            left: 'LEFT', right: 'RIGHT',
            offline: 'Nothing is arriving from the game. Is CS2 running, and is gamestate_integration_visionassist.cfg in place?',
            spectating: 'Not in a live round (menu or spectating).',
        },
    };

    let lang = 'uz';
    const t = (key) => (STRINGS[lang] && STRINGS[lang][key]) || STRINGS.en[key] || key;

    // ------------------------------------------------------------------ state

    const el = (id) => document.getElementById(id);
    const root = document.documentElement;

    let snapshot = null;
    let countdown = { phase: null, seconds: null, at: 0 };

    const sound = new window.SoundDirection();
    let soundSource = 'display';
    let soundDeviceId = '';

    // ----------------------------------------------------------- preferences

    const PREFS_KEY = 'visionassist.overlay';

    function loadPrefs() {
        try {
            return JSON.parse(localStorage.getItem(PREFS_KEY) || '{}');
        } catch (e) {
            return {};
        }
    }

    function savePrefs(patch) {
        const merged = Object.assign(loadPrefs(), patch);
        try {
            localStorage.setItem(PREFS_KEY, JSON.stringify(merged));
        } catch (e) {
            // Private mode. The overlay still works, the choice just is not kept.
        }
    }

    // --------------------------------------------------------------- rendering

    function applyLanguage(next) {
        lang = STRINGS[next] ? next : 'uz';
        root.setAttribute('lang', lang);

        document.querySelectorAll('[data-i18n]').forEach((node) => {
            node.textContent = t(node.getAttribute('data-i18n'));
        });

        el('settings-about').textContent = t('about');
        renderSnapshot();
        renderSoundStatus(sound.running ? (sound.stereo ? 'running' : 'running-mono') : 'stopped');
    }

    function phaseLabel(phase) {
        switch (phase) {
            case 'freezetime': return t('freezetime');
            case 'live': return t('live');
            case 'over': return t('roundOver');
            case 'warmup': return t('warmup');
            case 'paused': return t('paused');
            case 'bomb': return t('bomb');
            case 'defuse': return t('defuse');
            default: return t('round');
        }
    }

    function formatSeconds(seconds) {
        if (seconds === null || seconds === undefined) return '–';
        if (seconds < 10) return seconds.toFixed(1);
        if (seconds < 60) return String(Math.ceil(seconds));
        const minutes = Math.floor(seconds / 60);
        const rest = Math.floor(seconds % 60);
        return minutes + ':' + String(rest).padStart(2, '0');
    }

    function renderConnection() {
        const pill = el('connection');
        if (!snapshot || !snapshot.connected) {
            pill.className = 'pill pill-off';
            pill.textContent = snapshot ? t('noGame') : t('waiting');
            return;
        }
        if (!snapshot.inGame) {
            pill.className = 'pill pill-warn';
            pill.textContent = t('menu');
            return;
        }
        pill.className = 'pill pill-on';
        pill.textContent = t('connected');
    }

    function renderAlerts() {
        const host = el('alerts');
        host.textContent = '';
        if (!snapshot || !snapshot.connected) {
            if (snapshot) addAlert(host, 'alert-smoke', t('offline'));
            return;
        }
        if (!snapshot.inGame) {
            addAlert(host, 'alert-smoke', t('spectating'));
            return;
        }

        if (snapshot.bombState === 'planted') addAlert(host, 'alert-bomb', t('alertBomb'));
        if (snapshot.bombState === 'defused') addAlert(host, 'alert-defuse', t('alertDefused'));
        if (snapshot.bombState === 'exploded') addAlert(host, 'alert-bomb', t('alertExploded'));
        if (snapshot.burning > 0) addAlert(host, 'alert-burn', t('alertBurn'));
        if (snapshot.flashed > 60) addAlert(host, 'alert-flash', t('alertFlash'));
        if (snapshot.smoked > 60) addAlert(host, 'alert-smoke', t('alertSmoke'));
    }

    function addAlert(host, className, text) {
        const node = document.createElement('div');
        node.className = 'alert ' + className;
        node.textContent = text;
        host.appendChild(node);
    }

    function renderSnapshot() {
        renderConnection();
        renderAlerts();

        if (!snapshot) return;

        const team = (snapshot.team || '').toLowerCase();
        root.setAttribute('data-team', team === 't' || team === 'ct' ? team : 'none');

        el('map-name').textContent = snapshot.mapName || '';
        el('score-ct').textContent = snapshot.scoreCt;
        el('score-t').textContent = snapshot.scoreT;
        el('round-number').textContent = snapshot.roundNumber
            ? t('round') + ' ' + snapshot.roundNumber : '';
        el('player-name').textContent = snapshot.playerName || '';

        // Health.
        const healthCard = el('card-health');
        el('value-health').textContent = snapshot.inGame ? snapshot.health : '–';
        healthCard.classList.toggle('is-dead', snapshot.inGame && snapshot.health <= 0);
        healthCard.classList.toggle('is-critical',
            snapshot.health > 0 && snapshot.health <= lowHealthThreshold);
        healthCard.classList.toggle('is-hurt',
            snapshot.health > lowHealthThreshold && snapshot.health < 100);

        // Armour.
        el('value-armor').textContent = snapshot.inGame ? snapshot.armor : '–';
        el('note-helmet').textContent = snapshot.helmet ? t('helmet') : '';

        // Weapon and ammo.
        const ammoCard = el('card-ammo');
        el('label-weapon').textContent = snapshot.weaponName || t('weapon');
        if (snapshot.ammoClip === null || snapshot.ammoClip === undefined) {
            el('value-ammo').textContent = snapshot.weaponName ? '–' : t('noWeapon');
            el('note-reserve').textContent = '';
            ammoCard.classList.remove('is-empty');
        } else {
            el('value-ammo').textContent = snapshot.ammoClip;
            el('note-reserve').textContent = (snapshot.ammoReserve !== null
                && snapshot.ammoReserve !== undefined) ? '+ ' + snapshot.ammoReserve : '';
            ammoCard.classList.toggle('is-empty', snapshot.ammoClip === 0);
        }
        ammoCard.classList.toggle('is-reloading', !!snapshot.reloading);

        // Extras.
        el('value-money').textContent = snapshot.inGame ? '$' + snapshot.money : '–';
        el('value-kills').textContent = snapshot.kills + ' / ' + snapshot.deaths;
        el('value-utility').textContent = (snapshot.grenades && snapshot.grenades.length)
            ? snapshot.grenades.join(', ') : '–';
        el('chip-kit').classList.toggle('hidden', !snapshot.defuseKit);
        el('chip-bombcarry').classList.toggle('hidden', !snapshot.hasBomb);

        renderTimer();
    }

    /**
     * The game sends the remaining time once per update; between updates the
     * page counts down on its own so the number does not visibly step.
     */
    function renderTimer() {
        const card = el('card-timer');
        const phase = countdown.phase;
        el('label-timer').textContent = phaseLabel(phase);
        card.classList.toggle('is-bomb', phase === 'bomb');

        if (countdown.seconds === null || countdown.seconds === undefined || !snapshot
            || !snapshot.connected) {
            el('value-timer').textContent = '–';
            return;
        }

        const elapsed = (performance.now() - countdown.at) / 1000;
        el('value-timer').textContent = formatSeconds(Math.max(0, countdown.seconds - elapsed));
    }

    // ------------------------------------------------------------ sound panel

    const canvas = el('sound-canvas');
    const ctx2d = canvas.getContext('2d');

    function cssVar(name) {
        return getComputedStyle(root).getPropertyValue(name).trim() || '#ffffff';
    }

    function drawSound() {
        const dpr = window.devicePixelRatio || 1;
        const width = Math.max(1, Math.round(canvas.clientWidth * dpr));
        const height = Math.max(1, Math.round(canvas.clientHeight * dpr));
        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width;
            canvas.height = height;
        }

        const accent = cssVar('--accent');
        const muted = cssVar('--muted');
        const danger = cssVar('--danger');

        ctx2d.clearRect(0, 0, width, height);

        const pad = Math.round(height * 0.16);
        const axisY = height - pad;
        const scale = height / 150;

        // Axis with a centre tick and the two edge labels.
        ctx2d.strokeStyle = muted;
        ctx2d.globalAlpha = 0.35;
        ctx2d.lineWidth = Math.max(1, scale);
        ctx2d.beginPath();
        ctx2d.moveTo(pad, axisY);
        ctx2d.lineTo(width - pad, axisY);
        ctx2d.moveTo(width / 2, axisY - 6 * scale);
        ctx2d.lineTo(width / 2, axisY + 6 * scale);
        ctx2d.stroke();
        ctx2d.globalAlpha = 1;

        ctx2d.fillStyle = muted;
        ctx2d.font = Math.round(13 * scale) + 'px system-ui, sans-serif';
        ctx2d.textBaseline = 'middle';
        ctx2d.textAlign = 'left';
        ctx2d.fillText(t('left'), pad, axisY + 14 * scale);
        ctx2d.textAlign = 'right';
        ctx2d.fillText(t('right'), width - pad, axisY + 14 * scale);

        if (!sound.running) {
            ctx2d.textAlign = 'center';
            ctx2d.fillText(t('soundOff'), width / 2, height / 2);
            return;
        }

        sound.prune();

        const usable = width - pad * 2;
        const xFor = (pan) => pad + ((Math.max(-1, Math.min(1, pan)) + 1) / 2) * usable;

        // Live needle: horizontal position is the stereo balance, height is how
        // loud the band is right now.
        const loudness = Math.max(0, Math.min(1, (sound.levelDb + 80) / 60));
        const needleX = xFor(sound.pan);
        ctx2d.strokeStyle = accent;
        ctx2d.lineWidth = Math.max(2, 3 * scale);
        ctx2d.beginPath();
        ctx2d.moveTo(needleX, axisY);
        ctx2d.lineTo(needleX, axisY - loudness * (axisY - pad));
        ctx2d.stroke();

        // Events fall away from the axis as they age, newest closest to it, so a
        // run of footsteps reads as a rhythm rather than a single blob.
        const now = performance.now();
        for (const event of sound.events) {
            const age = (now - event.at) / 4000;
            if (age >= 1) continue;

            const radius = Math.max(3 * scale, Math.min(11 * scale, 3 * scale + Math.log2(event.ratio) * 2 * scale));
            ctx2d.globalAlpha = 1 - age;
            ctx2d.fillStyle = event.loud ? danger : accent;
            ctx2d.beginPath();
            ctx2d.arc(xFor(event.pan), pad + age * (axisY - pad - radius), radius, 0, Math.PI * 2);
            ctx2d.fill();
        }
        ctx2d.globalAlpha = 1;
    }

    function renderSoundStatus(status) {
        const button = el('sound-toggle');
        const note = el('sound-status');

        switch (status) {
            case 'running':
                button.textContent = t('soundStop');
                button.classList.add('is-active');
                note.textContent = t('soundRunning');
                break;
            case 'running-mono':
                button.textContent = t('soundStop');
                button.classList.add('is-active');
                note.textContent = t('soundMono');
                break;
            default:
                button.textContent = t('soundStart');
                button.classList.remove('is-active');
                note.textContent = t('soundIdle');
                break;
        }
    }

    sound.onstatus = renderSoundStatus;

    async function toggleSound() {
        if (sound.running) {
            sound.stop();
            return;
        }

        try {
            await sound.start(soundSource, soundDeviceId);
        } catch (error) {
            let message;
            if (error && error.message === 'no-audio-shared') message = t('soundNoAudio');
            else if (error && error.message === 'unsupported') message = t('soundUnsupported');
            else if (error && (error.name === 'NotAllowedError' || error.name === 'SecurityError')) {
                message = t('soundDenied');
            } else {
                message = t('soundError') + ': ' + ((error && error.message) || error);
            }

            // Reset the button first, then replace the generic note with the
            // reason it failed.
            renderSoundStatus('stopped');
            el('sound-status').textContent = message;
        }
    }

    // --------------------------------------------------------------- transport

    let pollTimer = 0;

    function connect() {
        let stream;
        try {
            stream = new EventSource('/events');
        } catch (e) {
            startPolling();
            return;
        }

        stream.addEventListener('state', (event) => {
            try {
                applySnapshot(JSON.parse(event.data));
            } catch (e) {
                // A truncated frame; the next one will be complete.
            }
        });

        // EventSource reconnects by itself, so there is nothing to do here but
        // show that the link is down.
        stream.addEventListener('error', () => {
            if (snapshot) {
                snapshot = Object.assign({}, snapshot, { connected: false });
                renderSnapshot();
            }
        });
    }

    function startPolling() {
        if (pollTimer) return;
        pollTimer = setInterval(async () => {
            try {
                const response = await fetch('/state', { cache: 'no-store' });
                applySnapshot(await response.json());
            } catch (e) {
                // Server not up yet.
            }
        }, 500);
    }

    function applySnapshot(next) {
        snapshot = next;
        countdown = {
            phase: next.countdownPhase || next.roundPhase || null,
            seconds: next.countdownSeconds,
            at: performance.now(),
        };
        renderSnapshot();
    }

    // ---------------------------------------------------------------- settings

    let lowHealthThreshold = 35;

    async function loadServerConfig() {
        try {
            const response = await fetch('/config', { cache: 'no-store' });
            return await response.json();
        } catch (e) {
            return {};
        }
    }

    function wireSettings() {
        el('settings-toggle').addEventListener('click', () => {
            const panel = el('settings');
            const open = panel.hidden;
            panel.hidden = !open;
            el('settings-toggle').setAttribute('aria-expanded', String(open));
        });

        el('select-language').addEventListener('change', (e) => {
            applyLanguage(e.target.value);
            savePrefs({ lang: e.target.value });
        });

        el('select-theme').addEventListener('change', (e) => {
            root.setAttribute('data-theme', e.target.value);
            savePrefs({ theme: e.target.value });
        });

        el('select-size').addEventListener('change', (e) => {
            root.setAttribute('data-size', e.target.value);
            savePrefs({ size: e.target.value });
        });

        el('check-transparent').addEventListener('change', (e) => {
            root.setAttribute('data-transparent', String(e.target.checked));
            savePrefs({ transparent: e.target.checked });
        });

        el('range-sensitivity').addEventListener('input', (e) => {
            sound.setSensitivity(e.target.value);
            savePrefs({ sensitivity: Number(e.target.value) });
        });

        el('select-sound-source').addEventListener('change', async (e) => {
            soundSource = e.target.value;
            savePrefs({ soundSource: soundSource });
            el('field-sound-device').classList.toggle('hidden', soundSource !== 'device');
            if (soundSource === 'device') await fillDeviceList();
        });

        el('select-sound-device').addEventListener('change', (e) => {
            soundDeviceId = e.target.value;
            savePrefs({ soundDeviceId: soundDeviceId });
        });

        el('sound-toggle').addEventListener('click', toggleSound);
    }

    async function fillDeviceList() {
        const select = el('select-sound-device');
        select.textContent = '';

        let devices = [];
        try {
            devices = await sound.listDevices();
        } catch (e) {
            devices = [];
        }

        for (const device of devices) {
            const option = document.createElement('option');
            option.value = device.id;
            option.textContent = device.label;
            select.appendChild(option);
        }

        if (soundDeviceId) select.value = soundDeviceId;
        else soundDeviceId = select.value || '';
    }

    function applyPreferences(serverConfig) {
        const prefs = loadPrefs();

        const theme = prefs.theme || serverConfig.theme || 'highcontrast';
        const size = prefs.size || serverConfig.size || 'l';
        const transparent = prefs.transparent !== undefined
            ? prefs.transparent : !!serverConfig.transparentBackground;
        const sensitivity = prefs.sensitivity !== undefined
            ? prefs.sensitivity
            : (serverConfig.soundSensitivity !== undefined ? serverConfig.soundSensitivity : 0.5);

        lowHealthThreshold = serverConfig.lowHealthThreshold || 35;
        soundSource = prefs.soundSource || 'display';
        soundDeviceId = prefs.soundDeviceId || '';

        root.setAttribute('data-theme', theme);
        root.setAttribute('data-size', size);
        root.setAttribute('data-transparent', String(transparent));

        el('select-theme').value = theme;
        el('select-size').value = size;
        el('check-transparent').checked = transparent;
        el('range-sensitivity').value = sensitivity;
        el('select-sound-source').value = soundSource;
        el('field-sound-device').classList.toggle('hidden', soundSource !== 'device');

        sound.setSensitivity(sensitivity);

        if (serverConfig.soundIndicatorEnabled === false) el('sound').classList.add('hidden');

        const language = prefs.lang || 'uz';
        el('select-language').value = language;
        applyLanguage(language);
    }

    // -------------------------------------------------------------------- boot

    async function main() {
        wireSettings();
        applyPreferences(await loadServerConfig());
        connect();

        // One clock for both: the countdown needs tenths, the canvas wants to
        // fade events smoothly, and neither is worth its own timer.
        setInterval(renderTimer, 100);
        const frame = () => {
            drawSound();
            requestAnimationFrame(frame);
        };
        requestAnimationFrame(frame);
    }

    main();
})();
