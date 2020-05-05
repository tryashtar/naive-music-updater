using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public static class Logger
    {
        private static StreamWriter Writer;
        private static int TabCount = 0;

        public static void Open(string path)
        {
            using (File.Create(path)) {; }
            Writer = new StreamWriter(path);
        }

        public static void Close()
        {
            Writer.Close();
        }

        public static void WriteLine(string text)
        {
            string tabs = new string('\t', TabCount);
            Console.WriteLine(tabs + text);
            Writer.WriteLine(tabs + text);
        }

        public static void TabIn() => TabCount++;
        public static void TabOut() => TabCount--;
    }
}
