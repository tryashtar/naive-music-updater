using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public static class ItemPredicateFactory
    {
        public static IItemPredicate CreateFrom(string str)
        {
            return new ExactItemPredicate(str);
        }

        public static IItemPredicate CreateFrom(Regex regex)
        {
            return new RegexItemPredicate(regex);
        }

        public static IItemPredicate FromNode(YamlNode node)
        {
            if (node.NodeType == YamlNodeType.Scalar)
                return CreateFrom((string)node);
            if (node is YamlMappingNode map)
                return CreateFrom((string)map["regex"]);
            throw new ArgumentException();
        }
    }

    public interface IItemPredicate
    {
        bool Matches(IMusicItem item);
    }

    public class ExactItemPredicate : IItemPredicate
    {
        public readonly string Matcher;
        public ExactItemPredicate(string str)
        {
            Matcher = str;
        }

        public bool Matches(IMusicItem item)
        {
            return String.Equals(item.SimpleName, Matcher, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class RegexItemPredicate : IItemPredicate
    {
        public readonly Regex Matcher;
        public RegexItemPredicate(Regex regex)
        {
            Matcher = regex;
        }

        public bool Matches(IMusicItem item)
        {
            return Matcher.IsMatch(item.SimpleName);
        }
    }

    public class SongPredicate
    {
        private readonly IItemPredicate[] Path;
        public SongPredicate(params IItemPredicate[] items)
        {
            Path = items;
        }

        public SongPredicate(string slash_delimited)
        {
            Path = slash_delimited.Split('/').Select(x => ItemPredicateFactory.CreateFrom(x)).ToArray();
        }

        public static SongPredicate FromNode(YamlNode node)
        {
            if (node.NodeType == YamlNodeType.Scalar)
                return new SongPredicate((string)node);
            if (node.NodeType == YamlNodeType.Sequence)
                return new SongPredicate(((YamlSequenceNode)node).Children.Select(x => ItemPredicateFactory.FromNode(x)).ToArray());
            throw new ArgumentException();
        }

        public bool Matches(IMusicItem start, IMusicItem song)
        {
            var startpath = start.PathFromRoot().Skip(1).ToArray();
            var songpath = song.PathFromRoot().Skip(1).ToArray();
            int index;
            // get the relative path between the folder and the song
            // fail if they don't share a history
            for (index = 0; index < startpath.Length; index++)
            {
                if (startpath[index] != songpath[index])
                    return false;
            }
            // fail if the song isn't deep enough
            if (Path.Length > songpath.Length - index)
                return false;
            for (int i = 0; i < Path.Length; i++)
            {
                if (!Path[i].Matches(songpath[index + i]))
                    return false;
            }
            return true;
        }
    }
}
