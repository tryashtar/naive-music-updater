﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib.Flac;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class FieldMapMetadataStrategy : IMetadataStrategy
    {
        private readonly Dictionary<MetadataField, IValueResolver> Fields = new();

        public FieldMapMetadataStrategy(YamlMappingNode yaml)
        {
            Fields = yaml.ToDictionary(
                x => MetadataField.FromID(x.String()),
                x => ValueResolverFactory.Create(x)
            );
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            var meta = new Metadata();
            foreach (var pair in Fields)
            {
                if (desired(pair.Key))
                    meta.Register(pair.Key, MetadataProperty.FromValue(pair.Value.Resolve(item), CombineMode.Replace));
            }
            return meta;
        }
    }
}
