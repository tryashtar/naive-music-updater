﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class LiteralStringSource : IValueSource
    {
        public readonly string Literal;
        public LiteralStringSource(string literal)
        {
            Literal = literal;
        }

        public IValue Get(IMusicItem item)
        {
            return new StringValue(Literal);
        }
    }
}