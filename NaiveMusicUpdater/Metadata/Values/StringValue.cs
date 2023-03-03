namespace NaiveMusicUpdater;

public class StringValue : IValue
{
    public readonly string Value;
    public StringValue(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public ListValue AsList() => new(Value);
    public StringValue AsString() => this;
    public bool IsBlank => false;

    public override string ToString() => Value;
}
