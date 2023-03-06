# Naive Music Updater
I use this program to keep the music files on my computer consistently tagged and organized.

The idea is that you put your song files wherever you want, and then write configuration files that determine how the song's name and location can be converted into embedded metadata.

## Quickstart
`NaiveMusicUpdater` needs one argument: the path to a `library.yaml` file. This file can go anywhere. It contains configuration for the application as a whole.

Bare minimum, you need two keys in `library.yaml`:
* `library`: The folder path where your music files live.
* `extensions`: List of file extensions the program will scan. Example: `[mp3, flac, wav]`

By default, the program won't make any changes to your files, it will just list them all. To make changes, you create `config.yaml` files. By default, they go in the same folders as your music, and affect everything in their folder and deeper.

A simple use case is copying the file name of every song to its `title` field. A simple `config.yaml` can do this:

```yaml
songs:
  title:
    from: this
```

## Wiki
To learn more about what options are available, check these pages:
* [Library configuration](wiki/library.md)
* [Music configuration](wiki/config.md)
* [Reverse configuration](wiki/auto-config.md)
