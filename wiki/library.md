### `library.yaml`
This file contains configuration that applies to the entire library. You pass its path to `NaiveMusicUpdater` as the first argument.
* `library`: Folder path where your music files reside.
* `extensions`: List of file extensions to be scanned and processed. Specifying leading dots is optional.
* `logs`: Optional folder path for logging. If specified, the program saves its output here each time it's run.
* `replay_gain`: Optional ReplayGain configuration.
  * `<extension>`: File extension. Specifying leading dots is optional.
    * `path`: File path to a ReplayGain program, such as [MP3Gain](http://mp3gain.sourceforge.net/), [metaflac](https://ftp.osuosl.org/pub/xiph/releases/flac/), or [aacgain](http://aacgain.altosdesign.com/). This program will be invoked for all music in your library of the given extension.
    * `args`: Arguments to pass to the program.
* `keep`: Optional whitelist for metadata. If specified, the program will remove any metadata fields found in files that aren't specified here.
  * `id3v2`: List of MP3 frame IDs. The program also removes duplicate frames, leaving only one. To preserve duplicates, use an object with `id` set to the ID and `dupes` set to `true`.
  * `xiph`: List of xiph metadata keys.
* `named_strategies`: Every object in this has a name for a key, and a [strategy](strategies.md) as a value. The names can be used in [config](config.md) files as shortcuts to reference this strategy.
* `find_replace`: Keys are [regular expressions](https://en.wikipedia.org/wiki/Regular_expression), values are strings. When generating a [clean name](names.md) from a file name, all of these substitutions are performed.
