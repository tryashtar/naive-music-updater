## Names
The name of a song is just its file name, without the file extension.

A "clean name" is also derived from this name, by applying the `find_replace` operations defined in [`library.yaml`](library.md). Doing this allows clean names to contain characters that aren't valid in file names, for example.

Both names are only used when requested by a [value source](value-sources.md), they don't get auto-assigned to the title or anything like that.
