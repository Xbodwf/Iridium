using System;

namespace Iridium.Polyfill;

class MathI
{
    public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0) return min;
        else if (val.CompareTo(max) > 0) return max;
        return val;
    }
}