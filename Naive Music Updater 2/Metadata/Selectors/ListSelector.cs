using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class ListSelector : MetadataSelector
    {
        private readonly List<MetadataSelector> SubSelectors;
        public ListSelector(YamlSequenceNode list)
        {
            SubSelectors = list.Select(x => MetadataSelectorFactory.Create(x)).ToList();
        }

        public override MetadataProperty GetRaw(IMusicItem item)
        {
            var results = SubSelectors.SelectMany(x => x.GetRaw(item).ListValue).ToList();
            return MetadataProperty.List(results, CombineMode.Replace);
        }
    }
}
