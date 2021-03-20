using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class ItemSelector
    {
        private readonly IItemPredicate[] Path;
        public ItemSelector(params IItemPredicate[] items)
        {
            Path = items;
        }

        public ItemSelector(string slash_delimited)
        {
            Path = slash_delimited.Split('/').Select(x => ItemPredicateFactory.CreateFrom(x)).ToArray();
        }

        public static ItemSelector FromNode(YamlNode node)
        {
            if (node.NodeType == YamlNodeType.Scalar)
                return new ItemSelector((string)node);
            if (node.NodeType == YamlNodeType.Sequence)
                return new ItemSelector(((YamlSequenceNode)node).Children.Select(x => ItemPredicateFactory.FromNode(x)).ToArray());
            throw new ArgumentException();
        }

        public IEnumerable<IMusicItem> AllMatchesFrom(IMusicItem start)
        {
            IEnumerable<IMusicItem> from = new IMusicItem[] { start };
            foreach (var item in Path)
            {
                from = from.OfType<MusicFolder>().SelectMany(x => x.SubItems.Where(y => item.Matches(y)));
            }
            return from;
        }

        public bool IsSelectedFrom(IMusicItem start, IMusicItem item)
        {
            var start_path = start.PathFromRoot().ToArray();
            var item_path = item.PathFromRoot().Skip(start_path.Length).ToArray();
            return IsMatch(item_path);
        }

        private bool IsMatch(IMusicItem[] item_path)
        {
            // fail if the song isn't deep enough
            if (Path.Length > item_path.Length)
                return false;
            for (int i = 0; i < Path.Length; i++)
            {
                if (!Path[i].Matches(item_path[i]))
                    return false;
            }
            return true;
        }

        public override string ToString()
        {
            return String.Join("/", Path.Select(x => x.ToString()));
        }
    }
}
