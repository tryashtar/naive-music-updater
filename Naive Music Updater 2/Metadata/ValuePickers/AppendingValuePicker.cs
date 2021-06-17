using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class AppendingValuePicker : IValuePicker
    {
        public readonly IEnumerable<IValuePicker> Pickers;
        public AppendingValuePicker(IEnumerable<IValuePicker> pickers)
        {
            Pickers = pickers;
        }

        public MetadataProperty PickFrom(MetadataProperty full)
        {
            var list = new List<string>();
            foreach (var picker in Pickers)
            {
                list.AddRange(picker.PickFrom(full).ListValue);
            }
            return MetadataProperty.List(list, CombineMode.Replace);
        }
    }
}
