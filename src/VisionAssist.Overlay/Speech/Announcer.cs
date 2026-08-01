using System.Globalization;
using System.Speech.Synthesis;
using VisionAssist.Companion;

namespace VisionAssist.Overlay;

/// <summary>
/// Speaks the player's own state out loud.
///
/// This is the half of the toolkit aimed at someone who cannot read a number on
/// screen quickly, however large it is drawn: hearing "twenty three" is faster
/// than finding the health panel and focusing on it. Everything spoken is the
/// player's own state - the same values already on their HUD.
///
/// Announcements are rate limited and prioritised, because a voice that talks
/// over itself is worse than no voice. Anything spoken also suppresses the sound
/// meter briefly, since the speech goes out of the same speakers the meter is
/// listening to.
/// </summary>
internal sealed class Announcer : IDisposable
{
    /// <summary>Higher wins: an urgent line cancels a routine one, never the reverse.</summary>
    private enum Priority
    {
        Routine = 0,
        Important = 1,
        Urgent = 2,
    }

    private static readonly TimeSpan RoutineGap = TimeSpan.FromMilliseconds(1200);

    private readonly OverlaySettings _settings;
    private readonly SoundMeter _sound;

    private SpeechSynthesizer? _voice;
    private string? _voiceLanguage;

    private long _lastSpokeAt;
    private Priority _lastPriority = Priority.Routine;

    // Previous state, so only changes are announced.
    private int _lastHealth = -1;
    private int _lastAmmo = -1;
    private bool _lastLowHealthSaid;
    private string? _lastBombState;
    private string? _lastRoundPhase;
    private int _lastBombCall = -1;

    public Announcer(OverlaySettings settings, SoundMeter sound)
    {
        _settings = settings;
        _sound = sound;
    }

    public string? LastError { get; private set; }

    public bool Available => _voice is not null;

    // ------------------------------------------------------------------ voice

    /// <summary>
    /// Builds the synthesiser, picking a voice for the chosen language when one
    /// is installed. Uzbek almost certainly is not, so the fallback matters more
    /// than the preference.
    /// </summary>
    private bool EnsureVoice()
    {
        if (_voice is not null && _voiceLanguage == _settings.Language) return true;

        DisposeVoice();

        try
        {
            var voice = new SpeechSynthesizer();
            voice.SetOutputToDefaultAudioDevice();

            // -10..10 in SAPI; the setting is 0..1 over a usable part of that.
            voice.Rate = (int)Math.Round(-2 + _settings.SpeechRate * 8);
            voice.Volume = Math.Clamp((int)Math.Round(_settings.SpeechVolume * 100), 0, 100);

            TrySelectVoice(voice, _settings.Language);

            _voice = voice;
            _voiceLanguage = _settings.Language;
            LastError = null;
            return true;
        }
        catch (Exception ex)
        {
            // No voices installed, or SAPI is unavailable. Not fatal - the HUD
            // does not depend on this.
            LastError = ex.Message;
            _voice = null;
            return false;
        }
    }

