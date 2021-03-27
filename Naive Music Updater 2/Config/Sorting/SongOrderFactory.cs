using System;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public static class SongOrderFactory
    {
        public static SongOrder FromNode(YamlNode yaml, MusicFolder folder)
        {
            if (yaml is YamlSequenceNode sequence)
                return new DefinedSongOrder(sequence, folder);
            if (yaml is YamlMappingNode map)
                return new DirectorySongOrder(map);
            throw new ArgumentException($"{yaml} type is {yaml.NodeType}, doesn't work for song order");
        }
    }
}
