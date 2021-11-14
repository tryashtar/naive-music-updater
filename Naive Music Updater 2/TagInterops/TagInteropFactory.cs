using System.IO;
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
        public static ITagInterop GetDynamicInterop(dynamic tag)
        {
            return GetInterop(tag);
        }

        private static ITagInterop GetInterop(TagLib.Id3v2.Tag tag) => new Id3v2TagInterop(tag);
        private static ITagInterop GetInterop(TagLib.Id3v1.Tag tag) => new Id3v1TagInterop(tag);
        private static ITagInterop GetInterop(TagLib.Ape.Tag tag) => new ApeTagInterop(tag);
        private static ITagInterop GetInterop(TagLib.Ogg.XiphComment tag) => new XiphTagInterop(tag);
        private static ITagInterop GetInterop(TagLib.Mpeg4.AppleTag tag) => new AppleTagInterop(tag);
        private static ITagInterop GetInterop(CombinedTag tag) => new MultipleInterop(tag);
    }
}
