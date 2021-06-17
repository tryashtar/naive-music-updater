using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class DefinedSongOrder : SongOrder
    {
        private readonly MusicFolder Folder;
        private readonly List<IMusicItem> Unselected;
        private readonly Dictionary<IMusicItem, uint> CachedResults;
        public readonly IItemSelector Order;
        public readonly uint TotalNumber;
        public ReadOnlyCollection<IMusicItem> UnselectedItems => Unselected.AsReadOnly();
        public DefinedSongOrder(YamlNode yaml, MusicFolder folder)
        {
            Folder = folder;
            Order = ItemSelectorFactory.Create(yaml);
            CachedResults = new Dictionary<IMusicItem, uint>();
            var used_folders = new HashSet<MusicFolder>();
            var order = Order.AllMatchesFrom(folder).ToList();
            uint index = 0;
            foreach (var item in order)
            {
                index++;
                CachedResults[item] = index;
                used_folders.Add(item.Parent);
            }
            TotalNumber = index;
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
                metadata.Register(MetadataField.TrackTotal, MetadataProperty.Single(TotalNumber.ToString(), CombineMode.Replace));
            }
            return metadata;
        }
    }
}
