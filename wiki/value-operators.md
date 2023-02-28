### Value Operators
Value operators let you modify a value before it gets ultimately assigned to metadata. A single value can be split into many values, or merged, or distributed around. There are quite a few value operators. Aside from a few string shortcuts, all are objects with a couple keys.

A list of value operators is by itself a valid value operator. Each operation is performed in turn.

**Strings**  
`first` and `last` are shortcuts for index operators with index 0 and -1, respectively. Using a number directly is also a shortcut for an index operator. Each of these use "exit" out-of-bounds mode. To change that, use the full index operator.

---

**Split**  
This splits a single string value into a list of values, using a separator. `split` is the string separator. `when_none` determines what to do if the separator wasn't found anywhere. `ignore` is the default, meaning you'll get a one-entry list. `exit` means the value will be discarded.

For example, if your folder contains multiple artist names, separated by commas, you can use a strategy like this:
```yaml
performer:
  from:
    up: 1
  modify:
    split: ", "
```

---

**Index**  
This allows you to select a particular value from a list of values. `index` is the zero-based index to use. You can use negative values to start from the end. `out_of_bounds` decides what to do if the index falls out of bounds. It defaults to `exit`, meaning the value will be discarded. It can be set to `wrap` to perform modulo on the index, or `clamp`, to clamp the index to the nearest end of the list.

This is most often used in tandem with an earlier `split` operation. For example:
```yaml
performer:
  from: this
  modify:
  - split: " - "
  - index: 0
```

---

**Regex**  
There are two operators that deal with regex, that must be used one after the other. The first uses a [regular expression](https://en.wikipedia.org/wiki/Regular_expression) called `regex`. A value called `fail` determines what to do if the value did not match the expression. It defaults to `exit`, meaning the value is discarded, but can be set to `take_whole`, meaning the value is kept as-is.

Your regular expression should contain groups. The values inside of those groups can then be extracted with another operator. It uses `group` to select the name of the group.

For example, if your songs are named like `wait (C418)`, you can use a strategy like this:
```yaml
source:
  from: this
  modify:
    regex: ^(?<outside>.*?) \((?<in_parens>.*?)\)$
apply:
  performer:
    modify:
      group: outside
  title:
    modify:
      group: in_parens
```

---

**Append**  
This appends a string value to an existing string value.

It's just a [value source](value-sources.md) called `append`. The value obtained is appended to the end of the value being modified. Use `prepend` to append to the beginning.

For example, if your songs are placed in folders named like `Piano Sonata No. 14/Movement 1`, and you want the full title to contain both, you can use a strategy like this:
```yaml
title:
  from: this
  modify:
  - prepend: " ("
  - prepend:
      from:
        up: 1
  - append: ")"
```

---

**Join**  
This combines a list of values into a single value, placing a separator between each one.

It's just a [value source](value-sources.md) called `join`. For example, to embed the full path to a song into its comment field, you can use a strategy like this:
```yaml
comment:
  from:
    from_root:
      start: 1
      stop: -1
  modify:
    join: '/'
```
