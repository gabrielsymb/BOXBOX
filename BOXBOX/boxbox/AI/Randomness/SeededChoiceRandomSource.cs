using Boxbox.AI.Contracts;

namespace Boxbox.AI.Randomness;

public sealed class SeededChoiceRandomSource : IChoiceRandomSource
{
    private ulong _state;

    public SeededChoiceRandomSource(ulong seed)
    {
        _state = seed;
    }

    public double NextUnitDouble()
    {
        unchecked
        {
            _state += 0x9E3779B97F4A7C15UL;
            var value = _state;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            value ^= value >> 31;
            return (value >> 11) * (1.0 / (1UL << 53));
        }
    }
}
