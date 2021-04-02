﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public abstract class MetadataSelector
    {
        public abstract MetadataProperty GetRaw(IMusicItem item);

        public MetadataProperty Get(IMusicItem item)
        {
            var result = GetRaw(item);
            if (result == null)
                return MetadataProperty.Ignore();
            return result;
        }

        protected string ResolveNameOrDefault(IMusicItem item, IMusicItem current)
        {
            if (item == current)
                return item.GlobalCache.Config.CleanName(item.SimpleName);
            return item.GetMetadata(MetadataField.Title.Only).Get(MetadataField.Title).Value;
        }
    }
}