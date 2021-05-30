# Naive Music Updater
I use this program to keep the music files on my computer consistently tagged and organized.

The idea is that all metadata can be derived from the song's name and location, with all of the rules defined through moderately powerful configuration files.

## Concepts
### Names
The name of a song is just its file name, without the file extension. A "clean name" is also derived from this name, by applying some of the settings defined in [`library.yaml`](#library.yaml). It includes case correction and custom substitutions. Doing this allows clean names to contain characters that aren't valid in file names, for example. The clean name is only used when requested, it doesn't get auto-assigned to the title or anything like that.

### Strategies
A strategy decides how to assign metadata to a song. It consists of one or more [metadata fields](#metadata-fields), and a matching [metadata selector](#metadata-selectors).

Here's an example of a simple strategy:
```
title: Joy to the World
artist: Isaac Watts
```
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
* `year`
* `lang`
* `genre`

These are mapped to various song metadata tags.

### Item Selectors
An item selector lets you pick one or more songs that will be affected by something, usually a [strategy](#strategies). The simplest item selector is just a string path, relative to the containing folder, that matches a song. For example, if there's a song in `C418/Volume Beta/Alpha.mp3`, then you can select it with just `Alpha` if you're in the `Volume Beta` folder. If you're in the `C418` folder, you can select it with `Volume Beta/Alpha`.

If you select a folder this way, it will not result in all of the containing songs being selected. Usually, item selectors are for selecting specific songs only.

A list of item selectors is by itself a valid item selector. Each selector is evaluated in turn, producing many selected songs. You can also write item selectors as an object to get special behavior. The behavior is defined by the value of the `type` key:

**`path`**  
Allows you to define a list of each subfolder that must be navigated in turn to reach the song. Each list entry can either be a string, or an object with a `regex` value, which will navigate any item matching the [regular expression](https://en.wikipedia.org/wiki/Regular_expression). For example, this selector selects all songs beginning with "C" across multiple folders:
```
type: path
path:
- C418
- regex: ^Volume .*$
- regex: ^C
```

---
**`subpath`**  
Allows you to change the folder a selector starts from. The new start folder is an item selector called `subpath`, and the selector to use is called `select`. For example, if you're in the C418 folder and want to select many songs in the Volume Alpha folder, you would have to write `Volume Alpha/` in front of every song. You can use `subpath` to avoid this:
```
type: subpath
subpath: Volume Alpha
select:
- Beginning
- Cat
- Chris
```

---
As a bonus, when the program finishes running, it will print out any item selectors that didn't find any songs. This is a sign that you made a typo, or something needs to be updated.

### Metadata Selectors
This is one of the more elaborate features of the program. Metadata selectors tell the program how to get a value that will be embedded in the song's metadata. There are many different kind of metadata selectors.

**String**
* A simple string will be used literally. For example, `title: Joy to the World` would set the title of all relevant songs to exactly that.
* `<this>` will select the song's "clean name." For example, `title: <this>` is an easy way to use file names to define your songs' titles.
* `<exact>` will select the song's file name, as-is, instead of the clean name.

**List**  
A list of selectors allows you to set multiple values in one property. For example, `performer: [Ricardo Arjona, Gaby Moreno]` can be used to set multiple artists.

**Object**  
The behavior is defined by the value of the `operation` key.

`parent`:  
This selects the "clean name" of a parent folder. `up` specifies the number of folders to navigate. If positive, it's relative to the root, so `1` would be whichever folder in the library root contains the song, `2` would be one deeper, etc. If negative, it's relative to the song, so `-1` would be whichever folder directly contains the song, `-2` would be one higher, etc. For example, if the songs are inside a folder named after the album, you can express that like this:
```
album:
  operation: parent
  up: -1
```

---
`split`:  
This allows you to grab a value from another metadata selector, split it, and take a specific piece. `from` is the selector to use. `separator` is what to split on. `index` is which piece to use. `no_separator` can be `ignore` or `exit`. If it's set to exit, the result must contain the separator somewhere. `out_of_bounds` can be `exit`, `wrap`, or `clamp`, which decides how the index should be used to choose the result. For example, if your songs are named like `C418 - wait`, you can use a strategy like this:
```
performer:
  operation: split
  from: <this>
  separator: " - "
  index: 0
  no_separator: exit
title:
  operation: split
  from: <this>
  separator: " - "
  index: 1
  out_of_bounds: clamp
```

---
`regex`:  
This is similar to `split`. It lets you extract a group from another selector according to a [regular expression](https://en.wikipedia.org/wiki/Regular_expression). `from` is the selector. `regex` is the expression, with at least one capture group. `group` is the name of the group to select. `fail` can be `ignore` or `exit`, to determine what to do if the regex didn't match. For example, if your songs are named like `wait (C418)`, you can use a strategy like this:
```
performer:
  operation: regex
  from: <this>
  regex: ^(?<title>.*?) \((?<artist>.*?)\)$
  group: artist
  fail: exit
title:
  operation: regex
  from: <this>
  regex: ^(?<title>.*?) \((?<artist>.*?)\)$
  group: title
```

---
`join`:  
This lets you combine two selectors with something in between them. `from1` and `from2` are the selectors. `with` is what to put between. For example, if your songs are named like `Piano Sonata No. 14/Movement 1`, and you want the full title to contain both, you can use a strategy like this:
```
title:
  operation: join
  from1:
    operation: parent
    up: -1
  with: ": "
  from2: <this>
```

---
`copy`:  
This lets you copy metadata from one field into another. The one value, `get`, is the field to copy. Note that this will copy the "final" value that ends up in that field. This means if later strategies modify the field you're copying from, this field will end up with those modifications as well. Example:
```
composer:
  operation: copy
  get: performer
```

---
`remove`:  
This deletes any metadata set by previously run strategies.

## Configuration
All songs start with "blank" or "ignore" metadata. This means the program will not make any changes to the song. Metadata is applied according to rules in `config.yaml` files. These files apply to any song they share a folder with, including subfolders.

For example, if you have a `config.yaml` file in the root of your library, those rules apply to every song in your entire library. If you have a `config.yaml` file in the C418 folder, those rules only apply to songs in that folder, and can override the root library rules.

Here are the options that can be included in a `config.yaml` file:

**`songs`**  
The value for this is a [strategy](#strategies). If you use a string, it will use the strategy defined in [`library.yaml`](#library.yaml) with that name. This strategy applies to all relevant songs unconditionally.

For example:
```
songs:
  title: <this>
  performer:
    operation: parent
    up: 2
```

---
**`set`**  
This allows you to apply strategies to specific songs. Each key is an [item selector](#item-selectors), and each value is a strategy.

For example:
```
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
```
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
```
order:
- Dearly Beloved
- Destati
- Treasured Memories
```

As a bonus, at the same time the program checks for unused selectors, it will print out any `order`s that didn't select every song in the folders it looked at. This is a sign that it should be updated to include those in the order.

## `.music-cache`
This hidden folder is created at the root of your music library folder. It contains a few things:

### `art`
Contains PNG files that are used to embed album art into your songs, and change the folder icons. Their names should correspond to folders in your music library. This will embed the image in the metadata of all songs in that folder, including songs inside of subfolders. You can mirror the folder structure of your library in the art folder to select specific folders. Additionally, you can create a file named `__contents__.png` inside of an art folder. Doing this will use the main image for the folder icon, but the `__contents__` image will be the one that gets embedded.

For example, if you have a folder named C418 in your music library, its image will be found in `.music-cache/art/C418.png`. All tracks in the C418 folder will be given that image. If you have folders named Volume Alpha and Volume Beta in the C418 folder, you can give them different album art by creating the images in `.music-cache/art/C418/Volume Alpha.png` and `.music-cache/art/C418/Volume Beta.png`, respectively. If you have a file in `.music-cache/art/C418/__contents__.png`, then the original `C418.png` image will now only be used as a folder icon; the new image will be the one that gets embedded into songs.

The program automatically generates `.ico` files from the PNGs. These are 256x256 pixels, as required by Windows to allow them to work as folder icons. When the source PNG is not a perfect square, the icon will be centered and given a transparent buffer to meet the size requirement.

### `logs`
All of the program output is both displayed to the console and written to a log file, named as `YYYY-MM-DD hh_mm_ss.txt`.

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

## Sources
The program writes a `sources.yaml` file to your library root. The intention of this file is to keep track of where you got your music from. As for me, I've downloaded my music from all kinds of random places, and it brings me peace of mind to know I can go back and get them again, or find them in a higher quality somewhere else.

Initially, it will list every folder as an object, with more objects for its subfolders. Songs will ultimately be listed under a `MISSING` object. This is where you can come in and manually write the URL or source you got those songs from. Each value can either be a list or a single song. Example:
```
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
