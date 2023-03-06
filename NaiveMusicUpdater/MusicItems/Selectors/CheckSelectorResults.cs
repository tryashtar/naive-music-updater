namespace NaiveMusicUpdater;

public class CheckSelectorResults
{
    public List<IMusicItem> UnselectedItems = new();
    public List<IItemSelector> UnusedSelectors = new();

    public void AddResults(CheckSelectorResults more)
    {
        UnselectedItems.AddRange(more.UnselectedItems);
        UnusedSelectors.AddRange(more.UnusedSelectors);
    }

    public bool AnyUnused => UnselectedItems.Any() || UnusedSelectors.Any();
}