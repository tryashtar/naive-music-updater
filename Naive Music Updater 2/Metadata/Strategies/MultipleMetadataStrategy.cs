using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class MultipleMetadataStrategy : IMetadataStrategy
    {
        private readonly List<IMetadataStrategy> Substrategies;

        public MultipleMetadataStrategy(YamlSequenceNode yaml)
        {
            Substrategies = new List<IMetadataStrategy>();
            foreach (var item in yaml.Children)
            {
                Substrategies.Add(MetadataStrategyFactory.Create(item));
            }
        }

        public MultipleMetadataStrategy(IEnumerable<IMetadataStrategy> strategies)
        {
            Substrategies = strategies.ToList();
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            var datas = Substrategies.Select(x => x.Get(item, desired));
            return Metadata.FromMany(datas);
        }
    }
}
