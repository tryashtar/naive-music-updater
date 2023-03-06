## Lyrics & Chapters
This program has functionality for embedding and extracting lyrics and chapter metadata, but it's not with the typical [config](config.md) system.

A single music file can have different sources of lyrics: embedded metadata, or lyrics in an external text file. It may even have more than one! You can choose which of these is your "top priority," and what to do with the rest.

## Sources

For lyrics, these are the possible sources:
* `simple_embedded`: Unsyced lyrics embedded in the song's metadata. Just lines.
* `synced_embedded`: Lyrics embedded in the song's metadata, with timestamps for each line.
* `rich_embedded`: My custom JSON format for lyrics embedded in the song's metadata. Includes channels and start/end times.
* `synced_file`: External `lrc` text file matching the path of the song, with timestamps for each line.
* `rich_file`: External JSON text file in my custom format.

And for chapters:
* `simple_embedded`: Chapters embedded in the song's metadata, each with a timestamp.
* `rich_embedded`: My custom JSON format for chapters. Includes start/end times.
* `simple_file`: External `chp` text file matching the path of the song.
* `rich_file`: External JSON text file in my custom format.

## Library Configuration

The `lyrics` and `chapters` options in [`library.yaml`](library.md) control this behavior. They both have these options:

* `folder`: Folder path where external lyrics/chapter text files are stored. Mirrors the directory structure of your library.
* `config`: Each key in this dictionary is a source name above, and the value is either `ignore` (do nothing, the default), `remove` (delete this source), or `replace` (replace with the highest-priority source).
* `priority`: List of the source names above. The first one that a song has will be copied to any others marked `replace`.

---

Here are some examples:

If you want to write your lyrics in a text editor and embed them into your songs:
```yaml
lyrics:
  folder: lyrics
  config:
    simple_embedded: replace
    synced_embedded: replace
   priority:
   - synced_file
```

If you want to extract the lyrics from your songs into text form:
```yaml
lyrics:
  folder: lyrics
  config:
    synced_file: replace
   priority:
   - synced_embedded
   - simple_embedded
```

If you want to only have embedded synced lyrics:
```yaml
lyrics:
  folder: lyrics
  config:
    synced_embedded: replace
    simple_embedded: remove
   priority:
   - synced_embedded
   - simple_embedded
```
