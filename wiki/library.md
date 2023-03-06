## `library.yaml`
This file contains configuration that applies to the entire library. You pass its path to `NaiveMusicUpdater` as the first argument.

All file and folder paths configured here are relative to the file itself.

* `library`: Folder path where your music files reside.
* `extensions`: List of file extensions to be scanned and processed. Specifying leading dots is optional.
* `config_folders`: List of folders where [config](config.md) files are stored. Mirrors the directory structure of your library, and defaults to just your library folder.
* `logs`: Optional folder path for logging. If specified, the program saves its output here each time it's run.
* `cache`: File path to save a date cache to. This will make the program faster when running subsequent times, as files that have not been changed since the last run will be skipped.
* [`lyrics`](lyrics.md): Optional configuration for extracting/importing lyrics metadata.
* `chapters`: Like `lyrics`, but for chapters.
* [`art`](art.md): Optional configuration for embedding images and folder icons.
* [`custom_fields`](fields.md): List of custom [fields](fields.md).
* `named_strategies`: Every entry in this has a name for a key, and a [strategy](strategies.md) as a value. The names can be used in [config](config.md) files as shortcuts to reference this strategy.
* `find_replace`: Keys are [regular expressions](https://en.wikipedia.org/wiki/Regular_expression), values are strings. When generating a [clean name](names.md) from a file name, all of these substitutions are performed.
* `replay_gain`: Optional ReplayGain configuration.
  * `<extension>`: File extension. Specifying leading dots is optional.
    * `path`: File path to a ReplayGain program, such as [MP3Gain](http://mp3gain.sourceforge.net/), [metaflac](https://ftp.osuosl.org/pub/xiph/releases/flac/), or [aacgain](http://aacgain.altosdesign.com/). This program will be invoked for all music in your library of the given extension that has no ReplayGain metadata detected inside it.
    * `args`: Arguments to pass to the program.
* `keep`: Optional whitelist for metadata. If specified, the program will remove any metadata fields found in files that aren't specified here.
  * `id3v2`: List of MP3 frame IDs. The program also removes duplicate frames, leaving only one. To preserve duplicates, use an object with `id` set to the ID and `dupes` set to `true`.
  * `xiph`: List of XIPH metadata keys.
  * `ape`: List of APE tag fields.
