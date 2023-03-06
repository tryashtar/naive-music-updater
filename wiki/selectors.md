## Item Selectors
An item selector lets you pick one or more songs that will be affected by something, usually a [strategy](strategies.md). The simplest item selector is just a string path, relative to the containing folder, that matches a song.

For example, if there's a song in `C418/Volume Beta/Alpha.mp3`, then you can select it with just `Alpha` if you're in the Volume Beta folder. If you're in the C418 folder, you can select it with `Volume Beta/Alpha`.

A list of item selectors is by itself a valid item selector. Each selector is evaluated in turn, producing many selected songs.

You can also write item selectors as an object to get special behavior. 

---

**Predicate Path**  
Allows you to define a list of each subfolder that must be navigated in turn to reach the song, called `path`. Each list entry can either be a string, or an object with a `regex` value, which will navigate any item matching the [regular expression](https://en.wikipedia.org/wiki/Regular_expression).

For example, this selector selects all songs beginning with "C" across multiple folders:
```yaml
path:
- C418
- regex: ^Volume .*$
- regex: ^C
```

---

**Subpath**  
Allows you to change the folder a selector starts from. The new start folder is an item selector called `subpath`, and the selector to use is called `select`.

For example, if you're in the C418 folder and want to select many songs in the Volume Alpha folder, you would have to write `Volume Alpha/` in front of every song. You can use `subpath` to avoid this:
```yaml
subpath: Volume Alpha
select:
- Beginning
- Cat
- Chris
```

---

As a bonus, when the program finishes running, it will print out any item selectors that didn't find any songs. This is a sign that you made a typo, or something needs to be updated.
