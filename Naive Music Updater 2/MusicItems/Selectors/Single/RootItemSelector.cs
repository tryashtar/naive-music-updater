using System;
using System.Collections.Generic;
using System.Linq;

namespace NaiveMusicUpdater
{
    public class RootItemSelector : ISingleItemSelector
    {
        public readonly int Down;
        public readonly MusicItemType? MustBe;
        public RootItemSelector(int down, MusicItemType? must_be = null)
        {
            Down = down;
            MustBe = must_be;
        }

        public IMusicItem SelectFrom(IMusicItem value)
        {
            var path = value.PathFromRoot().ToList();
            if (Down >= path.Count)
                return null;
            return CheckMustBe(path[Down]);
        }

        private IMusicItem CheckMustBe(IMusicItem item)
        {
            if (MustBe == null)
                return item;
            if (MustBe == MusicItemType.File && item is Song)
                return item;
            if (MustBe == MusicItemType.Folder && item is MusicFolder)
                return item;
            return null;
        }
    }
}
