# Katey.Localization

The language mechanism, with no words of its own. It knows how to look a key up in a table, fill a template by
placeholder name, pick the plural form a language takes, and switch the language every table follows. The
tables and the words are each consuming project's; see the [App](../Katey.App/README.md#words-and-languages)
and [Ui](../Katey.Ui/README.md#words) references. Nothing here references another Katey project.

## File map

| File | Types | Purpose |
| --- | --- | --- |
| `ITextSource.cs` | `ITextSource`, `ResourceTextSource`, `DictionaryTextSource` | Where the words come from: one text per key per culture, with the keys a culture's own table holds. `ResourceTextSource` wraps a `ResourceManager`, so culture fallback is the resource manager's: the culture asked for, its parents, the neutral table. `DictionaryTextSource` is the same over dictionaries, for tests and tools. |
| `Localizer.cs` | `Localizer` | Words by key in the language in force: `Get`, `TryGet`, `Has`, `Format` and `Plural`. A key the source lacks comes back as the key itself. `Decorate` wraps the source, which is how the pseudo-locale is put on. `Write` formats one value with a placeholder's format in the formatting culture. |
| `Template.cs` | `Template`, `TemplateSegment` | Templates with named placeholders, `{name}` or `{name:format}`, and `{{` and `}}` for literal braces. `Parse` caches, `Fill` resolves each placeholder through a callback, `Placeholders` lists the names, `AsWritten` writes a placeholder back. An unbalanced brace or an empty name throws `FormatException`. |
| `PluralRules.cs` | `PluralRules`, `PluralCategory` | Which plural form a whole number takes in a language, by CLDR's integer rules: English, German, Spanish and the neutral culture take `one` for exactly 1; French and Portuguese for 0 and 1; Russian and Ukrainian take `one`, `few` and `many` by the last digits. `CategoriesOf` lists a language's forms, `Suffix` the key suffix of a form, `IsPluralForm` reads a suffixed key back. |
| `Language.cs` | `Language` | The one switch. `Set` changes the UI culture of the process and raises `Changed`; `Current` reads it; `Resolve` picks a culture from a saved name, the machine's culture and the supported list. The formatting culture is never touched. |
| `PseudoTextSource.cs` | `PseudoTextSource` | A pseudo-locale over another source: every text about a third longer, accented and bracketed, placeholders left alone, so clipping and untranslated words show before any translation exists. |
| `TableChecks.cs` | `TableChecks` | The checks a table and its translations must pass, as sentences: `KeyProblems` compares generated constants with the neutral table, or a translation with an expected key list; `TranslationProblems` compares every translation with the neutral table: missing keys, extra keys, placeholder sets, plural forms, templates that do not parse. |

## How a table works

A table is a `.resx` file compiled into its assembly, with one `Strings.<culture>.resx` per language beside the
neutral one. A key names one text. A text with placeholders is a template. A counted text is a group of keys
with a category suffix: `Plural.Jumps.one`, `Plural.Jumps.other`, and for Russian `.few` and `.many` as well;
`Localizer.Plural` picks the form for the count, falls back to `.other`, then to the bare key.

`Language.Set` is called once at start from the saved preference, and again when the player picks a language.
Every `Localizer` reads `Language.Current` on each call, so a lookup made after the switch is already in the
new language; a view re-reads its words when the application tells it to.

Numbers and dates inside a template take the formatting culture, `CultureInfo.CurrentCulture`, which stays the
machine's regional setting. A German interface with US dates is a legitimate configuration, and the player
chose it in Windows.

## Plural rules

| Language | Forms | Rule for whole numbers |
| --- | --- | --- |
| English, German, Spanish, neutral | one, other | one for exactly 1 |
| French, Portuguese | one, other | one for 0 and 1 |
| Russian, Ukrainian | one, few, many, other | one when n mod 10 is 1 and n mod 100 is not 11; few when n mod 10 is 2 to 4 and n mod 100 is not 12 to 14; many otherwise |

Katey counts whole things, so the fractional rules are left out. A language the table does not know follows
the English rule.
