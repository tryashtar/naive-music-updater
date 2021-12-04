namespace NaiveMusicUpdater;

public interface IValue
{
    StringValue AsString();
    ListValue AsList();
    bool IsBlank { get; }
}
