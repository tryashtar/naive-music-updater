using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IValueResolver
    {
        IValue Resolve(IMusicItem item);
    }

    public static class ValueResolverFactory
    {
        public static IValueResolver Create(YamlNode yaml)
        {
            if (yaml is YamlScalarNode scalar)
                return new LiteralStringResolver(scalar.Value);
            else if (yaml is YamlSequenceNode sequence)
                return new LiteralListResolver(sequence.ToList());
            else if (yaml is YamlMappingNode map)
                return new SourcedResolver(map);
            throw new ArgumentException($"Can't create a value resolver from {yaml}");
        }
    }
}
