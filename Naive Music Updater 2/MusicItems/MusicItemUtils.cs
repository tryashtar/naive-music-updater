using System;
using System.Collections.Generic;

namespace NaiveMusicUpdater
{
    public static class MusicItemUtils
    {
        public static Metadata GetMetadata(this IMusicItem item, Predicate<MetadataField> desired)
        {
            var metadata = new Metadata();
            foreach (var parent in item.PathFromRoot())
            {
                if (parent.LocalConfig != null)
                    metadata.Merge(parent.LocalConfig.GetMetadata(item, desired));
            }
            return metadata;
        }

        public static IEnumerable<IMusicItem> PathFromRoot(this IMusicItem item)
        {
            var list = new List<IMusicItem>();
            while (item != null)
            {
                list.Add(item);
                item = item.Parent;
            }
            list.Reverse();
            return list;
        }
    }
}
