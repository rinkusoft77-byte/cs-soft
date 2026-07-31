using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace VisionAssist.Overlay;

/// <summary>A transient that was loud enough to be worth marking.</summary>
internal readonly record struct SoundEvent(long At, double Pan, double Ratio, bool Loud);

/// <summary>
/// Turns what the speakers are playing into a left/right reading.
///
/// WASAPI loopback, which is the whole reason the native program is nicer than
/// the browser version: it reads the default output device directly, with no
/// screen-share picker and no permission prompt, and it cannot be forgotten
/// about halfway through a match.
///
/// Left and right only. Front versus back lives in the HRTF filtering that has
/// already been applied by the time audio reaches the output mix, and no amount
/// of processing here can pull it back out. Nothing about the game is read.
/// </summary>
internal sealed class SoundMeter : IDisposable
{
    private const double HighPassHz = 90;
    private const double LowPassHz = 1200;

    private const double FastAlpha = 0.4;       // ~30 ms, follows a footstep's attack
    private const double BaselineAlpha = 0.006; // ~1.8 s, what "quiet" means right now
    private const double PanAlpha = 0.35;
    private const double LoudRatio = 12;        // above this it is gunfire, not a step
    private const long EventCooldownMs = 110;
    private const long EventMemoryMs = 4000;
    private const double Epsilon = 1e-12;

    private readonly object _gate = new();
    private readonly List<SoundEvent> _events = new();

    private WasapiLoopbackCapture? _capture;
    private WaveFormat? _format;
    private Biquad? _highLeft, _highRight, _lowLeft, _lowRight;

    private int _framesPerBlock;
    private int _framesInBlock;
    private double _sumLeft, _sumRight;

    private double _fast, _baseline;
    private long _lastEventAt;

    private double _pan;
    private double _levelDb = -100;
    private long _blocks;
    private long _lastAudibleAt;
    private long _startedAt;

    /// <summary>0.0 = only obvious sounds, 1.0 = very twitchy.</summary>
    public double Sensitivity { get; set; } = 0.5;

    public bool Running { get; private set; }

    /// <summary>Why the last <see cref="Start"/> failed, or null.</summary>
    public string? LastError { get; private set; }

    public bool Stereo { get; private set; } = true;

    public double Pan { get { lock (_gate) return _pan; } }

    public double LevelDb { get { lock (_gate) return _levelDb; } }

    public long Blocks { get { lock (_gate) return _blocks; } }

    /// <summary>
    /// What the panel should say. A live capture that delivers nothing looks
    /// exactly like a broken one from the outside, so the two are separated here.
    /// </summary>
    public SoundHealth Health
    {
        get
        {
            if (!Running) return SoundHealth.Stopped;

            lock (_gate)
            {
                long now = Environment.TickCount64;
                if (_blocks == 0)
                    return now - _startedAt > 1500 ? SoundHealth.NoBlocks : SoundHealth.Starting;
                if (now - _lastAudibleAt > 2000) return SoundHealth.Silent;
                return Stereo ? SoundHealth.Ok : SoundHealth.Mono;
            }
        }
    }

    /// <summary>A copy of the recent transients, oldest first, already pruned.</summary>
    public SoundEvent[] RecentEvents()
    {
        lock (_gate)
        {
            long cutoff = Environment.TickCount64 - EventMemoryMs;
            _events.RemoveAll(e => e.At < cutoff);
            return _events.ToArray();
        }
    }

    // ----------------------------------------------------------------- start

    public bool Start()
    {
        if (Running) return true;
        LastError = null;

        try
        {
            var capture = new WasapiLoopbackCapture();
            _format = capture.WaveFormat;

            Stereo = _format.Channels >= 2;

            // One block every ~10 ms, matching the browser version so the two
            // behave the same at the same sensitivity setting.
            _framesPerBlock = Math.Max(64, _format.SampleRate / 100);

            _highLeft = Biquad.HighPass(_format.SampleRate, HighPassHz);
            _highRight = Biquad.HighPass(_format.SampleRate, HighPassHz);
            _lowLeft = Biquad.LowPass(_format.SampleRate, LowPassHz);
            _lowRight = Biquad.LowPass(_format.SampleRate, LowPassHz);

            lock (_gate)
            {
                _events.Clear();
                _blocks = 0;
                _pan = 0;
                _levelDb = -100;
                _startedAt = Environment.TickCount64;
                _lastAudibleAt = _startedAt;
            }

            _framesInBlock = 0;
            _sumLeft = 0;
            _sumRight = 0;
            _fast = 0;
            _baseline = 0;

            capture.DataAvailable += OnDataAvailable;
            capture.RecordingStopped += OnRecordingStopped;
            capture.StartRecording();

            _capture = capture;
            Running = true;
            return true;
        }
        catch (Exception ex)
        {
            // No output device, exclusive mode held by something else, or the
            // audio service is down. None of them are worth crashing over.
            LastError = ex.Message;
            Running = false;
            return false;
        }
    }

