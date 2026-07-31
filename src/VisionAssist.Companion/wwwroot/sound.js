/*
  Sound direction.

  Listens to the audio your speakers are already playing and reports where each
  transient sits in the stereo field, so a footstep you cannot hear well still
  shows up as a mark on the left or the right.

  What it cannot do, and no program can: tell front from back. That difference
  lives in HRTF filtering, not in the left/right balance, and it is gone by the
  time the signal reaches a loopback capture. Nothing here reads the game - it
  only measures sound the game already sent to your ears.
*/

(function () {
    'use strict';

    // Footsteps are broadband thumps; this window keeps them and drops both the
    // low rumble of ambience and the high hiss of gunfire tails.
    const HIGHPASS_HZ = 90;
    const LOWPASS_HZ = 1200;

    const FAST_ALPHA = 0.4;      // ~30 ms - follows the attack of a step
    const BASELINE_ALPHA = 0.006; // ~1.8 s - what "quiet" currently means
    const PAN_ALPHA = 0.35;
    const EVENT_COOLDOWN_MS = 110;
    const EVENT_MEMORY_MS = 4000;
    const LOUD_RATIO = 12;       // above this it is gunfire or an explosion
    const EPSILON = 1e-12;

    class SoundDirection {
        constructor() {
            this.running = false;
            this.stereo = true;

            /** -1 hard left, 0 centre, +1 hard right. */
            this.pan = 0;
            /** Band energy in dB, for the level bars. */
            this.levelDb = -100;
            /** How far above the running baseline the signal is right now. */
            this.ratio = 1;
            /** Recent transients: { at, pan, ratio, loud }. */
            this.events = [];

            this.sensitivity = 0.5;
            this.onstatus = null;

            this._stream = null;
            this._ctx = null;
            this._node = null;
            this._fast = 0;
            this._baseline = 0;
            this._lastEventAt = 0;
        }

        get supported() {
            return !!(window.AudioContext && navigator.mediaDevices
                && window.AudioWorkletNode);
        }

        setSensitivity(value) {
            this.sensitivity = Math.min(1, Math.max(0, Number(value) || 0));
        }

        /**
         * source: 'display' captures system audio through the screen-share
         * picker; 'device' opens an input such as Realtek "Stereo Mix".
         */
        async start(source, deviceId) {
            if (this.running) return;
            if (!this.supported) throw new Error('unsupported');

            const stream = source === 'device'
                ? await this._openDevice(deviceId)
                : await this._openDisplay();

            this._stream = stream;

            const track = stream.getAudioTracks()[0];
            const settings = typeof track.getSettings === 'function' ? track.getSettings() : {};
            this.stereo = settings.channelCount === undefined || settings.channelCount >= 2;

            await this._buildGraph(stream);

            // A stream that ends (the share was stopped from the browser's own
            // bar) has to tear the graph down too, or the panel lies.
            track.addEventListener('ended', () => this.stop());

            this.running = true;
            this._report(this.stereo ? 'running' : 'running-mono');
        }

        stop() {
            const wasRunning = this.running;
            this.running = false;

            if (this._node) {
                this._node.port.onmessage = null;
                try { this._node.disconnect(); } catch (e) { /* already gone */ }
                this._node = null;
            }

            if (this._stream) {
                this._stream.getTracks().forEach((t) => t.stop());
                this._stream = null;
            }

            if (this._ctx) {
                this._ctx.close().catch(() => { /* already closed */ });
                this._ctx = null;
            }

            this._fast = 0;
            this._baseline = 0;
            this.pan = 0;
            this.levelDb = -100;
            this.ratio = 1;
            this.events = [];

            if (wasRunning) this._report('stopped');
        }

        /** Input devices, for the "Stereo Mix" route. Labels need permission. */
        async listDevices() {
            // Without an earlier grant the labels come back empty, so ask first.
            let probe = null;
            try {
                probe = await navigator.mediaDevices.getUserMedia({ audio: true });
            } catch (e) {
                // Denied - enumerate anyway, the ids still work.
            } finally {
                if (probe) probe.getTracks().forEach((t) => t.stop());
            }

            const devices = await navigator.mediaDevices.enumerateDevices();
            return devices
                .filter((d) => d.kind === 'audioinput')
                .map((d, i) => ({ id: d.deviceId, label: d.label || ('Input ' + (i + 1)) }));
        }

        // ------------------------------------------------------------- capture

        async _openDisplay() {
            // Chrome only offers the "share system audio" tick when video is
            // requested too, so the video track is asked for and then ignored.
            // It must not be stopped: stopping it ends the whole share.
            const stream = await navigator.mediaDevices.getDisplayMedia({
                video: true,
                audio: {
                    echoCancellation: false,
                    noiseSuppression: false,
                    autoGainControl: false,
                    channelCount: 2,
                },
            });

            if (stream.getAudioTracks().length === 0) {
                stream.getTracks().forEach((t) => t.stop());
                throw new Error('no-audio-shared');
            }

            return stream;
        }

        async _openDevice(deviceId) {
            const audio = {
                echoCancellation: false,
                noiseSuppression: false,
                autoGainControl: false,
                channelCount: 2,
            };
            if (deviceId) audio.deviceId = { exact: deviceId };

            return navigator.mediaDevices.getUserMedia({ audio: audio });
        }

        // --------------------------------------------------------------- graph

        async _buildGraph(stream) {
            const ctx = new AudioContext();
            this._ctx = ctx;

            await ctx.audioWorklet.addModule('sound-worklet.js');

            const source = ctx.createMediaStreamSource(stream);

            const highpass = ctx.createBiquadFilter();
            highpass.type = 'highpass';
            highpass.frequency.value = HIGHPASS_HZ;

            const lowpass = ctx.createBiquadFilter();
            lowpass.type = 'lowpass';
            lowpass.frequency.value = LOWPASS_HZ;

            const node = new AudioWorkletNode(ctx, 'visionassist-level', {
                numberOfInputs: 1,
                numberOfOutputs: 0,
                channelCount: 2,
                channelCountMode: 'explicit',
                channelInterpretation: 'discrete',
            });

            node.port.onmessage = (event) => this._measure(event.data);
            this._node = node;

            source.connect(highpass);
            highpass.connect(lowpass);
            lowpass.connect(node);

            // Nothing is connected to ctx.destination: the game is already
            // playing this audio, and routing it back would echo.
            if (ctx.state === 'suspended') await ctx.resume();
        }

        /** One block of energy from the worklet. */
        _measure(data) {
            const left = data.l || 0;
            const right = data.r || 0;
            const total = left + right;

            this._fast = this._fast + FAST_ALPHA * (total - this._fast);
            this._baseline = this._baseline + BASELINE_ALPHA * (total - this._baseline);

            const targetPan = (right - left) / (total + EPSILON);
            this.pan = this.pan + PAN_ALPHA * (targetPan - this.pan);

            this.levelDb = 10 * Math.log10(this._fast + EPSILON);
            this.ratio = this._fast / (this._baseline + EPSILON);

            this._detect();
        }

        /**
         * A transient is a jump above the running baseline. Both thresholds move
         * with the sensitivity slider, because "loud enough to matter" depends
         * entirely on how the player has their volume set.
         */
        _detect() {
            const now = performance.now();
            if (now - this._lastEventAt < EVENT_COOLDOWN_MS) return;

            const ratioNeeded = 6 - 4 * this.sensitivity;
            const dbNeeded = -78 + (1 - this.sensitivity) * 22;

            if (this.ratio < ratioNeeded || this.levelDb < dbNeeded) return;

            this._lastEventAt = now;
            this.events.push({
                at: now,
                pan: this.pan,
                ratio: this.ratio,
                loud: this.ratio > LOUD_RATIO,
            });

            this._forget(now);
        }

        _forget(now) {
            while (this.events.length > 0 && now - this.events[0].at > EVENT_MEMORY_MS) {
                this.events.shift();
            }
            // Nothing sane produces this many, but a runaway list must not grow
            // without bound if the clock jumps.
            if (this.events.length > 256) this.events.splice(0, this.events.length - 256);
        }

        /** Drops stale events without waiting for a new one. Called by the renderer. */
        prune() {
            this._forget(performance.now());
        }

        _report(status) {
            if (typeof this.onstatus === 'function') this.onstatus(status);
        }
    }

    window.SoundDirection = SoundDirection;
})();
