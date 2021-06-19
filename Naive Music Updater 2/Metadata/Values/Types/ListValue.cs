﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class ListValue : IValue
    {
        public readonly List<string> Values;
        public ListValue(IEnumerable<string> values)
        {
            Values = values.ToList();
        }

        public ListValue AsList()
        {
            return this;
        }

        public StringValue AsString()
        {
            return new StringValue(Values.First());
        }

        public bool HasContents => Values.Any();
    }
}
