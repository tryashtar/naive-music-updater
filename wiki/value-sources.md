### Value Sources
A value source is a method for obtaining a value that can be modified, then ultimately embedded into metadata.

The simplest value source is just a literal string or list of strings. For example, `title: Joy to the World` would set the title of all relevant songs to exactly that.

To get information from the file structure of the songs, you have to use an object. It contains three values:

**`from`**  
This is a "local item selector." Its purpose is to select a file or folder, relative to the song being modified, to fetch data from. It can be set to `this` or `self` to select the song file in question.

Another option is using `up`. This allows you to "navigate up" from the song being modified towards the library root. For example, to select the parent folder:
```yaml
from:
  up: 1
```

Instead of a single integer, you can specify a range with `start` and `stop` to select multiple folders. If `stop` is negative, it will target a folder relative to the end of the navigation path.
```yaml
from:
  up:
    start: 1
    stop: 2
```

Another option is using `from_root`. This is like the opposite of `up`; it navigates down, starting from the library root, towards the song in question. For example, to select the folder in the library root that (eventually) contains the current song:
```yaml
from:
  from_root: 1
```

The last option is using a normal [item selector](selectors.md). It's called `selector`.
```yaml
from:
  selector: Volume Alpha/Living Mice
```

---

**`value`**  
This is to decide what kind of data should be acquired from the selected item. The default is [`clean_name`](names.md), but you can use `file_name` to select the original file name.

The other option is `copy`, which lets you choose a metadata field. Note that this will copy the "final" value that ends up in that field. This means if later strategies modify the field you're copying from, this field will end up with those modifications as well. For example:
```yaml
album artist:
  from: this
  value:
    copy: performer
```

---

**`modify`**  
Lastly, you can optionally provide a [value operator](value-operators.md) that will modify the acquired value.
