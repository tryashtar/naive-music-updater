namespace NaiveMusicUpdater;

public class MultipleOperator : IValueOperator
{
    private readonly List<IValueOperator> Operators;

    public MultipleOperator(IEnumerable<IValueOperator> operators)
    {
        Operators = operators.ToList();
    }

    public IValue? Apply(IMusicItem item, IValue original)
    {
        foreach (var op in Operators)
        {
            original = op.Apply(item, original);
            if (original == null)
                return null;
        }

        return original;
    }
}