using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface ISongOrder
    {
        Metadata Get(IMusicItem item);
    }

    public static class SongOrderFactory
    {
        public static ISongOrder Create(YamlNode yaml, MusicFolder folder)
        {
            var selector = ItemSelectorFactory.Create(yaml);
            return new DefinedSongOrder(selector, folder);
        }
    }
}
