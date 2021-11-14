using System;
using System.Collections.Generic;
using System.Linq;
using TagLib;
using Tag = TagLib.Tag;

namespace NaiveMusicUpdater
{
    public abstract class AbstractInterop<T> : ITagInterop where T : Tag
    {
        protected T Tag;
        private readonly TagTypes TagType;
        private readonly ByteVector OriginalVector;
        public bool Changed
        {
            get
            {
                return OriginalVector != RenderTag();
            }
        }
        private readonly Dictionary<MetadataField, InteropDelegates> Schema;
        private readonly Dictionary<string, WipeDelegates> WipeSchema;

        public AbstractInterop(T tag)
        {
            Tag = tag;
            TagType = tag.TagTypes;
            Schema = CreateSchema();
            WipeSchema = CreateWipeSchema();
            CustomSetup();
            OriginalVector = RenderTag();
        }

        protected virtual void CustomSetup() { }

        protected abstract ByteVector RenderTag();

        protected abstract Dictionary<MetadataField, InteropDelegates> CreateSchema();
        protected abstract Dictionary<string, WipeDelegates> CreateWipeSchema();

        public MetadataProperty Get(MetadataField field)
        {
            if (Schema.TryGetValue(field, out var entry))
                return entry.Getter();
            return MetadataProperty.Ignore();
        }

        public void Set(MetadataField field, MetadataProperty value)
        {
            if (Schema.TryGetValue(field, out var entry))
                Replace(field, entry, value);
        }

        private void Replace(MetadataField field, InteropDelegates delegates, MetadataProperty incoming)
        {
            var current = delegates.Getter();
            var result = MetadataProperty.Combine(current, incoming);
            if (!delegates.Equal(current, result))
            {
                Logger.WriteLine($"Changing {field.Name} in {TagType} tag from \"{current}\" to \"{result}\"");
                delegates.Setter(result);
            }
        }

        public void WipeUselessProperties()
        {
            foreach (var item in WipeSchema)
            {
                var result = item.Value.Wipe();
                if (result.Changed)
                    Logger.WriteLine($"Wiped {item.Key} in {TagType} tag from \"{result.OldValue}\" to \"{result.NewValue}\"");
            }
        }

        protected static MetadataProperty Get(string str)
        {
            if (str == null)
                return MetadataProperty.Ignore();
            return new MetadataProperty(new StringValue(str), CombineMode.Replace);
        }

        protected static MetadataProperty Get(uint num)
        {
            return new MetadataProperty(new NumberValue(num), CombineMode.Replace);
        }

        protected static MetadataProperty Get(string[] str)
        {
            if (str.Length == 0)
                return MetadataProperty.Ignore();
            return new MetadataProperty(new ListValue(str), CombineMode.Replace);
        }

        protected static string Value(MetadataProperty prop)
        {
            if (prop.Value.IsBlank)
                return null;
            return prop.Value.AsString().Value;
        }

        protected static string[] Array(MetadataProperty prop)
        {
            if (prop.Value.IsBlank)
                return new string[0];
            return prop.Value.AsList().Values.ToArray();
        }

        protected static uint Number(MetadataProperty prop)
        {
            if (prop.Value.IsBlank)
                return 0;
            return uint.Parse(Value(prop));
        }

        protected static bool StringEqual(MetadataProperty p1, MetadataProperty p2)
        {
            return Array(p1).SequenceEqual(Array(p2));
        }

        protected static bool NumberEqual(MetadataProperty p1, MetadataProperty p2)
        {
            if (p1.Value.IsBlank)
                p1 = Get(0);
            if (p2.Value.IsBlank)
                p2 = Get(0);
            var n1 = Array(p1).Select(uint.Parse);
            var n2 = Array(p2).Select(uint.Parse);
            return n1.SequenceEqual(n2);
        }

        protected static InteropDelegates Delegates(Getter get, Setter set)
        {
            return new InteropDelegates(get, set, StringEqual);
        }

        protected static InteropDelegates NumDelegates(Getter get, Setter set)
        {
            return new InteropDelegates(get, set, NumberEqual);
        }

        protected static WipeDelegates SimpleWipeRet(Func<string> get, Func<bool> set)
        {
            return new WipeDelegates(() =>
            {
                var before = get();
                bool changed = set();
                var after = get();
                return new WipeResult()
                {
                    OldValue = before,
                    NewValue = after,
                    Changed = changed
                };
            });
        }

        protected static WipeDelegates SimpleWipe(Func<string> get, Action set)
        {
            return SimpleWipeRet(() => get() ?? "(blank)", () =>
            {
                var before = get();
                set();
                var after = get();
                return before != after;
            });
        }

        protected static WipeDelegates SimpleWipe(Func<uint> get, Action set)
        {
            return SimpleWipeRet(() => get().ToString(), () =>
            {
                var before = get();
                set();
                var after = get();
                return before != after;
            });
        }

        protected static WipeDelegates SimpleWipe(Func<string[]> get, Action set)
        {
            return SimpleWipeRet(() => String.Join(";", get()), () =>
            {
                var before = get();
                set();
                var after = get();
                return !ArrayEquals(before, after);
            });
        }

        private static bool ArrayEquals<U>(U[] one, U[] two)
        {
            if (one == null)
                return two == null;
            if (two == null)
                return one == null;
            return one.SequenceEqual(two);
        }
    }
}
