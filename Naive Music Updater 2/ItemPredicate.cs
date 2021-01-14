﻿using Newtonsoft.Json.Linq;
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

        public List<IMusicItem> SelectFrom(MusicFolder start)
        {
            var start_path = start.PathFromRoot().ToArray();
            var results = new List<IMusicItem>();
            var all = start.SubItems;
            foreach (var item in all)
            {
                var item_path = item.PathFromRoot().Skip(start_path.Length).ToArray();
                if (IsMatch(item_path))
                    results.Add(item);
            }
            return results;
        }

        public bool IsSelectedFrom(MusicFolder start, IMusicItem item)
        {
            return SelectFrom(start).Contains(item);
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
    }
}
