### Field Setters
A field setter determines how a value should be combined with existing song metadata. The simplest field setter is just a [value source](sources.md), which replaces the existing metadata. However, you can also specify additional rules.

Use `mode` to determine how the new metadata should be combined with previously assigned metadata. The combine mode can be one of the following:
* `replace` (this is the default, it doesn't need to be specified)
* `append`
* `prepend`
* `remove`

When using `remove`, no other information needs to be provided. It results in the existing metadata being removed. For example:
```yaml
title:
  mode: remove
```

For the other options, you must include a value source called `source`. To add C418 to the list of composers, for example:
```yaml
composer:
  mode: append
  source: C418
```

If this field setter is part of a field spec [with context](strategies.md#context), then there's already a value source provided by the strategy itself. In this case, you don't need to specify a `source`.

Either way, you can specify a [value operator](value-operators.md) called `modify` to change the value.
