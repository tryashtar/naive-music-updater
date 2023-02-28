### `library.yaml`
This is for configuration that applies to the entire library. It's mostly just some top-level objects:
* `replay_gain`
  * `mp3`
    * `path`: File path to `mp3gain.exe`. This is run on every MP3 file in your library.
    * `args`: Arguments to pass to [MP3Gain](http://mp3gain.sourceforge.net/).
  * `flac`
    * `path`: File path to `metaflac.exe`. This is run on every FLAC file in your library.
    * `args`: Arguments to pass to [metaflac](https://ftp.osuosl.org/pub/xiph/releases/flac/).
  * `aac`
    * `path`: File path to `aacgain.exe`. This is run on every M4A file in your library.
    * `args`: Arguments to pass to [aacgain](http://aacgain.altosdesign.com/).
* `extensions`: List of file extensions that should be considered songs.
* `keep`
  * `id3v2`: List of MP3 frame IDs that should not be wiped. By default the program wipes them all. The program also removes duplicate frames, leaving only one. To preserve duplicates, use an object with `id` set to the ID and `dupes` set to `true`.
  * `xiph`: List of xiph metadata keys that should not be wiped. By default the program wipes them all.
* `source_auto_max_distance`: This is used to suggest corrections of [song source](sources.md) names.
* `named_strategies`: Every object in this has a name for a key, and a [strategy](strategies.md) as a value. The names can be used in config files as shortcuts to reference this strategy.
* `find_replace`: Keys are [regular expressions](https://en.wikipedia.org/wiki/Regular_expression), values are strings. When generating a [clean name](names.md) from a file name, all of these substitutions are performed.
