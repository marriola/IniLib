# IniLib

IniLib is a library for reading, modifying and writing INI files and similar formats. It's non-destructive and supports a number of options for different dialects. The `Configuration` module is meant to be used in a functional style, but there are wrapper classes in `IniLib.Wrappers` for easier C# interop.

## Structure

Internally, an `IniLib.Configuration` consists of a tuple of the complete syntax tree of an INI file and a map of section names to sections and their associated nodes in the syntax tree. The sections are stored as maps of key names to key values and their associated key nodes. This allows configuration files with different formats to be read, manipulated and written back to text with any pre-existing formatting and comments left intact.

In order to support multivalue keys, key values are stored internally as a list, but there are functions to return only a single value.

## Reading a configuration

Configuration files can be read from text, from stream, stream reader, or by file path.

```fsharp
let config = Configuration.fromFile Options.defaultOptions "settings.ini"

let config = Configuration.fromStream Options.defaultOptions (System.IO.File.Open("settings.ini", System.IO.FileMode.Open))

let config = Configuration.fromStreamReader Options.defaultOptions (new System.IO.StreamReader("settings.ini"))

let config =
    Configuration.fromText
        Options.defaultOptions
        """[Section]
           foo = bar
           baz = quux
        """
```

## Using a configuration

### Reading

| Function                                           | Return type   | Description                                                                                                         |
| -------------------------------------------------- | ------------- | ------------------------------------------------------------------------------------------------------------------- |
| **tryGetMultiValues** *sectionName keyName config* | string list   | Looks up all values added to a key.                                                                                 |
| **tryGet** *sectionName keyName config*            | string option | Looks up the last value added to a key. If not present in the section, returns `None`.                              |
| **tryGetFirst** *sectionName keyName config*       | string option | Looks up the first value added to a key. If not present in the section, returns `None`.                             |
| **tryGetInt** *sectionName keyName config*         | int option    | Looks up the last value added to a key and converts it to an `int`. If not present in the section, returns `None`.  |
| **tryGetFirstInt** *sectionName keyName config*    | int option    | Looks up the first value added to a key and converts it to an `int`. If not present in the section, returns `None`. |
| **tryGetNode** *sectionName keyName config*        | Node option   | Looks up the last key node added with the name specified. If not present in the section, returns `None`.            |
| **tryGetFirstNode** *sectionName keyName config*   | Node option   | Looks up the first key node added with the name specified. If not present in the section, returns `None`.           |
| **get** *sectionName keyName config*               | string        | Looks up the last value added to a key. If not present, throws `KeyNotFoundException`.                              |
| **getFirst** *sectionName keyName config*          | string        | Looks up the first value added to a key. If not present, throws `KeyNotFoundException`.                             |
| **getMultiValues** *sectionName keyName config*    | string list   | Looks up all values added to a key. If not present, throws `KeyNotFoundException`.                                  |
| **getInt** *sectionName keyName config*            | int           | Looks up the last value added to a key and converts it to an `int`. If not present in the section, returns `None`.  |
| **getFirstInt** *sectionName keyName config*       | int           | Looks up the first value added to a key and converts it to an `int`. If not present in the section, returns `None`. |
| **getNode** *sectionName keyName config*           | Node          | Looks up the last key node added with the name specified.  If not present, throws `KeyNotFoundException`.           |
| **getFirstNode** *sectionName keyName config*      | Node          | Looks up the first key node added with the name specified.  If not present, throws `KeyNotFoundException`.          |

### Modifying

| Function                                                  | Return type   | Description                                                                                                                                                                    |
| --------------------------------------------------------- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **add** *options section key value config*                | Configuration | Adds a key with a value to a section. If using the DuplicateKeyAddsValue rule, subsequent calls for the same key add additional keys; otherwise, the original key is replaced. |
| **renameKey** *options section key newKeyName config*     | Configuration | Renames a key.                                                                                                                                                                 |
| **renameSection** *options section newSectionName config* | Configuration | Renames a section.                                                                                                                                                             |
| **removeKey** *options section key config*                | Configuration | Removes a key from a section.                                                                                                                                                  |
| **removeSection** *options section config*                | Configuration | Removes a section.                                                                                                                                                             |

### Creating

