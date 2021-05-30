using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public static class SongOrderFactory
    {
        public static SongOrder FromNode(YamlNode node, MusicFolder folder)
        {
            return new DefinedSongOrder(node, folder);
        }
    }
}
