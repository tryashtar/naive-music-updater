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
            if (!values.Any())
                throw new ArgumentException($"Empty lists not allowed");
            Values = values.ToList();
        }

        public ListValue(params string[] values) : this((IEnumerable<string>)values)
        {
        }

        public ListValue AsList() => this;
        public StringValue AsString() => new(Values.First());
        public bool IsBlank => false;

        public override string ToString() => $"[{String.Join(", ", Values)}]";
    }
}
