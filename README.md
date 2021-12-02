# Naive Music Updater
I use this program to keep the music files on my computer consistently tagged and organized.

The idea is that you put your song files wherever you want, and then write configuration files that determine how the song's name and location can be converted into embedded metadata.

## Concepts
### Names
The name of a song is just its file name, without the file extension. A "clean name" is also derived from this name, by applying some of the settings defined in [`library.yaml`](#library.yaml). It includes case correction and custom substitutions. Doing this allows clean names to contain characters that aren't valid in file names, for example. The clean name is only used when requested, it doesn't get auto-assigned to the title or anything like that.

### Strategies
A strategy decides how to assign metadata to the [metadata fields](#metadata-fields) of a song.

The simplest form of a strategy is a context-free "field spec." There are two different types of field specs:

**Mapping Field Spec**  
This consists of a map of fields, and a matching [field setter](#field-setters).

Here's an example of a simple strategy, using a mapping field spec:
```yaml
title: Joy to the World
composer: Isaac Watts
```

---

**Multiple Field Spec**  
This type allows you to set multiple fields to the same value. The fields are listed in `fields`, and the field setter to use for all of them is called `set`. You can also use `*` to assign the value to all fields.

Here's an example of this kind of field spec:
```yaml
fields: [performer, composer, album artist]
set: Isaac Watts
```

---

**Context Strategies**
Each of those types of field specs works as a strategy on its own. However, you can also provide "context" to a strategy. This allows the field setters to modify a common value.

To do this, put the field spec under a key called `apply`, and provide a [value provider](#value-providers) called `source`. Doing this will allow the field setters in the spec to reference and modify the provided value.

---

A list of strategies is also a valid strategy. Each strategy is applied in turn. A typical song will have many strategies applied to it; later metadata assignments will override earlier ones.

### Metadata Fields
All the valid metadata fields are as follows:
* `title`
* `album`
* `performer`
* `album artist`
* `composer`
* `arranger`
* `comment`
* `track`
* `track total`
* `disc`
* `disc total`
* `year`
* `lang`
* `genre`

These are mapped to various song metadata tags.

### Item Selectors
An item selector lets you pick one or more songs that will be affected by something, usually a [strategy](#strategies). The simplest item selector is just a string path, relative to the containing folder, that matches a song. For example, if there's a song in `C418/Volume Beta/Alpha.mp3`, then you can select it with just `Alpha` if you're in the Volume Beta folder. If you're in the C418 folder, you can select it with `Volume Beta/Alpha`.

If you select a folder this way, it will not result in all of the containing songs being selected. Usually, item selectors are for selecting specific songs only.

A list of item selectors is by itself a valid item selector. Each selector is evaluated in turn, producing many selected songs.

You can also write item selectors as an object to get special behavior. 

**Predicate Path**  
Allows you to define a list of each subfolder that must be navigated in turn to reach the song, called `path`. Each list entry can either be a string, or an object with a `regex` value, which will navigate any item matching the [regular expression](https://en.wikipedia.org/wiki/Regular_expression).

For example, this selector selects all songs beginning with "C" across multiple folders:
```yaml
path:
- C418
- regex: ^Volume .*$
- regex: ^C
```

---
**Subpath**  
Allows you to change the folder a selector starts from. The new start folder is an item selector called `subpath`, and the selector to use is called `select`.

For example, if you're in the C418 folder and want to select many songs in the Volume Alpha folder, you would have to write `Volume Alpha/` in front of every song. You can use `subpath` to avoid this:
```yaml
subpath: Volume Alpha
select:
- Beginning
- Cat
- Chris
```

---
As a bonus, when the program finishes running, it will print out any item selectors that didn't find any songs. This is a sign that you made a typo, or something needs to be updated.

### Field Setters
A field setter determines how a value should be combined with existing song metadata. The simplest field setter is just a [value source](#value-sources), which replaces the existing metadata. However, you can also specify additional rules.

Use `mode` to determine how the new metadata should be combined with previously assigned metadata. The combine mode can be one of the following:
* `replace` (this is the default, it doesn't need to be specified)
* `append`
* `prepend`
* `remove`

When using `remove`, no other information needs to be provided. It results in the existing metadata being removed. For example:
```yaml
title:
  mode: remove
```

For the other options, you must include a value source called `source`. To add C418 to the list of composers, for example:
```yaml
composer:
  mode: append
  source: C418
```

If this field setter is part of a field spec [with context](#context-strategies), then there's already a value source provided by the strategy itself. In this case, you don't need to specify a `source`, though you can specify a [value operator](#value-operators) called `modify` to change the value.

For example, here is a complete strategy that splits the clean name of the file in half, and assigns each half to different fields:
```yaml
source:
  from: this
  modify:
    split: " - "
apply:
  album artist:
    modify:
      index: 0
  album:
    modify:
      index: 1
```

### Value Sources
A value source is a method for obtaining a value that can be modified, then ultimately embedded into metadata.

The simplest value source is just a literal string or list of strings. For example, `title: Joy to the World` would set the title of all relevant songs to exactly that.

To get information from the file structure of the songs, you have to use an object. It contains three values:

**`from`**  
This is a "single item selector." Its purpose is to uniquely identify a file or folder, relative to the song being modified, to fetch data from. It can be set to `this` or `self` to select the song file in question.

Another option is using `up`. This is an integer value equal to the number of folders to navigate up towards the library root. That folder will be selected. For example, to select the parent folder:
```yaml
from:
  up: 1
```

Another option is using `from_root`. This is like the opposite of `up`; it's an integer value equal to the number of folders to navigate down, starting from the root, towards the song in question. For example, to select the folder in the library root that (eventually) contains the current song:
```yaml
from:
  from_root: 1
```

The last option is using a normal [item selector](#item-selectors). It's called `selector`, and the only important thing to know is that an error will be thrown if it selects more than one item. For example:
```yaml
from:
  selector: Volume Alpha/Living Mice
```

---

**`value`**  
This is to decide what kind of data should be acquired from the selected item. The default is [`clean_name`](#names), but you can use `file_name` to select the original file name.

The other option is `copy`, which lets you choose a metadata field. Note that this will copy the "final" value that ends up in that field. This means if later strategies modify the field you're copying from, this field will end up with those modifications as well. For example:
```yaml
album artist:
  from: this
  value:
    copy: performer
```

---

**`modify`**  
Lastly, you can optionally provide a [value operator](#value-operators) that will modify the acquired value.

### Value Operators
Value operators let you modify a value before it gets ultimately assigned to metadata. A single value can be split into many values, or merged, or distributed around. There are quite a few value operators. Aside from a few string shortcuts, all are objects with a couple keys.

**Strings**  
`first` and `last` are shortcuts for index operators with index 0 and -1, respectively. Using a number directly is also a shortcut for an index operator. Each of these use "exit" out-of-bounds mode. To change that, use the full index operator.

---

**Split**  
This splits a single string value into a list of values, using a separator. `split` is the string separator. `when_none` determines what to do if the separator wasn't found anywhere. `ignore` is the default, meaning you'll get a one-entry list. `exit` means the value will be discarded.

For example, if your folder contains multiple artist names, separated by commas, you can use a strategy like this:
```yaml
performer:
  from:
    up: 1
  modify:
    split: ", "
```

---

**Index**  
This allows you to select a particular value from a list of values. `index` is the zero-based index to use. You can use negative values to start from the end. `out_of_bounds` decides what to do if the index falls out of bounds. It defaults to `exit`, meaning the value will be discarded. It can be set to `wrap` to perform modulo on the index, or `clamp`, to clamp the index to the nearest end of the list.

This is most often used in tandem with an earlier `split` operation. For example, if your songs are named like `C418 - wait`, you can use a strategy like this:
```yaml
source:
  from: this
  modify:
    split: " - "
apply:
  performer:
    modify:
      index: 0
  title:
    modify:
      index: 1
```

---

**Regex**  
There are two operators that deal with regex, that must be used one after the other. The first uses a [regular expression](https://en.wikipedia.org/wiki/Regular_expression) called `regex`. A value called `fail` determines what to do if the value did not match the expression. It defaults to `exit`, meaning the value is discarded, but can be set to `take_whole`, meaning the value is kept as-is.

Your regular expression should contain groups. The values inside of those groups can then be extracted with another operator. It uses `group` to select the name of the group.

For example, if your songs are named like `wait (C418)`, you can use a strategy like this:
```yaml
source:
  from: this
  modify:
    regex: ^(?<outside>.*?) \((?<in_parens>.*?)\)$
apply:
  performer:
    modify:
      group: outside
  title:
    modify:
      group: in_parens
```

---

**Append**  
This appends a string value to an existing string value.

Anyway, it's just a [value source](#value-sources) called `append`. The value obtained is appended to the end of the value being modified. Use `prepend` to append to the beginning.

For example, if your songs are placed in folders named like `Piano Sonata No. 14/Movement 1`, and you want the full title to contain both, you can use a strategy like this:
```yaml
title:
  from: this
  modify:
    prepend:
      from:
        up: 1
```

## Configuration
All songs start with "blank" or "ignore" metadata. This means the program will not make any changes to the song. Metadata is applied according to rules in `config.yaml` files. These files apply to any song they share a folder with, including subfolders.

For example, if you have a `config.yaml` file in the root of your library, those rules apply to every song in your entire library. If you have a `config.yaml` file in the C418 folder, those rules only apply to songs in that folder, and can override the root library rules.

Here are the options that can be included in a `config.yaml` file:

**`songs`**  
The value for this is a [strategy](#strategies). If you use a string, it will use the strategy defined in [`library.yaml`](#library.yaml) with that name. This strategy applies to all relevant songs unconditionally.

For example:
```yaml
songs:
  title:
    from: this
  performer:
    operation: parent
    up: 2
```

---
**`set`**  
This allows you to apply strategies to specific songs. Each key is an [item selector](#item-selectors), and each value is a strategy.

For example:
```yaml
set:
  Beyond the Sea:
    performer: Robbie Williams
    language: eng
  Classic/Maple Leaf Rag:
    performer: Scott Joplin
```

---
**`set all`**  
This is like `set`, but lets you apply the same strategy to multiple selectors. The main reason for this is that a YAML list doesn't work as a key. `set all` is a list of objects, with `names` as a list of item selectors, and `set` as a strategy.

For example:
```yaml
set all:
- names: [Cat, Far, Ward]
  set:
    comment: Green
- names: [Blocks, Chirp]
  set:
    comment: Red
```

---
**`order`**  
This is a more convenient way to set track number metadata than using the `track` and `track total` fields. It's simply an item selector. The items it selects will be assigned track metadata according to their order and count.

For example, these tracks will be assigned a track number of 1, 2, and 3, respectively, and all will be assigned a track total of 3:
```yaml
order:
- Dearly Beloved
- Destati
- Treasured Memories
```

---
**`discs`**  
Likewise, you can assign track and disc number metadata at the same time using this option. It's an object with disc numbers as keys, and item selectors as values.

For example, these tracks will be assigned a track number of 1, 2, or 3, a track total of 3, a disc number of 1 or 2, and a disc total of 2.
```yaml
discs:
  1:
  - Night of Fate
  - Destiny's Force
  - The Deep End
  2:
  - Working Together
  - Vim and Vigor
  - Desire for All That is Lost
```

As a bonus, at the same time the program checks for unused selectors, it will print out any `order`s or `discs` that didn't select every song in the folders it looked at. This is a sign that it should be updated to include those in the order.

## `.music-cache`
This hidden folder is created at the root of your music library folder. It contains a few things:

### `art`
This folder contains PNG files that are used to embed album art into your songs, and change the folder icons. Their names should correspond to folders in your music library. This will embed the image in the metadata of all songs in that folder, including songs inside of subfolders. You can mirror the folder structure of your library in the art folder to select specific folders. Additionally, you can create a file named `__contents__.png` inside of an art folder. Doing this will use the main image for the folder icon, but the `__contents__` image will be the one that gets embedded.

For example, if you have a folder named C418 in your music library, its image will be found in `.music-cache/art/C418.png`. All tracks in the C418 folder will be given that image. If you have folders named Volume Alpha and Volume Beta in the C418 folder, you can give them different album art by creating the images in `.music-cache/art/C418/Volume Alpha.png` and `.music-cache/art/C418/Volume Beta.png`, respectively. If you have a file in `.music-cache/art/C418/__contents__.png`, then the original `C418.png` image will now only be used as a folder icon; the new image will be the one that gets embedded into songs.

The program automatically generates `.ico` files from the PNGs. These are 256x256 pixels, as required by Windows to allow them to work as folder icons. When the source PNG is not a perfect square, the icon will be centered and given a transparent buffer to meet the size requirement.

### `logs`
All of the program output is both displayed to the console and written to a log file in this folder, named as `YYYY-MM-DD hh_mm_ss.txt`.

### `lyrics`
The program looks for three different sources of lyrics and tries to make them consistent. They are, in order of priority:
1. Embedded synchronized lyrics. For MP3s, this is a `SynchronisedLyricsFrame`. For FLACs, this is found in the values of the `SYNCED LYRICS` field, in [LRC format](https://en.wikipedia.org/wiki/LRC_(file_format)).
2. Lyrics in this folder. These are `.lrc` files in [LRC format](https://en.wikipedia.org/wiki/LRC_(file_format)), named corresponding to their names in your music library.
3. Embedded unsynchronized lyrics. These are simple text lyrics without timestamps for each line.

The highest priority source of lyrics for a song is treated as the source of truth, and the others are modified to match. For example, if you have a song in `Take That/Shine.mp3` with embedded synchronized lyrics, the program will write them to `.music-cache/lyrics/Take That/Shine.lrc`, and copy them to the unsynchronized lyrics field. If you have a song with no embedded lyrics, you can write some yourself here and the program will embed them for you.

### `datecache.yaml`
The program does not scan every file every time, that would take forever, especially for someone like me who has like 12,000 songs. Every time the program modifies a file, it saves the date here. Next time, if the file's last modified date isn't any more recent than the date it saved before, the file will just be skipped.

If the program notices that a [config](#configuration) or art file that would apply to a song has changed, it will also decide to scan the file. For example, this means if you change a `config.yaml` in your library root, the program will scan everything the next time it runs.

### `library.yaml`
This is for configuration that applies to the entire library. It's mostly just some top-level objects:
* `replay_gain`
  * `mp3`
    * `path`: File path to `mp3gain.exe`. This is run on every MP3 file in your library.
    * `args`: Arguments to pass to [MP3Gain](http://mp3gain.sourceforge.net/).
  * `flac`
    * `path`: File path to `metaflac.exe`. This is run on every FLAC file in your library.
    * `args`: Arguments to pass to [metaflac](https://ftp.osuosl.org/pub/xiph/releases/flac/).
* `extensions`: List of file extensions that should be considered songs.
* `clear_private_owners`: List of certain frames that should be removed from MP3s, though this doesn't currently do anything right now.
* `keep_frames`
  * `ids`: List of MP3 frame IDs that should not be wiped. By default the program wipes them all.
  * `text`: List of [regular expressions](https://en.wikipedia.org/wiki/Regular_expression). Frames with text that matches any of these will not be wiped, regardless of its ID.
* `source_auto_max_distance`: This is used to suggest corrections of [song source](#sources) names.
* `named_strategies`: Every object in this has a name for a key, and a [strategy](#strategies) as a value.
* `lowercase`: List of words that should be made lowercase when case-correcting file names.
* `skip`: List of song names that should be left exactly the same and not be case-corrected.
* `find_replace`: When generating the song's "clean name" from its file name, all of these will be run.
* `map`: Any song with its name as the key will have its "clean name" set exactly to the value with no further processing.
* `title_to_filename`: For characters that can't be saved as file names, values for their replacements.
* `title_to_foldername`: The same thing but for folders.
* `title_splits`: List of [regular expressions](https://en.wikipedia.org/wiki/Regular_expression) with named groups. When generating a clean name, the program will recursively process any groups ending with `_title` when considering capitalization rules, and not process any groups ending with `_skip`. Other groups are entirely excluded from the final clean name.

## Sources
The program writes a `sources.yaml` file to your library root. The intention of this file is to keep track of where you got your music from. As for me, I've downloaded my music from all kinds of random places, and it brings me peace of mind to know I can go back and get them again, or find them in a higher quality somewhere else.

Initially, it will list every folder as an object, with more objects for its subfolders. Songs will ultimately be listed under a `MISSING` object. This is where you can come in and manually write the URL or source you got those songs from. Each value can either be a list or a single song.

Example:
```yaml
C418:
  Volume Alpha:
    https://www.youtube.com/watch?v=wnHy42Zh14Y: Moog City
    https://c418.bandcamp.com/album/minecraft-volume-alpha:
    - Key
    - Cat
    - Sweden
    https://www.youtube.com/watch?v=4i0d6CPLSGoL: Dry Hands
```
When the program finishes running, it will print out the names of any songs whose source is missing, as well as any songs that have a listed source, but actually aren't in your library, probably because they were removed.

In case it was just because you renamed a file, the program tries to figure out if it can autocorrect song names. The threshold for how hard it tries (according to [levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance)) is configurable in [`library.yaml`](#library.yaml). It will print out its suggestions, and you can simply press enter to reject, or type anything first to accept.
