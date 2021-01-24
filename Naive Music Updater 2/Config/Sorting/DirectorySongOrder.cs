using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class DirectorySongOrder : SongOrder
    {
        private readonly SortType Sort;
        private readonly SubfolderMode Subfolders;
        public DirectorySongOrder(YamlMappingNode yaml)
        {
            var sort = yaml.TryGet("sort");
            if ((string)sort == "alphabetical")
                Sort = SortType.Alphabetical;
            var subfolder = yaml.TryGet("subfolders");
            if (subfolder == null)
                Subfolders = SubfolderMode.Ignore;
            else
            {
                if ((string)subfolder == "before")
                    Subfolders = SubfolderMode.IncludeBefore;
                else if ((string)subfolder == "after")
                    Subfolders = SubfolderMode.IncludeAfter;
            }
        }

        public override Metadata Get(IMusicItem item)
        {
            List<Song> Sorted = SongSource(item, Subfolders).OrderBy(GetSort<Song>(), GetComparer<Song>()).ToList();
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

        private IEnumerable<Song> SongSource(IMusicItem item, SubfolderMode mode)
        {
            //if (mode == SubfolderMode.Ignore)
            return item.Parent.Songs;
            var sort = GetSort<IMusicItem>();
            var subfolders = item.Parent.SubFolders.OrderBy(sort);
            var extra_songs = subfolders.SelectMany(x => SongSource(x, mode));
            if (mode == SubfolderMode.IncludeAfter)
                return item.Parent.Songs.Concat(extra_songs);
            else if (mode == SubfolderMode.IncludeBefore)
                return extra_songs.Concat(item.Parent.Songs);
            throw new ArgumentException();
        }

        private Func<T, string> GetSort<T>() where T : IMusicItem
        {
            if (Sort == SortType.Alphabetical)
                return x => x.SimpleName;
            throw new ArgumentException();
        }

        private IComparer<string> GetComparer<T>() where T : IMusicItem
        {
            if (Sort == SortType.Alphabetical)
                return LogicalComparer.Instance;
            throw new ArgumentException();
        }

        private class LogicalComparer : IComparer<string>
        {
            public static LogicalComparer Instance = new LogicalComparer();
            private LogicalComparer() { }
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            static extern int StrCmpLogicalW(String x, String y);

            public int Compare(string x, string y)
            {
                return StrCmpLogicalW(x, y);
            }
        }

        private enum SortType
        {
            Alphabetical
        }

        private enum SubfolderMode
        {
            Ignore,
            IncludeBefore,
            IncludeAfter
        }
    }
}
