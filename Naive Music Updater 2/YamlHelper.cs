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
            using var reader = new StreamReader(File.OpenRead(file_path));
            var stream = new YamlStream();
            stream.Load(reader);
            var root = stream.Documents.SingleOrDefault()?.RootNode;
            return root;
        }

        public static void SaveToFile(YamlNode node, string file_path)
        {
            var doc = new YamlDocument(node);
            var stream = new YamlStream(doc);
            using var writer = File.CreateText(file_path);
            stream.Save(writer, false);
        }

        public static List<OutType> ToList<OutType>(this YamlNode node, Func<YamlNode, YamlNode, OutType> getter)
        {
            if (node == null)
                return null;
            return ((YamlMappingNode)node).Children.Select(x => getter(x.Key, x.Value)).ToList();
        }

        public static List<OutType> ToList<OutType>(this YamlNode node, Func<YamlNode, OutType> getter)
        {
            if (node == null)
                return null;
            return ((YamlSequenceNode)node).Children.Select(x => getter(x)).ToList();
        }

        public static List<OutType> ToListFromStrings<OutType>(this YamlNode node, Func<string, OutType> getter)
        {
            return ToList(node, (YamlNode x) => getter(x.String()));
        }

        public static List<string> ToList(this YamlNode node)
        {
            return ToListFromStrings(node, x => x);
        }

        public static Dictionary<string, string> ToDictionary(this YamlNode node)
        {
            return ToDictionary(node, x => x.String());
        }

        public static Dictionary<string, OutType> ToDictionary<OutType>(this YamlNode node, Func<YamlNode, OutType> value_getter)
        {
            return ToDictionary(node, x => x.String(), value_getter);
        }

        public static Dictionary<KeyType, ValueType> ToDictionary<KeyType, ValueType>(this YamlNode node, Func<YamlNode, KeyType> key_getter, Func<YamlNode, ValueType> value_getter)
        {
            if (node == null)
                return null;
            return ((YamlMappingNode)node).Children.ToDictionary(
                x => key_getter(x.Key),
                x => value_getter(x.Value));
        }

        public static OutType NullableParse<OutType>(this YamlNode node, Func<YamlNode, OutType> parser) where OutType : class
        {
            if (node == null)
                return null;
            return parser(node);
        }

        public static OutType Parse<OutType>(this YamlNode node, Func<YamlNode, OutType> parser) where OutType : class
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            return parser(node);
        }

        public static T ToEnum<T>(this YamlNode node, T def) where T : struct
        {
            if (node == null)
                return def;
            return Util.ParseUnderscoredEnum<T>(node.String());
        }

        public static T? ToEnum<T>(this YamlNode node) where T : struct
        {
            if (node == null)
                return null;
            return Util.ParseUnderscoredEnum<T>(node.String());
        }

        public static string String(this YamlNode node)
        {
            if (node == null)
                return null;
            return ((YamlScalarNode)node).Value;
        }

        public static int? Int(this YamlNode node)
        {
            var result = String(node);
            if (result == null)
                return null;
            return int.Parse(result);
        }

        public static YamlNode Go(this YamlNode node, params string[] path)
        {
            if (node == null)
                return null;
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
            if (node is YamlMappingNode map && map.Children.TryGetValue(key, out var result))
                return result;
            return null;
        }
    }
}
