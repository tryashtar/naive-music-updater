## Automatic Config
If your songs already have metadata embedded, you can actually generate a config file from that! Just make the `config.yaml` look like one of these:
```yaml
reverse: full
# or
reverse: minimal
```
The config file will automatically be replaced with a full one that sets all of the metadata in your songs to what it already is. If you use `minimal`, it will only include metadata that wouldn't have already been set to the correct value by an earlier config. If you use `full`, it will include everything no matter what. Either way, the songs should end up untouched, with any changes caused by earlier configs overwritten by the newly created config.

This is a handy way to see how config files can look, although all [item selectors](selectors.md) and [strategies](strategies.md) generated will be simple and "hardcoded." It's also useful for preserving information that would otherwise be overwritten by broader configs.