| Function                                           | Return type   | Description                                                                               |
| -------------------------------------------------- | ------------- | ----------------------------------------------------------------------------------------- |
| **ofList** *options xs*                            | Configuration | Generates a configuration from a list of tuples of section name, key name, and value.     |
| **ofSeq** *options seq*                            | Configuration | Generates a configuration from a sequence of tuples of section name, key name, and value. |
| **fromFile** *options path*                        | Configuration | Generates a configuration from a file.                                                    |
| **fromText** *options text*                        | Configuration | Generates a configuration from text.                                                      |
| **fromStream** *options stream*                    | Configuration | Generates a configuration from a stream.                                                  |
| **fromStreamReader** *options streamReader*        | Configuration | Generates a configuration from a stream reader.                                           |
| **fromTextReader** *options textReader*            | Configuration | Generates a configuration from a text reader.                                             |

### Saving

| Function                                           | Return type   | Description                                |
| -------------------------------------------------- | ------------- | ------------------------------------------ |
| **toText** *options config*                        | string        | Converts a configuration to text.          |
| **writeToFile** *options path config*              | unit          | Writes a configuration to a file.          |
| **writeToStream** *options stream*                 | unit          | Writes a configuration to a stream.        |
| **writeToStreamWriter** *options streamWriter*     | unit          | Writes a configuration to a stream writer. |
| **writeToTextWriter** *options streamWriter*       | unit          | Writes a configuration to a text writer.   |

### Example

```fsharp
let options = Options.defaultOptions.WithDuplicateKeyRule DuplicateKeyAddsValue

let textIn = "[Section 1]\n\
              key = test\n\
              \n"

let config =
    textIn
    |> Configuration.fromText Options.defaultOptions
    |> Configuration.add options "Section 1" "key" "test value 2"
    |> Configuration.add options "Section 2" "up" "down"
    |> Configuration.add options "Section 2" "beauty" "truth"
    
let keyValue = Configuration.getMultiValues "Section 1" "key" config
printfn "%O\n" keyValue

// Output: [test; test value 2]

let textOut = Configuration.toText (options.WithNameValueDelimiterRule ColonDelimiter) config
printfn "%s" textOut

// Output:

// [Section 1]
// key: test
// key: test value 2
// 
// [Section 2]
// up: down
// beauty: truth
```

```fsharp
let options = Options.defaultOptions.WithDuplicateKeyRule DuplicateKeyAddsValue

let config =
	[ "foo", "bar", "baz"
	  "foo", "bar", "2"
	  "foo", "bar", "3"
	  "Section 2", "test", "key"
	  "Section 3", "quux", "5" ]
	|> Configuration.ofList options
	
printf "%s" (Configuration.toText options config)

// Output:

// [foo]
// bar = baz
// bar = 2
// bar = 3
// 
// [Section 2]
// test = key
// 
// [Section 3]
// quux = 5
```

## C# wrapper classes

Since the functional style used by the `Configuration` module is meant to be conducive to piping changes through F#'s `|>` operator, it is less than convenient to use in C#, so wrapper classes are made available in `IniLib.Wrappers` that provide a more familiar indexing syntax.

```csharp
var options = Options.defaultOptions.WithDuplicateKeyRule(DuplicateKeyRule.DuplicateKeyAddsValue);
var config = new IniLib.Wrappers.ConfigurationWrapper(options);
config["foo"]["bar"] = "test";
config["foo"]["bar"] = "ABC";
config["foo"]["bar"] = "123";

Console.WriteLine(config["foo"]["bar"]);                              // Output: 123
Console.WriteLine(config["foo"].GetInt("bar"));                       // Output: 123
Console.WriteLine(config["foo"].GetFirstValue("bar"));                // Output: test
Console.WriteLine(string.Join(", ", config["foo"].GetValues("bar"))); // Output: test, ABC, 123

config.WriteToFile("settings.ini");


// Generate a configuration from a list of tuples

config = new IniLib.Wrappers.ConfigurationWrapper(new List<Tuple<string, string, string>>
{
	Tuple.Create("foo", "bar", "baz"),
	Tuple.Create("foo", "bar", "2"),
	Tuple.Create("foo", "bar", "3"),
	Tuple.Create("Section 2", "test", "key"),
	Tuple.Create("Section 3", "quux", "5"),
}, options);

Console.Write(conf.ToString());

// Output:

// [foo]
// bar = baz
// bar = 2
// bar = 3
// 
// [Section 2]
// test = key
// 
// [Section 3]
// quux = 5
```

## Options

