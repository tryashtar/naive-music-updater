namespace NaiveMusicUpdater;

public class LiteralMetadataSource : IValueSource
{
    public readonly MetadataProperty Literal;
    public LiteralMetadataSource(MetadataProperty literal)
    {
        Literal = literal;
    }

    public IValue Get(IMusicItem item)
    {
        return Literal.Value;
    }
}
