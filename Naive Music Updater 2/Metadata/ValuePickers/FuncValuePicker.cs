using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public delegate string SingleGet(IList<string> list);
    public delegate IEnumerable<string> MultiGet(IList<string> list);
    public delegate bool IsValid(IList<string> list);
    public class FuncValuePicker : IValuePicker
    {
        public readonly MultiGet Getter;
        public readonly IsValid Checker;
        public FuncValuePicker(SingleGet getter, IsValid is_valid = null)
        {
            Getter = x => new[] { getter(x) };
            Checker = is_valid;
        }

        public FuncValuePicker(MultiGet getter, IsValid is_valid = null)
        {
            Getter = getter;
            Checker = is_valid;
        }

        public MetadataProperty PickFrom(MetadataProperty full)
        {
            if (Checker == null || Checker(full.ListValue))
                return MetadataProperty.List(Getter(full.ListValue).ToList(), full.CombineMode);
            else
                return MetadataProperty.Ignore();
        }
    }
}
