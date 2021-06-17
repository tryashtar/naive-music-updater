using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class ThenValuePicker : IValuePicker
    {
        public readonly IValuePicker BasePicker;
        public readonly IValuePicker ThenPicker;
        public ThenValuePicker(IValuePicker base_pick, IValuePicker then_pick)
        {
            BasePicker = base_pick;
            ThenPicker = then_pick;
        }

        public MetadataProperty PickFrom(MetadataProperty full)
        {
            return ThenPicker.PickFrom(BasePicker.PickFrom(full));
        }
    }
}