    public void Stop()
    {
        var capture = _capture;
        _capture = null;
        Running = false;

        if (capture is null) return;

        capture.DataAvailable -= OnDataAvailable;
        capture.RecordingStopped -= OnRecordingStopped;

        try
        {
            capture.StopRecording();
        }
        catch (Exception)
        {
            // Already stopping.
        }

        capture.Dispose();
    }

    public bool Toggle() => Running ? StopAndReport() : Start();

    private bool StopAndReport()
    {
        Stop();
        return false;
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        Running = false;
        if (e.Exception is not null) LastError = e.Exception.Message;
    }

    // --------------------------------------------------------------- measure

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var format = _format;
        if (format is null) return;

        int bytesPerSample = format.BitsPerSample / 8;
        if (bytesPerSample <= 0) return;

        int channels = Math.Max(1, format.Channels);
        int frameSize = bytesPerSample * channels;

        for (int offset = 0; offset + frameSize <= e.BytesRecorded; offset += frameSize)
        {
            double left = ReadSample(e.Buffer, offset, format);
            double right = channels > 1
                ? ReadSample(e.Buffer, offset + bytesPerSample, format)
                : left;

            // Band-limit each channel independently, then accumulate energy.
            left = _lowLeft!.Process(_highLeft!.Process(left));
            right = _lowRight!.Process(_highRight!.Process(right));

            _sumLeft += left * left;
            _sumRight += right * right;
            _framesInBlock++;

            if (_framesInBlock >= _framesPerBlock)
            {
                PublishBlock(_sumLeft / _framesInBlock, _sumRight / _framesInBlock);
                _sumLeft = 0;
                _sumRight = 0;
                _framesInBlock = 0;
            }
        }
    }

    private static double ReadSample(byte[] buffer, int offset, WaveFormat format)
    {
        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
            return BitConverter.ToSingle(buffer, offset);

        if (format.BitsPerSample == 16)
            return BitConverter.ToInt16(buffer, offset) / 32768.0;

        if (format.BitsPerSample == 32)
            return BitConverter.ToInt32(buffer, offset) / 2147483648.0;

        return 0;
    }

    private void PublishBlock(double left, double right)
    {
        double total = left + right;

        _fast += FastAlpha * (total - _fast);
        _baseline += BaselineAlpha * (total - _baseline);

        double targetPan = (right - left) / (total + Epsilon);
        double ratio = _fast / (_baseline + Epsilon);
        double levelDb = 10 * Math.Log10(_fast + Epsilon);

        long now = Environment.TickCount64;

        lock (_gate)
        {
            _pan += PanAlpha * (targetPan - _pan);
            _levelDb = levelDb;
            _blocks++;

            // Below this the stream is digital silence rather than a quiet room,
            // which normally means the wrong output device is the default one.
            if (total > 1e-9) _lastAudibleAt = now;

            if (now - _lastEventAt >= EventCooldownMs)
            {
                double ratioNeeded = 6 - 4 * Sensitivity;
                double dbNeeded = -78 + (1 - Sensitivity) * 22;

                if (ratio >= ratioNeeded && levelDb >= dbNeeded)
                {
                    _lastEventAt = now;
                    _events.Add(new SoundEvent(now, _pan, ratio, ratio > LoudRatio));
                    if (_events.Count > 256) _events.RemoveRange(0, _events.Count - 256);
                }
            }
        }
    }

    public void Dispose() => Stop();
}

internal enum SoundHealth
{
    Stopped,
    Starting,
    Ok,
    Mono,
    NoBlocks,
    Silent,
}
