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
        private readonly List<IMusicItem> Unselected;
        private readonly Dictionary<IMusicItem, uint> CachedResults;
        public ReadOnlyCollection<ItemSelector> Order => DefinedOrder.AsReadOnly();
        public ReadOnlyCollection<IMusicItem> UnselectedItems => Unselected.AsReadOnly();
        public DefinedSongOrder(YamlSequenceNode yaml, MusicFolder folder)
        {
            Folder = folder;
            DefinedOrder = yaml.Children.Select(x => ItemSelector.FromNode(x)).ToList();
            CachedResults = new Dictionary<IMusicItem, uint>();
            var used_folders = new HashSet<MusicFolder>();
            uint index = 1;
            foreach (var item in DefinedOrder)
            {
                var matches = item.AllMatchesFrom(folder);
                foreach (var match in matches)
                {
                    CachedResults[match] = index;
                    used_folders.Add(match.Parent);
                }
                index++;
            }
            Unselected = new List<IMusicItem>();
            foreach (var used in used_folders)
            {
                Unselected.AddRange(used.Songs.Except(CachedResults.Keys));
            }
        }

        public override Metadata Get(IMusicItem item)
        {
            var metadata = new Metadata();
            if (CachedResults.TryGetValue(item, out uint track))
            {
                metadata.Register(MetadataField.Track, MetadataProperty.Single(track.ToString(), CombineMode.Replace));
                metadata.Register(MetadataField.TrackTotal, MetadataProperty.Single(DefinedOrder.Count.ToString(), CombineMode.Replace));
            }
            return metadata;
        }
    }
}
