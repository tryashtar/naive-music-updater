using System.Numerics;

namespace NaiveMusicUpdater;

public interface IValue
{
    StringValue AsString();
    ListValue AsList();

    NumberValue AsNumber()
    {
        return new NumberValue(uint.Parse(AsString().Value));
    }

    bool IsBlank { get; }
}

public enum CombineMode
{
    Replace,
    Append,
    Prepend
}

public class ValueEqualityChecker : IEqualityComparer<IValue>
{
    public static readonly ValueEqualityChecker Instance = new();

    public bool Equals(IValue? x, IValue? y)
    {
        if (x == null)
            return y == null;
        if (y == null)
            return false;
        if (x.IsBlank && y.IsBlank)
            return true;
        if (x.IsBlank || y.IsBlank)
            return false;
        return x.AsList().Values.SequenceEqual(y.AsList().Values);
    }

    public int GetHashCode(IValue obj)
    {
        if (obj.IsBlank)
            return 0;
        return String.Join(';', obj.AsList().Values).GetHashCode();
    }
}

public static class ValueExtensions
{
    public static IValue Combine(IValue v1, IValue v2, CombineMode mode)
    {
        return mode switch
        {
            CombineMode.Replace => v2,
            CombineMode.Append => new ListValue(v1.AsList().Values.Concat(v2.AsList().Values)),
            CombineMode.Prepend => new ListValue(v2.AsList().Values.Concat(v1.AsList().Values)),
            _ => throw new ArgumentException($"Invalid combine mode {mode}")
        };
    }
}