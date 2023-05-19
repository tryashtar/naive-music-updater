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
        var final = original;
        foreach (var op in Operators)
        {
            final = op.Apply(item, final);
            if (final == null)
                return null;
        }

        return final;
    }
}