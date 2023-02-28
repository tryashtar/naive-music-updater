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
1. Embedded synchronized lyrics. For MP3s, this is a `SynchronisedLyricsFrame`. For FLACs, this is found in the values of the `LYRICS` field, in [LRC format](https://en.wikipedia.org/wiki/LRC_(file_format)).
2. Lyrics in this folder. These are `.lrc` files in [LRC format](https://en.wikipedia.org/wiki/LRC_(file_format)), named corresponding to their names in your music library.
3. Embedded unsynchronized lyrics. These are simple text lyrics without timestamps for each line.

The highest priority source of lyrics for a song is treated as the source of truth, and the others are modified to match. For example, if you have a song in `Take That/Shine.mp3` with embedded synchronized lyrics, the program will write them to `.music-cache/lyrics/Take That/Shine.lrc`, and copy them to the unsynchronized lyrics field. If you have a song with no embedded lyrics, you can write some yourself here and the program will embed them for you.

### `datecache.yaml`
The program does not scan every file every time, that would take forever, especially for someone like me who has like 18,000 songs. Every time the program modifies a file, it saves the date here. Next time, if the file's last modified date isn't any more recent than the date it saved before, the file will just be skipped.

If the program notices that a [config](config.md) or art file that would apply to a song has changed, it will also decide to scan the file. For example, this means if you change a `config.yaml` in your library root, the program will scan everything the next time it runs.
