using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public interface IValue
    {
        StringValue AsString();
        ListValue AsList();
        bool HasContents { get; }
    }
}
