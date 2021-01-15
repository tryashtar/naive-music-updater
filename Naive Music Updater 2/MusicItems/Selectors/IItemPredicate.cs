using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public interface IItemPredicate
    {
        bool Matches(IMusicItem item);
    }
}
