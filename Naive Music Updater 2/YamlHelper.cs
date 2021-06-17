using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public static class YamlHelper
    {
        public static YamlNode ParseFile(string file_path)
        {
            using (var reader = new StreamReader(File.OpenRead(file_path)))
            {
                var stream = new YamlStream();
                stream.Load(reader);
                var root = stream.Documents.SingleOrDefault()?.RootNode;
                return root;
            }
        }

        public static void SaveToFile(YamlNode node, string file_path)
        {
            var doc = new YamlDocument(node);
            var stream = new YamlStream(doc);
            using (var writer = File.CreateText(file_path))
            {
                stream.Save(writer, false);
            }
        }

        public static T ParseOrDefault<T>(this YamlNode node, string key, Func<YamlNode, T> parse, T def = default)
        {
            var sub = node.TryGet(key);
            if (sub == null)
                return def;
            return parse(sub);
        }

        public static YamlNode Go(this YamlNode node, params string[] path)
        {
            foreach (var item in path)
            {
                node = TryGet(node, item);
                if (node == null)
                    return null;
            }
            return node;
        }

        public static YamlNode TryGet(this YamlNode node, string key)
        {
            try
            {
                return node[key];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public static List<TValue> ToList<TValue>(this YamlNode node, Func<YamlNode, TValue> value)
        {
            if (node == null || (node is YamlScalarNode scalar && String.IsNullOrEmpty(scalar.Value)))
                return null;
            if (node is YamlSequenceNode sequence)
            {
                return sequence.Select(value).ToList();
            }
            throw new ArgumentException($"Can't convert {node} ({node.NodeType}) to list");
        }

        public static List<string> ToStringList(this YamlNode node)
        {
            return ToList(node, x => (string)x);
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this YamlNode node, Func<YamlNode, TKey> key, Func<YamlNode, TValue> value)
        {
            if (node == null || (node is YamlScalarNode scalar && String.IsNullOrEmpty(scalar.Value)))
                return null;
            if (node is YamlMappingNode map)
            {
                var dict = new Dictionary<TKey, TValue>();
                foreach (var child in map)
                {
                    dict[key(child.Key)] = value(child.Value);
                }
                return dict;
            }
            throw new ArgumentException($"Can't convert {node} ({node.NodeType}) to dictionary");
        }

        public static Dictionary<string, string> ToStringDictionary(this YamlNode node)
        {
            return ToDictionary(node, x => (string)x, x => (string)x);
        }
    }
}
