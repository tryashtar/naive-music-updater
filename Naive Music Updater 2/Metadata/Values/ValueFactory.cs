using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public interface IValue
    {
        public MetadataProperty ToProperty();
    }

    public static class ValueFactory
    {

    }
}
