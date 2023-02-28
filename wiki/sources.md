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

In case it was just because you renamed a file, the program tries to figure out if it can autocorrect song names. The threshold for how hard it tries (according to [levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance)) is configurable in [`library.yaml`](library.md). It will print out its suggestions, and you can simply press enter to reject, or type anything first to accept.
