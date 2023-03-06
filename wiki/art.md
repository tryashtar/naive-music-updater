## Art
This program has functionality for embedding album art into song files, and setting folder icons.

It's pretty simple: put some images in a folder, then reference their paths in [configs](config.md) with the `art` [field](fields.md).

For example, to make a song look for an image that matches its path, use this [strategy](strategies.md):
```yaml
art:
   from: this
   value: path
```

## Processing

These images can also be processed using some simple settings. Use `images.yaml` files to configure this. These files have these fields:
* `all`: Settings to apply to all images in this folder and deeper.
* `set`: This allows you to apply settings to specific images. Each key is a path to an image, relative to this folder. Each value is a settings object.
* `set all`: A list of objects, with `names` as a list of image paths, and `set` as a settings object to apply to all of them.

The actual settings objects have these values. All are optional. Settings from deeper configs will override those of earlier configs.
* `width`: Desired width to resize the image to.
* `height`: Desired height to resize the image to. If either it or width is unspecified, the other will be determined automatically.
* `buffer`: Either `false` for no buffer, or a list of 4 pixel values in order [left, up, right, down]. If specified, the image will be cropped of outer transparency, then transparency will be re-added on each respective side.
* `background`: Color to apply to the background, as 4 values in order [red, green, blue, alpha] from 0-255.
* `interpolation`: Interpolation mode for resizing. Either `bicubic` or `nearest_neighbor`.
* `scale`: Mode for resizing. `stretch` stretches the image to fill the size, altering the aspect ratio. `max` scales the image to fit the size, then removes any excess, preserving the aspect ratio. `pad` is the same but without removing the excess.
* `integer_scale`: If set to `true`, the image will only be scaled in integer multiples of its original size. Useful alongside `nearest_neighbor` for pixel art.

## Library Configuration

The art folder and other settings are configured in [`library.yaml`](library.md), under the `art` key:
* `templates`: Folder path where images are stored.
* `cache`: Optional folder path to save templates after processing. This will take up more space, but will make the program faster on subsequent runs.
* `icons`: Optional folder path to save ICO files. If present, folders with `art` metadata will change to show their icon.
* `named_settings`: Every entry in this has a name for a key, and an art settings object as a value. The names can be used in the `images.yaml` files instead of an object as a shortcut to reference these.
