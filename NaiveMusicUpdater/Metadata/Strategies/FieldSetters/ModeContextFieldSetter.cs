namespace NaiveMusicUpdater;

public class ModeContextFieldSetter : IFieldSetter
{
    public readonly CombineMode Mode;
    public readonly IValueOperator Modify;

    public ModeContextFieldSetter(CombineMode mode, IValueOperator modify)
    {
        Mode = mode;
        Modify = modify;
    }

    public MetadataProperty Get(IMusicItem item)
    {
        throw new InvalidOperationException($"Performing an operation on a value requires context!");
    }

    public MetadataProperty GetWithContext(IMusicItem item, IValue value)
    {
        value = Modify.Apply(item, value);
        if (value.IsBlank)
            return MetadataProperty.Ignore();
        return new MetadataProperty(value, Mode);
    }
}