The default options are in `IniLib.Options.defaultOptions`. Options can be set in either a functional style or a fluent style.

```fsharp
// functional F#
let options =
    Options.defaultOptions
    |> Options.withNewlineRule CrLfNewline
    |> Options.withDuplicateKeyRule DuplicateKeyAddsValue
```

```csharp
// fluent C#
var options = Options.defaultOptions
    .WithNewlineRule(NewlineRule.CrLfNewline)
    .WithDuplicateKeyRule(DuplicateKeyRule.DuplicateKeyAddsValue);
```

### Default options

| Option                           | Default value             | Description                                                                                                                               |
| -------------------------------- | ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| CommentRule                      | HashAndSemicolonComments  | Comments may begin with either a `#` or a `;` token                                                                                       |
| DuplicateKeyRule                 | DuplicateKeyReplacesValue | Allows duplicate keys. Additional keys parsed with the same name replace the old value of the key, but are preserved when output to text. |
| DuplicateSectionRule             | DisallowDuplicateSections | Disallows duplicate sections and throws an exception when the parser encounters one.                                                      |
| EscapeSequenceRule               | IgnoreEscapeSequences     | Escape sequences are ignored and parsed as literal text.                                                                                  |
| GlobalKeysRule                   | DisallowGlobalKeys        | Disallows global keys and throws an exception when a key is encountered outside of a section.                                             |
| NameValueDelimiterRule           | EqualsDelimiter           | Accepts a `=` token to assign a value to a key.                                                                                           |
| NameValueDelimiterPreferenceRule | PreferEqualsDelimiter     | Writes the equal delimiter to text between keys and values.                                                                               |
| NameValueDelimiterSpacingRule    | BothSides                 | A single space is added on either side of the delimiter.                                                                                  |
| NewlineRule                      | DefaultEnvironmentNewline | Writes `System.Environment.NewLine` to text.                                                                                              |
| QuotationRule                    | IgnoreQuotation           | Quotation marks are parsed as literal text and are included in the key value.                                                             |

### CommentRule

`CommentRule` determines which types of comments are legal to parse.

| Rule option              | Description                                         |
| ------------------------ | --------------------------------------------------- |
| HashAndSemicolonComments | Comments may begin with either a `#` or a `;` token |
| HashComments             | Comments may begin with a `#` token                 |
| SemicolonComments        | Comments may begin with a `;` token                 |

### DuplicateKeyRule

`DuplicateKeyRule` controls parser behavior when it encounters duplicate keys. It also controls the behavior of adding values to a key.

| Rule option               | Description                                                                                                                                                             |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| DisallowDuplicateKeys     | Disallows duplicate keys and throws an exception when the parser encounters one. Adding a value to a key replaces the old value.                                        |
| DuplicateKeyReplacesValue | Allows duplicate keys. Additional keys parsed with the same name replace the old value of the key, but are preserved when output to text.                               |
| DuplicateKeyAddsValue     | Allows duplicate keys. Additional keys parsed and values added to the configuration accumulate new values. All keys parsed and added are preserved when output to text. |

### DuplicateSectionRule

`DuplicateSectionRule` controls parser behavior when it encounters duplicate sections.

| Rule option                       | Description                                                                                                                                                                                                                                   |
| --------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| DisallowDuplicateSections         | Disallows duplicate sections and throws an exception when the parser encounters one.                                                                                                                                                          |
| AllowDuplicateSections            | Allows duplicate sections. Additional sections parsed replace the original section in the map, but are preserved when output to text.                                                                                                         |
| MergeDuplicateSectionIntoOriginal | Allows duplicate sections. When a duplicate section is encountered, its keys are appended to the original section. The duplicate is not preserved when output to text.                                                                        |
| MergeOriginalSectionIntoDuplicate | Allows duplicate sections. When a duplicate section is encountered, the original section is removed from its original position and its keys are appended to the duplicate section. The original section is not preserved when output to text. |

### EscapeSequenceRule

`EscapeSequenceRule` controls parser behavior when it encounters escape sequences.

