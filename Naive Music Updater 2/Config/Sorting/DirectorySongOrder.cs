using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class DirectorySongOrder : SongOrder
    {
        private readonly SortType Sort;
        public DirectorySongOrder(YamlMappingNode yaml)
        {
            var sort = yaml.TryGet("sort");
            if ((string)sort == "alphabetical")
                Sort = SortType.Alphabetical;
        }

        public override Metadata Get(IMusicItem item)
        {
            List<Song> Sorted = item.Parent.Songs.OrderBy(GetSort()).ToList();
            var metadata = new Metadata();
            for (int i = 0; i < Sorted.Count; i++)
            {
                if (Sorted[i] == item)
                {
                    metadata.Register(MetadataField.Track, MetadataProperty.Single((i + 1).ToString(), CombineMode.Replace));
                    metadata.Register(MetadataField.TrackTotal, MetadataProperty.Single(Sorted.Count.ToString(), CombineMode.Replace));
                    break;
                }
            }
            return metadata;
        }

        private Func<Song, string> GetSort()
        {
            if (Sort == SortType.Alphabetical)
                return x => x.SimpleName;
            throw new ArgumentException();
        }

        private enum SortType
        {
            Alphabetical
        }
    }
}
