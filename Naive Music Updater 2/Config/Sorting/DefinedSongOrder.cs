using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class DefinedSongOrder : SongOrder
    {
        private readonly MusicFolder Folder;
        private readonly List<ItemSelector> DefinedOrder;
        public ReadOnlyCollection<ItemSelector> Order => DefinedOrder.AsReadOnly();
        public DefinedSongOrder(YamlSequenceNode yaml, MusicFolder folder)
        {
            Folder = folder;
            DefinedOrder = yaml.Children.Select(x => ItemSelector.FromNode(x)).ToList();
        }

        public override Metadata Get(IMusicItem item)
        {
            var metadata = new Metadata();
            for (int i = 0; i < DefinedOrder.Count; i++)
            {
                if (DefinedOrder[i].IsSelectedFrom(Folder, item))
                {
                    metadata.Register(MetadataField.Track, MetadataProperty.Single((i + 1).ToString(), CombineMode.Replace));
                    metadata.Register(MetadataField.TrackTotal, MetadataProperty.Single(DefinedOrder.Count.ToString(), CombineMode.Replace));
                    break;
                }
            }
            return metadata;
        }
    }
}
