## Strategies
A strategy decides how to assign metadata to the [metadata fields](fields.md) of a song.

The simplest form of a strategy is "map strategy." It simply assigns a value to each specified field. Other strategies allow for additional behavior.

---

**Map Strategy**  
This is just an object with fields as keys, and [value sources](value-sources.md) as values.

For example:
```yaml
title: Joy to the World
composer: Isaac Watts
```

---

**Mode Strategy**  
A map strategy replaces existing metadata with the specified values. However, you can instead choose to `append` or `prepend` to an existing list. Just use `mode`, then put the fields under `values`.

For example:
```yaml
mode: append
values:
  performer: Tommy Tallarico
```

---

**Remove Strategy**  
This type allows you to delete metadata. If a song is saved with metadata removed with this strategy, the tags will be cleared. It's simply a list called `remove` of fields to remove, or `*` to remove all fields

For example:
```yaml
remove: [performer, composer, album artist]
```

---

**Shared Strategy**  
A shared strategy takes one value and assigns it to multiple fields. Each field has a chance to modify the value for itself.

To do this, put a value source called `source`, then a dictionary called `modify` with fields as keys and [value operator](value-operators.md) as values.

For example, this splits the clean name of the file in half, and assigns each half to different fields:
```yaml
source:
  from: this
  modify:
    split: " - "
modify:
  album artist:
    index: 0
  album:
    index: 1
```

---

**Modify Strategy**  
Leave off the `source`, and a shared strategy becomes a modify strategy. It simply uses a value operator to modify the existing values of the fields.

For example:
```yaml
modify:
  title:
    append: ' (Deluxe Edition)'
```

---

A list of strategies is by itself a valid strategy. Each strategy is applied in turn. A typical song will have many strategies applied to it; later metadata assignments will override earlier ones.
