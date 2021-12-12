namespace NaiveMusicUpdater;

public class ModeValueSourceFieldSetter : IFieldSetter
{
    public readonly CombineMode Mode;
    public readonly IValueSource Source;
    public readonly IValueOperator Modifier; // can be null

    public ModeValueSourceFieldSetter(CombineMode mode, IValueSource source, IValueOperator modify)
    {
        Mode = mode;
        Source = source;
        Modifier = modify;
    }

    public MetadataProperty Get(IMusicItem item)
    {
        var value = Source.Get(item);
        if (Modifier != null)
            value = Modifier.Apply(item, value);
        if (value.IsBlank)
            return MetadataProperty.Ignore();
        return new MetadataProperty(value, Mode);
    }

    public MetadataProperty GetWithContext(IMusicItem item, IValue value)
    {
        // discard context
        return Get(item);
    }
}
