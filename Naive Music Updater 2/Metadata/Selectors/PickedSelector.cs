using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class PickedSelector : MetadataSelector
    {
        public readonly MetadataSelector BaseSelector;
        public readonly IValuePicker Picker;
        public PickedSelector(MetadataSelector base_selector, IValuePicker picker)
        {
            BaseSelector = base_selector;
            Picker = picker;
        }

        public override MetadataProperty GetRaw(IMusicItem item)
        {
            var result = BaseSelector.GetRaw(item);
            if (result == null)
                return null;
            return Picker.PickFrom(result);
        }
    }
}
