using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


namespace NaiveMusicUpdater
{
    public static class Util
    {
        public static string StringPathAfterRoot(IMusicItem item)
        {
            return String.Join(Path.DirectorySeparatorChar.ToString(), item.PathFromRoot().Skip(1).Select(x => x.SimpleName));
        }

        public static string RelativePath(FileSystemInfo from, FileSystemInfo to)
        {
            Func<FileSystemInfo, string> getPath = fsi =>
            {
                var d = fsi as DirectoryInfo;
                return d == null ? fsi.FullName : d.FullName.TrimEnd('\\') + "\\";
            };

            var fromPath = getPath(from);
            var toPath = getPath(to);

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        public static string RelativePath(string current, string destination)
        {
            return RelativePath(new DirectoryInfo(current), new FileInfo(destination));
        }

        public static void MoveDirectory(string from, string to)
        {
            var temp_windows_hack = to + "_TEMPORARY_FOLDER";
            Directory.Move(from, temp_windows_hack);
            Directory.Move(temp_windows_hack, to);
        }
    }
}
