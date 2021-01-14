using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    // cannot be used to get itself, use "<this>" instead
    public class SimpleParentSelector : MetadataSelector
    {
        private readonly int Number;
        public SimpleParentSelector(int number)
        {
            Number = number;
        }

        public override string GetRaw(IMusicItem item)
        {
            IMusicItem found;
            var list = item.PathFromRoot().ToList();
            if (Number >= 0)
            {
                if (Number >= list.Count)
                    return null;
                found = list[Number];
            }
            else
            {
                int index = list.Count + Number - 1;
                if (index < 0)
                    return null;
                found = list[index];
            }
            if (found == item)
                return null;
            return ResolveNameOrDefault(found, item);
        }
    }
}
