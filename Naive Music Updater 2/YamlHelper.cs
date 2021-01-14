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

        public static string[] ToStringArray(YamlSequenceNode node)
        {
            return node.Select(x => (string)x).ToArray();
        }
    }
}
