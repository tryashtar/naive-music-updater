namespace NaiveMusicUpdater;

public class BlankValue : IValue
{
    public static readonly BlankValue Instance = new();

    public ListValue AsList() => throw new InvalidOperationException();
    public StringValue AsString() => throw new InvalidOperationException();
    public bool IsBlank => true;

    public override string ToString() => "(blank)";
}