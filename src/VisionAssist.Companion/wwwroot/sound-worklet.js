/*
  Runs on the audio thread, so it keeps measuring at a steady rate even when the
  browser window is behind the game and requestAnimationFrame has been throttled
  to a crawl. All it does is add up energy per channel and post the totals.
*/

const BLOCK_SAMPLES = 512; // ~10.7 ms at 48 kHz

class LevelProcessor extends AudioWorkletProcessor {
    constructor() {
        super();
        this._sumLeft = 0;
        this._sumRight = 0;
        this._samples = 0;
    }

    process(inputs) {
        const input = inputs[0];

        // No input connected yet, or the stream ended.
        if (!input || input.length === 0 || !input[0]) return true;

        const left = input[0];
        // A mono source reports one channel; treating it as both keeps the
        // maths valid, and the page separately warns that pan will read centre.
        const right = input.length > 1 && input[1] ? input[1] : left;

        for (let i = 0; i < left.length; i++) {
            this._sumLeft += left[i] * left[i];
            this._sumRight += right[i] * right[i];
        }
        this._samples += left.length;

        if (this._samples >= BLOCK_SAMPLES) {
            this.port.postMessage({
                l: this._sumLeft / this._samples,
                r: this._sumRight / this._samples,
                n: this._samples,
            });
            this._sumLeft = 0;
            this._sumRight = 0;
            this._samples = 0;
        }

        return true;
    }
}

registerProcessor('visionassist-level', LevelProcessor);
