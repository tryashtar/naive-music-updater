using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class SongPredicate
    {
        public SongPredicate(JObject json)
        {

        }

        public bool Matches(Song song)
        {
            return true;
        }
    }

    public class MetadataStrategy
    {
        private int? Artist;
        private int? Album;
        private int? Comment;
        public MetadataStrategy(JObject json)
        {
            json.TryGetValue("artist", out var artist);
            if (artist.Type == JTokenType.Integer)
                Artist = (int)artist;

            json.TryGetValue("album", out var album);
            if (album.Type == JTokenType.Integer)
                Album = (int)album;

            json.TryGetValue("comment", out var comment);
            if (comment.Type == JTokenType.Integer)
                Comment = (int)comment;
        }

        public IMusicItem GetArtist(Song song)
        {
            if (Artist.HasValue)
                return From(song, (int)Artist);
            return null;
        }

        public IMusicItem GetAlbum(Song song)
        {
            if (Album.HasValue)
                return From(song, (int)Album);
            return null;
        }

        public IMusicItem GetComment(Song song)
        {
            if (Comment.HasValue)
                return From(song, (int)Comment);
            return null;
        }

        private IMusicItem From(IMusicItem item, int moves)
        {
            var list = item.PathFromRoot().ToList();
            if (moves >= 0)
            {
                if (moves >= list.Count)
                    return null;
                return list[moves];
            }
            int index = list.Count - moves;
            if (index < 0)
                return null;
            return list[moves];
        }
    }
}
