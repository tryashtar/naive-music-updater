using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IMusicItemValueSource
    {
        IValue Get(IMusicItem item);
    }

    public static class MusicItemGetterFactory
    {
        public static IMusicItemValueSource Create(YamlNode yaml)
        {
            if (yaml is YamlScalarNode scalar)
            {
                var name = scalar.ToEnum<NameType>();
                if (name == NameType.CleanName)
                    return CleanNameGetter.Instance;
                else if (name == NameType.FileName)
                    return SimpleNameGetter.Instance;
            }
            else if (yaml is YamlMappingNode map)
            {
                var copy = yaml.Go("copy").NullableParse(x => MetadataField.FromID(x.String()));
                if (copy != null)
                    return new CopyMetadataGetter(copy);
            }

            throw new ArgumentException($"Can't make metadata selector from {yaml}");
        }
    }

    public enum NameType
    {
        CleanName,
        FileName
    }
}
