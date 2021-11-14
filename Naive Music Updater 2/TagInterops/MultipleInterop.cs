using System.Collections.Generic;
using System.Linq;
using TagLib;

namespace NaiveMusicUpdater
{
    public class MultipleInterop : ITagInterop
    {
        private readonly List<ITagInterop> Interops;
        public bool Changed => Interops.Any(x => x.Changed);
        public MultipleInterop(CombinedTag tag)
        {
            Interops = tag.Tags.Select(x => TagInteropFactory.GetDynamicInterop(x)).ToList();
        }

        public MetadataProperty Get(MetadataField field)
        {
            foreach (var interop in Interops)
            {
                var result = interop.Get(field);
                if (result.Value.IsBlank)
                    return result;
            }
            return MetadataProperty.Ignore();
        }

        public void Set(MetadataField field, MetadataProperty value)
        {
            foreach (var interop in Interops)
            {
                interop.Set(field, value);
            }
        }

        public void WipeUselessProperties()
        {
            foreach (var interop in Interops)
            {
                interop.WipeUselessProperties();
            }
        }
    }
}
