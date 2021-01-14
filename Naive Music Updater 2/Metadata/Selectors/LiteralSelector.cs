using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class LiteralSelector : MetadataSelector
    {
        private readonly string LiteralText;
        public LiteralSelector(string spec)
        {
            LiteralText = spec;
        }

        public override MetadataProperty GetRaw(IMusicItem item)
        {
            return MetadataProperty.Single(LiteralText, CombineMode.Replace);
        }
    }
}
