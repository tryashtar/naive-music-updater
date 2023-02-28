# Naive Music Updater
I use this program to keep the music files on my computer consistently tagged and organized.

The idea is that you put your song files wherever you want, and then write configuration files that determine how the song's name and location can be converted into embedded metadata.

## Quickstart
`NaiveMusicUpdater` needs one argument: the path to a `library.yaml` file. This file can go anywhere. It contains all of the configuration for the application.

Bare minimum, you need two keys in `library.yaml`:
* `library`: The folder path where your music files live.
* `extensions`: List of file extensions the program will scan. Example: `[mp3, flac, wav]`

By default, the program won't make any changes to your files.
