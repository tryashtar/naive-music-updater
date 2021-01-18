using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public abstract class SongOrder
    {
        public abstract Metadata Get(IMusicItem item);
    }
}
