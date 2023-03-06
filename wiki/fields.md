## Metadata Fields
These are the default metadata fields that can be assigned by [strategies](strategies.md).
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
* `art`

These are saved to different places depending on which tags your file has (such as ID3V2 or XIPH).

[`art`](art.md) is special. It should be a path to an image template, relative to the folder specified in [`library.yaml`](library.md) under the `art` key. The image will be processed and embedded into the song's data. If it's a list, the first template found will be embedded.

## Custom Fields

Custom fields can also be created in `library.yaml` under the `custom_fields` key. They can be assigned by strategies, but they won't be saved to song metadata. Instead, you can choose to export the resulting values to a file.

Each entry in the `custom_fields` list has these options:
* `name`: Name for the field.
* `export`: Optional file path to save the results to. Every song in the library that was assigned a value for the field will appear.
* `group`: If set to `item` (the default), the export will show a mapping of every song to its value. If set to `value`, the export will be reversed, grouping songs that share a value together.
* `blanks`: Set to `true` for the export to include songs that weren't assigned a value.
