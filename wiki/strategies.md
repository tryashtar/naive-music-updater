### Strategies
A strategy decides how to assign metadata to the [metadata fields](fields.md) of a song.

The simplest form of a strategy is a context-free "field spec." There are two different types of field specs:

**Mapping Field Spec**  
This consists of a map of fields, and a matching [field setter](field-setters.md).

Here's an example of a simple strategy, using a mapping field spec:
```yaml
title: Joy to the World
composer: Isaac Watts
```

---

**Multiple Field Spec**  
This type allows you to set multiple fields to the same value. The fields are listed in `fields`, and the field setter to use for all of them is called `set`. You can also use `*` to assign the value to all fields.

Here's an example of this kind of field spec:
```yaml
fields: [performer, composer, album artist]
set: Isaac Watts
```

---

**Context Strategies**  
Each of those types of field specs works as a strategy on its own. However, you can also provide "context" to a strategy. This allows the field setters to modify a common value.

To do this, put the field spec under a key called `apply`, and provide a [value source](value-sources.md) called `source`. Doing this will allow the field setters in the spec to reference and modify the provided value.

For example, this splits the clean name of the file in half, and assigns each half to different fields:
```yaml
source:
  from: this
  modify:
    split: " - "
apply:
  album artist:
    modify:
      index: 0
  album:
    modify:
      index: 1
```

---

A list of strategies is by itself a valid strategy. Each strategy is applied in turn. A typical song will have many strategies applied to it; later metadata assignments will override earlier ones.
