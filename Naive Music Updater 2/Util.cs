using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public static class Util
    {
        public static string StringPathAfterRoot(IMusicItem item)
        {
            return String.Join(Path.DirectorySeparatorChar.ToString(), item.PathFromRoot().Skip(1).Select(x => x.SimpleName));
        }
    }
}
