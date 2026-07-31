namespace VisionAssist.Overlay;

/// <summary>
/// One second-order filter section, transposed direct form II. Coefficients from
/// the Audio EQ Cookbook.
///
/// Each instance carries its own state, so the left and right channels need
/// separate ones - sharing a filter between channels would smear the very
/// left/right difference the meter exists to measure.
/// </summary>
internal sealed class Biquad
{
    private readonly double _b0, _b1, _b2, _a1, _a2;
    private double _z1, _z2;

    private Biquad(double b0, double b1, double b2, double a0, double a1, double a2)
    {
        _b0 = b0 / a0;
        _b1 = b1 / a0;
        _b2 = b2 / a0;
        _a1 = a1 / a0;
        _a2 = a2 / a0;
    }

    public static Biquad HighPass(double sampleRate, double frequency, double q = 0.707)
    {
        double w0 = 2 * Math.PI * frequency / sampleRate;
        double cos = Math.Cos(w0);
        double alpha = Math.Sin(w0) / (2 * q);

        return new Biquad(
            b0: (1 + cos) / 2,
            b1: -(1 + cos),
            b2: (1 + cos) / 2,
            a0: 1 + alpha,
            a1: -2 * cos,
            a2: 1 - alpha);
    }

    public static Biquad LowPass(double sampleRate, double frequency, double q = 0.707)
    {
        double w0 = 2 * Math.PI * frequency / sampleRate;
        double cos = Math.Cos(w0);
        double alpha = Math.Sin(w0) / (2 * q);

        return new Biquad(
            b0: (1 - cos) / 2,
            b1: 1 - cos,
            b2: (1 - cos) / 2,
            a0: 1 + alpha,
            a1: -2 * cos,
            a2: 1 - alpha);
    }

    public double Process(double x)
    {
        double y = _b0 * x + _z1;
        _z1 = _b1 * x - _a1 * y + _z2;
        _z2 = _b2 * x - _a2 * y;
        return y;
    }

    public void Reset()
    {
        _z1 = 0;
        _z2 = 0;
    }
}
