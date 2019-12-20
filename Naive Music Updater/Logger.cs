using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public static class Logger
    {
        private static StreamWriter Writer;

        public static void Open(string path)
        {
            using (File.Create(path)) ;
            Writer = new StreamWriter(path);
        }

        public static void Close()
        {
            Writer.Close();
        }

        public static void WriteLine(string text)
        {
            Console.WriteLine(text);
            Writer.WriteLine(text);
        }
    }
}
