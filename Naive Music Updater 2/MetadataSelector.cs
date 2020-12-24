using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public abstract class MetadataSelector
    {
        public abstract string GetRaw(IMusicItem item);

        public MetadataProperty<string> Get(IMusicItem item)
        {
            string result = GetRaw(item);
            if (result == null)
                return MetadataProperty<string>.Ignore();
            if (result == "<remove>")
                return MetadataProperty<string>.Delete();
            return MetadataProperty<string>.Create(result);
        }

        protected string ResolveNameOrDefault(IMusicItem item, IMusicItem current)
        {
            if (item == current)
                return item.GlobalCache.Config.CleanName(item.SimpleName);
            return item.GlobalCache.Config.GetMetadataFor(item).Title.Value;
        }
    }

    public static class MetadataSelectorFactory
    {
        public static MetadataSelector FromToken(JToken token)
        {
            if (token.Type == JTokenType.Integer)
                return new SimpleParentSelector((int)token);
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                if (obj.TryGetValue("operation", out var operation))
                {
                    if ((string)operation == "split")
                        return new SplitOperationSelector(obj);
                    else if ((string)operation == "join")
                        return new JoinOperationSelector(obj);
                    else if ((string)operation == "regex")
                        return new RegexSelector(obj);
                }
            }
            if (token.Type == JTokenType.String)
            {
                string str = (string)token;
                if (str == "<this>")
                    return new FilenameSelector();
                if (str == "<title>")
                    return new GetMetadataSelector(x => x.Title);
                return new LiteralSelector(str);
            }
            throw new ArgumentException($"Couldn't figure out what kind of metadata selector this is: {token}");
        }
    }

    // cannot be used to get itself, use "<this>" instead
    public class SimpleParentSelector : MetadataSelector
    {
        private readonly int Number;
        public SimpleParentSelector(int number)
        {
            Number = number;
        }

        public override string GetRaw(IMusicItem item)
        {
            IMusicItem found;
            var list = item.PathFromRoot().ToList();
            if (Number >= 0)
            {
                if (Number >= list.Count)
                    return null;
                found = list[Number];
            }
            else
            {
                int index = list.Count + Number - 1;
                if (index < 0)
                    return null;
                found = list[index];
            }
            if (found == item)
                return null;
            return ResolveNameOrDefault(found, item);
        }
    }

    public class SplitOperationSelector : MetadataSelector
    {
        private readonly MetadataSelector From;
        private readonly string Separator;
        private readonly int Index;
        private readonly NoSeparatorDecision NoSeparator;
        private readonly OutofBoundsDecision OutofBounds;

        private enum NoSeparatorDecision
        {
            Exit,
            Ignore
        }

        private enum OutofBoundsDecision
        {
            Exit,
            Wrap,
            Clamp
        }

        // gets metadata "From" somewhere else and extracts a part of it by splitting the string and taking one of its pieces
        public SplitOperationSelector(JObject data)
        {
            From = MetadataSelectorFactory.FromToken(data["from"]);
            Separator = (string)data["separator"];
            Index = (int)data["index"];
            NoSeparator = NoSeparatorDecision.Ignore;
            if (data.TryGetValue("no_separator", out var sep) && (string)sep == "exit")
                NoSeparator = NoSeparatorDecision.Exit;
            OutofBounds = OutofBoundsDecision.Exit;
            if (data.TryGetValue("out_of_bounds", out var bounds))
            {
                if ((string)bounds == "wrap")
                    OutofBounds = OutofBoundsDecision.Wrap;
                if ((string)bounds == "clamp")
                    OutofBounds = OutofBoundsDecision.Clamp;
            }
        }

        public override string GetRaw(IMusicItem item)
        {
            var basetext = From.GetRaw(item);
            if (basetext == null)
                return null;
            string[] parts = basetext.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && NoSeparator == NoSeparatorDecision.Exit)
                return null;
            int index = Index;
            if (index < 0 || index >= parts.Length)
            {
                if (OutofBounds == OutofBoundsDecision.Exit)
                    return null;
                if (OutofBounds == OutofBoundsDecision.Wrap)
                    index %= parts.Length;
                if (OutofBounds == OutofBoundsDecision.Clamp)
                    index = Math.Max(0, Math.Min(parts.Length - 1, index));
            }
            return parts[index];
        }
    }

    public class RegexSelector : MetadataSelector
    {
        private readonly MetadataSelector From;
        private readonly Regex Regex;
        private readonly string Group;
        private readonly MatchFailDecision MatchFail;

        private enum MatchFailDecision
        {
            Exit,
            Ignore
        }

        // gets metadata "From" somewhere else and extracts a part of it by splitting the string and taking one of its pieces
        public RegexSelector(JObject data)
        {
            From = MetadataSelectorFactory.FromToken(data["from"]);
            Regex = new Regex((string)data["regex"]);
            Group = (string)data["group"];
            MatchFail = MatchFailDecision.Ignore;
            if (data.TryGetValue("fail", out var fail) && (string)fail == "exit")
                MatchFail = MatchFailDecision.Exit;
        }

        public override string GetRaw(IMusicItem item)
        {
            var basetext = From.GetRaw(item);
            if (basetext == null)
                return null;
            var match = Regex.Match(basetext);
            if (!match.Success)
                return MatchFail == MatchFailDecision.Ignore ? basetext : null;
            return match.Groups[Group].Value;
        }
    }

    public class JoinOperationSelector : MetadataSelector
    {
        private readonly MetadataSelector From1;
        private readonly MetadataSelector From2;
        private readonly string With;

        // gets metadata "From" two other places and combines them with "With" in between
        public JoinOperationSelector(JObject data)
        {
            From1 = MetadataSelectorFactory.FromToken(data["from1"]);
            From2 = MetadataSelectorFactory.FromToken(data["from2"]);
            With = (string)data["with"];
        }

        public override string GetRaw(IMusicItem item)
        {
            var text1 = From1.GetRaw(item);
            var text2 = From2.GetRaw(item);
            if (text1 == null && text2 == null)
                return null;
            if (text1 == null)
                return text2;
            if (text2 == null)
                return text1;
            return text1 + With + text2;
        }
    }

    public class FilenameSelector : MetadataSelector
    {
        public FilenameSelector()
        { }

        public override string GetRaw(IMusicItem item)
        {
            return ResolveNameOrDefault(item, item);
        }
    }

    public class LiteralSelector : MetadataSelector
    {
        private readonly string LiteralText;
        public LiteralSelector(string spec)
        {
            LiteralText = spec;
        }

        public override string GetRaw(IMusicItem item)
        {
            return LiteralText;
        }
    }

    public class GetMetadataSelector : MetadataSelector
    {
        public delegate MetadataProperty<string> MetadataGetter(SongMetadata meta);
        private readonly MetadataGetter Getter;
        public GetMetadataSelector(MetadataGetter getter)
        {
            Getter = getter;
        }

        public override string GetRaw(IMusicItem item)
        {
            return Getter(item.GetMetadata()).Value;
        }
    }
}
