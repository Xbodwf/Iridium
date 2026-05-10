namespace Iridium.Polyfill;

class Double
{
    public static bool IsFinite(double d) => !double.IsNaN(d) && !double.IsInfinity(d);
}