| Rule option                           | Description                                                                                                                                                  |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| IgnoreEscapeSequences                 | Escape sequences are ignored and parsed as literal text.                                                                                                     |
| UseEscapeSequences                    | Accepts the escape sequences `\0`, `\a`, `\b`, `\f`, `\n`, `\r`, `\t`, `\v`, `\"`, `\'`, `\#`, `\:`, `\ ` and `\xHHHH`                                             |
| UseEscapeSequencesAndLineContinuation | Accepts the escape sequences `\0`, `\a`, `\b`, `\f`, `\n`, `\r`, `\t`, `\v`, `\"`, `\'`, `\#`, `\:`, `\ ` and `\xHHHH`, as well as the line continuation token `\` |

### GlobalKeysRule

`GlobalKeysRule` controls parser behavior when keys are encountered outside a section.

| Rule option         | Description                                                                                           |
| ------------------- | ----------------------------------------------------------------------------------------------------- |
| DisallowGlobalKeys  | Disallows global keys and throws an exception when a key is encountered outside of a section.         |
| AllowGlobalKeys     | Allows keys to occur outside of a section. They can be accessed through the section named `<global>`. |

### NameValueDelimiterRule

`NameValueDelimiterRule` determines which assignment tokens are legal to parse.

| Rule option            | Description                                            |
| ---------------------- | ------------------------------------------------------ |
| EqualsDelimiter        | Accepts a `=` token to assign a value to a key.        |
| ColonDelimiter         | Accepts a `:` token to assign a value to a key.        |
| EqualsOrColonDelimiter | Accepts a `=` or `:` token to assign a value to a key. |
| NoDelimiter            | Keys and values are separated only by whitespace.      |

Additionally, when using one of the `Options.with...` functions to change this option, `NameValueDelimiterPreferenceRule` and `NameValueDelimiterSpacingRule` are changed to certain defaults at the same time.

| NameValueDelimiterRule | Sets NameValueDelimiterPreferenceRule to | Sets NameValueDelimiterSpacingRule to |
| ---------------------- | ---------------------------------------- | ------------------------------------- |
| EqualsOrColonDelimiter | PreferEqualsDelimiter                    | BothSides                             |
| EqualsDelimiter        | PreferEqualsDelimiter                    | BothSides                             |
| ColonDelimiter         | PreferColonDelimiter                     | RightOnly                             |
| NoDelimiter            | PreferNoDelimiter                        | LeftOnly                              |

### NameValueDelimiterPreferenceRule

`NameValueDelimiterPreferenceRule` determines which assignment token will be written when the configuration is converted to text. Any assignment token which is different from the preferred token is replaced.

| Rule option           | Description                                                        |
| --------------------- | ------------------------------------------------------------------ |
| PreferEqualsDelimiter | Writes the equal delimiter to text.                                |
| PreferColonDelimiter  | Writes the colon delimiter to text.                                |
| PreferNoDelimiter     | Writes no delimiter except a single space between keys and values. |

### NameValueDelimiterSpacingRule

`NameValueDelimiterSpacingRule` determines what spacing is written around the assignment token when keys are added to the configuration, or when replacing mismatching assignment tokens before converting to text. If an assignment token in a configuration is the same as the preferred token determined by `NameValueDelimiterPreferenceRule`, it and its surrounding whitespace are preserved when converted to text.

| Rule option | Description                                                      |
| ----------- | ---------------------------------------------------------------- |
| BothSides   | A single space is added on either side of the delimiter.         |
| LeftOnly    | A single space is added on the left side of the delimiter only.  |
| RightOnly   | A single space is added on the right side of the delimiter only. |
| NoSpacing   | No space is inserted on either side of the delimiter.            |

### NewlineRule

`NewlineRule` determines which type of newline is written when the configuration is converted to text.

| Rule option               | Description                                  |
| ------------------------- | -------------------------------------------- |
| DefaultEnvironmentNewline | Writes `System.Environment.NewLine` to text. |
| LfNewLine                 | Writes `\n` to text.                         |
| CrLfNewline               | Writes `\r\n` to text.                       |

### QuotationRule

`QuotationRule` controls how quotes are parsed, and how key values with leading and trailing whitespace are converted to text.

| Rule option        | Description                                                                                                                                                                                                                                                                     |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| IgnoreQuotation    | Quotation marks are parsed as literal text and are included in the key value.                                                                                                                                                                                                   |
| UseQuotation       | Quotation marks are treated as their own type of token and are not included in the key value. Any leading or trailing whitespace is included in the key value. Additionally, quotation marks are automatically added when adding leading or trailing whitespace to a key value. |
| AlwaysUseQuotation | Like UseQuotation, but always writes quotation marks to text, whether the value has leading or trailing whitespace or not.                                                                                                                                                      |
