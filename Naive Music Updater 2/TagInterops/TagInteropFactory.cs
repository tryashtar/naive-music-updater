﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using TagLib;

namespace NaiveMusicUpdater
{
    public interface ITagInterop
    {
        MetadataProperty Get(MetadataField field);
        void Set(MetadataField field, MetadataProperty value);
        void WipeUselessProperties();
        bool Changed { get; }
    }

    public static class TagInteropFactory
    {
        public static ITagInterop GetDynamicInterop(dynamic tag, LibraryConfig config)
        {
            return GetInterop(tag, config);
        }

        private static ITagInterop GetInterop(TagLib.Id3v2.Tag tag, LibraryConfig config) => new Id3v2TagInterop(tag, config);
        private static ITagInterop GetInterop(TagLib.Id3v1.Tag tag, LibraryConfig config) => new Id3v1TagInterop(tag, config);
        private static ITagInterop GetInterop(TagLib.Ape.Tag tag, LibraryConfig config) => new ApeTagInterop(tag, config);
        private static ITagInterop GetInterop(TagLib.Ogg.XiphComment tag, LibraryConfig config) => new XiphTagInterop(tag, config);
        private static ITagInterop GetInterop(TagLib.Mpeg4.AppleTag tag, LibraryConfig config) => new AppleTagInterop(tag, config);
        private static ITagInterop GetInterop(CombinedTag tag, LibraryConfig config) => new MultipleInterop(tag, config);
    }
}
