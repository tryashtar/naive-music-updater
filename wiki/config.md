## Configuration
All songs start with empty metadata. This means the program will not make any changes to the song. Metadata is applied according to rules in `config.yaml` files. These files apply to any song they share a folder with, including subfolders.

For example, if you have a `config.yaml` file in the root of your library, those rules apply to every song in your entire library. If you have a `config.yaml` file in the C418 folder, those rules only apply to songs in that folder, and can override the root library rules.

Here are the options that can be included in a `config.yaml` file. Anytime a [strategy](strategies.md) is mentioned, you can also use the name of a strategy defined in [`library.yaml`](library.md)'s `named_strategies` option.

**`songs`**  
The value for this is a strategy. The strategy applies to all songs in this and deeper folders unconditionally.

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
This allows you to apply strategies to specific songs. Each key is an [item selector](selectors.md), and each value is a strategy.

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

**`set fields`**  
This is also like `set`, but for when you need to set the same [field](fields.md) to different values for a bunch of items. It's a list of objects, with `field` as the field, `set` as a dictionary with item selectors as keys and [value sources](value-sources.md) as values, and an optional combine `mode`.

For example:
```yaml
set fields:
- field: comment
  mode: append
  set:
    Tripping Upstairs: nautical
    Ramblin Man from Gramblin: action
    Phantom Train: spooky
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
