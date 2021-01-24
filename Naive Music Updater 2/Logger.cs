﻿using System;
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
        private static string UnwrittenData = "";
        private static int TabCount = 0;

        public static void Open(string path)
        {
            Writer = new StreamWriter(File.Create(path));
            Writer.Write(UnwrittenData);
            UnwrittenData = "";
        }

        public static void Close()
        {
            Writer?.Close();
        }

        private static void Write(string text)
        {
            if (Writer == null)
                UnwrittenData += text + Environment.NewLine;
            else
                Writer.WriteLine(text);
        }

        public static void WriteLine(string text)
        {
            string tabs = new string('\t', TabCount);
            Console.WriteLine(tabs + text);
            Write(tabs + text);
        }

        public static string ReadLine()
        {
            string text = Console.ReadLine();
            string tabs = new string('\t', TabCount);
            Write(tabs + text);
            return text;
        }

        public static void TabIn() => TabCount++;
        public static void TabOut() => TabCount--;
    }
}
