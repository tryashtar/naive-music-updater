using System;

namespace NaiveMusicUpdater
{
    public class ExactItemPredicate : IItemPredicate
    {
        public readonly string Matcher;
        public ExactItemPredicate(string str)
        {
            Matcher = str;
        }

        public bool Matches(IMusicItem item)
        {
            return String.Equals(item.SimpleName, Matcher, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return Matcher;
        }
    }
}