    private static void TrySelectVoice(SpeechSynthesizer voice, string language)
    {
        // Uzbek has no shipped Windows voice, so it is deliberately mapped to
        // Russian: hearing the numbers in a language the player understands beats
        // an English voice reading Uzbek words it cannot pronounce.
        string culture = language switch
        {
            "ru" => "ru-RU",
            "en" => "en-US",
            _ => "ru-RU",
        };

        try
        {
            voice.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0,
                new CultureInfo(culture));
        }
        catch (Exception)
        {
            // Whatever the default voice is will have to do.
        }
    }

    private void DisposeVoice()
    {
        if (_voice is null) return;

        try
        {
            _voice.SpeakAsyncCancelAll();
        }
        catch (Exception)
        {
            // Already torn down.
        }

        _voice.Dispose();
        _voice = null;
    }

    /// <summary>Re-reads rate, volume and language from the settings.</summary>
    public void SettingsChanged()
    {
        if (!_settings.SpeechEnabled)
        {
            DisposeVoice();
            return;
        }

        DisposeVoice();
        EnsureVoice();
    }

    // ----------------------------------------------------------------- speaking

    private void Say(string text, Priority priority, TimeSpan suppressMeter)
    {
        if (!_settings.SpeechEnabled || text.Length == 0) return;
        if (!EnsureVoice() || _voice is null) return;

        long now = Environment.TickCount64;
        bool busy = now - _lastSpokeAt < RoutineGap.TotalMilliseconds;

        // A routine line waits its turn; an urgent one interrupts.
        if (busy && priority <= _lastPriority && priority != Priority.Urgent) return;

        try
        {
            if (priority == Priority.Urgent) _voice.SpeakAsyncCancelAll();

            // Muted before speaking, not after, so the first syllable is already
            // covered by the time it reaches the speakers.
            _sound.SuppressFor(suppressMeter);
            _voice.SpeakAsync(text);

            _lastSpokeAt = now;
            _lastPriority = priority;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    // ------------------------------------------------------------------ update

    /// <summary>
    /// Called from the overlay's timer. Compares against the previous snapshot and
    /// speaks only what changed.
    /// </summary>
    public void Update(OverlaySnapshot s, double? countdownSeconds)
    {
        if (!_settings.SpeechEnabled) return;

        if (!s.Connected || !s.InGame)
        {
            ResetRoundState();
            return;
        }

        AnnounceRound(s);
        AnnounceHealth(s);
        AnnounceAmmo(s);
        AnnounceBomb(s, countdownSeconds);
    }

    private void AnnounceRound(OverlaySnapshot s)
    {
        if (s.RoundPhase == _lastRoundPhase) return;

        string? previous = _lastRoundPhase;
        _lastRoundPhase = s.RoundPhase;

        // Nothing is said for the very first snapshot, or the program would
        // announce a round that started before it was running.
        if (previous is null) return;

        if (s.RoundPhase == "live" && _settings.AnnounceRound)
        {
            ResetRoundState();
            Say(Phrase("roundStart"), Priority.Important, TimeSpan.FromMilliseconds(1400));
        }
    }

    private void AnnounceHealth(OverlaySnapshot s)
    {
        if (!_settings.AnnounceHealth)
        {
            _lastHealth = s.Health;
            return;
        }

        if (_lastHealth < 0)
        {
            _lastHealth = s.Health;
            return;
        }

        int lost = _lastHealth - s.Health;
        _lastHealth = s.Health;

        if (s.Health <= 0)
        {
            _lastLowHealthSaid = false;
            return;
        }

        // Crossing the threshold is the thing worth interrupting for; a plain hit
        // just gets the new number.
        if (s.Health <= _settings.LowHealthThreshold && !_lastLowHealthSaid)
        {
            _lastLowHealthSaid = true;
            Say($"{Phrase("lowHealth")} {s.Health}", Priority.Urgent, TimeSpan.FromMilliseconds(1800));
            return;
        }

        if (s.Health > _settings.LowHealthThreshold) _lastLowHealthSaid = false;

        // Below five points is noise - chip damage from smoke or fall damage.
        if (lost >= 5) Say(s.Health.ToString(), Priority.Routine, TimeSpan.FromMilliseconds(900));
    }

    private void AnnounceAmmo(OverlaySnapshot s)
    {
        if (!_settings.AnnounceAmmo || s.AmmoClip is null)
        {
            _lastAmmo = s.AmmoClip ?? -1;
            return;
        }

        int ammo = s.AmmoClip.Value;
        int previous = _lastAmmo;
        _lastAmmo = ammo;

        if (previous < 0 || ammo >= previous) return;

        if (ammo == 0)
        {
            Say(Phrase("noAmmo"), Priority.Urgent, TimeSpan.FromMilliseconds(1200));
            return;
        }

        // One warning per magazine, when a quarter or less is left.
        int max = s.AmmoClipMax ?? 30;
        int warnAt = Math.Max(3, max / 4);
        if (ammo <= warnAt && previous > warnAt)
            Say($"{Phrase("lowAmmo")} {ammo}", Priority.Important, TimeSpan.FromMilliseconds(1400));
    }

    private void AnnounceBomb(OverlaySnapshot s, double? countdownSeconds)
    {
        if (!_settings.AnnounceBomb)
        {
            _lastBombState = s.BombState;
            return;
        }

        if (s.BombState != _lastBombState)
        {
            _lastBombState = s.BombState;
            _lastBombCall = -1;

            string key = s.BombState switch
            {
                "planted" => "bombPlanted",
                "defused" => "bombDefused",
                "exploded" => "bombExploded",
                _ => string.Empty,
            };

            if (key.Length > 0)
                Say(Phrase(key), Priority.Urgent, TimeSpan.FromMilliseconds(1600));
        }

        // Counted down out loud at 20, 10 and 5 seconds - the points where the
        // decision to keep defusing or back off actually changes.
        if (s.BombState != "planted" || s.CountdownPhase != "bomb" || countdownSeconds is null) return;

        int remaining = (int)Math.Ceiling(countdownSeconds.Value);
        foreach (int mark in new[] { 20, 10, 5 })
        {
            if (remaining == mark && _lastBombCall != mark)
            {
                _lastBombCall = mark;
                Say(mark.ToString(), Priority.Important, TimeSpan.FromMilliseconds(900));
                return;
            }
        }
    }

    private void ResetRoundState()
    {
        _lastHealth = -1;
        _lastAmmo = -1;
        _lastLowHealthSaid = false;
        _lastBombCall = -1;
    }

    // ----------------------------------------------------------------- phrases

    /// <summary>
    /// Kept short on purpose. A long sentence is still being read out when the
    /// situation it described has already changed.
    /// </summary>
    private string Phrase(string key)
    {
        var table = _settings.Language switch
        {
            "en" => English,
            _ => Russian,
        };

        return table.TryGetValue(key, out string? value) ? value : string.Empty;
    }

    private static readonly Dictionary<string, string> Russian = new()
    {
        ["lowHealth"] = "мало здоровья",
        ["noAmmo"] = "нет патронов",
        ["lowAmmo"] = "патроны",
        ["bombPlanted"] = "бомба установлена",
        ["bombDefused"] = "разминирована",
        ["bombExploded"] = "взрыв",
        ["roundStart"] = "раунд",
    };

    private static readonly Dictionary<string, string> English = new()
    {
        ["lowHealth"] = "low health",
        ["noAmmo"] = "out of ammo",
        ["lowAmmo"] = "ammo",
        ["bombPlanted"] = "bomb planted",
        ["bombDefused"] = "defused",
        ["bombExploded"] = "bomb exploded",
        ["roundStart"] = "round",
    };

    public void Dispose() => DisposeVoice();
}
