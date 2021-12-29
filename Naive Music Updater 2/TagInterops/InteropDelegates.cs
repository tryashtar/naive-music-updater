namespace NaiveMusicUpdater;

public delegate MetadataProperty Getter();
public delegate void Setter(MetadataProperty value);
public delegate bool Equal(MetadataProperty p1, MetadataProperty p2);
public class InteropDelegates
{
    public readonly Getter Getter;
    public readonly Setter Setter;
    public readonly Equal Equal;
    public InteropDelegates(Getter getter, Setter setter, Equal equal)
    {
        Getter = getter;
        Setter = setter;
        Equal = equal;
    }
}

public delegate WipeResult Wiper();
public class WipeDelegates
{
    public readonly Wiper Wipe;
    public WipeDelegates(Wiper wipe)
    {
        Wipe = wipe;
    }
}

public record WipeResult(string OldValue, string NewValue, bool Changed);

