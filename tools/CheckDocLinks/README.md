# CheckDocLinks

A console utility that guards the repository's docs against two kinds of rot the guide-plus-reference structure
is prone to. First, **resolution**: every relative Markdown link and heading anchor must resolve, so a moved
file or a renamed `#anchor` fails. Second, **symbol links**: a link whose visible text is a single
backtick-wrapped identifier must land on a page that documents that code element, not merely one that mentions
it. It computes heading anchors with GitHub's slug rules, so the resolution check matches how the links resolve
on the repository host.

## Run

```sh
dotnet run --project tools/CheckDocLinks
```

Run it from anywhere inside the repository; it walks up to the repository root, the directory holding `.git`.
Pass a root directory as the first argument to check a different tree. It is part of the docs gate: run it
with the markdown lint whenever a `README.md`, `MANUAL.md` or `AGENTS.md` changes.

## What it checks

**Resolution.**

- Relative links to files and directories exist: `[text](path)` and `[text](path#anchor)`.
- An `#anchor` fragment resolves to a real heading in the target file, or in the same file for a bare
  `#anchor`, matching GitHub's slugging: lower case, punctuation dropped, spaces to hyphens, duplicate
  headings suffixed `-1`, `-2`.

**Symbol links.** A link whose visible text is a single code span, a backtick-wrapped identifier such as
`Catalog`, names a code element, so it must point at that element's documentation:

- **Dangling symbol.** The element's name appears nowhere in the target file. Usually a rename left the link
  behind, or it points at the wrong file.
- **Wrong section.** The link lands in a solution guide section that is not about the element. A guide is
  recognised by `> Reference:` deferral lines, which hand reference detail to the per-project READMEs. This
  repository's guide carries no such lines, so here only the dangling-symbol check applies.

A link whose visible text is a file name, such as `README.md`, is treated as a file pointer, not a symbol.

**Both.**

- Fenced code blocks are skipped, so a `#` inside a code sample is not mistaken for a heading or a link.
- External links (`http`, `https`, `mailto`, `tel`) and anything under `obj/`, `bin/`, `.git/` or
  `node_modules/` are ignored.

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | all links resolve |
| 1 | broken links found, each listed as `file:line: reason` |
| 2 | the repository root could not be located |
