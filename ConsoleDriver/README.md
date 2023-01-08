# iniq

iniq is a simple console driver for IniLib.

![autocomplete](https://github.com/marriola/IniLib/raw/master/ConsoleDriver/autocomplete.gif)

## Installation

To get autocomplete, install iniq:

```bash
# Copy iniq to ~/.local/bin and add a line to ~/.bashrc to add autocomplete
./install --local

# or

# Copy iniq to /usr/bin and add autocomplete script to /etc/bash_completion.d
./install --global
```

## Usage

```sh
iniq [options] file [section] [key] [value]
```

If `file` is `-`, then iniq reads from stdin.

If an input file and no section is specified, iniq lists all sections in the file. If a section is specified, all key-value pairs in the specified section are listed. If a section and key are specified, the value of the key is printed.

If a section, key and value are specified, the key's value is either added to or changed, depending on the settings used, and the configuration is written to the specified output. If no output is specified, writes back to the input file.

## Examples

```sh
iniq /etc/samba/smb.conf                        # Print sections in Samba config

iniq /etc/samba/smb.conf Share                  # Print keys in Share section

iniq -n /etc/samba/smb.conf Share path          # Print the path to a Samba share
```

## Command line options

| Short switch | Long switch                               | Description                                                                                                                                                                    |
| ------------ | ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| -c           | --stdout                                  | Print output to stdout                                                                                                                                                         |
| -d           | --delete                                  | Delete the specified section or key                                                                                                                                            |
| -y           | --yes                                     | Delete without prompting                                                                                                                                                       |
| -n           | --newline                                 | Print a newline after printing the key value                                                                                                                                   |
| -o           | --out path                                | Save output to file at path                                                                                                                                                    |
| -O           | --outputDelimiter delim                   | Sets the delimiter between printed key values. Default is newline.                                                                                                             |
| -C           | --commentRule option                      | Set the comment rule. Options are `HashAndSemicolonComments`, `HashComments` and `SemicolonComments`                                                                           |
| -K           | --duplicateKeyRule option                 | Set the duplicate key rule. Options are `DisallowDuplicateKeys`, `DuplicateKeyReplacesValue` and `DuplicateKeyAddsValue`                                                       |
| -T           | --duplicateSectionRule option             | Set the duplicate section rule. Options are `DisallowDuplicateSections`, `AllowDuplicateSections`, `MergeDuplicateSectionIntoOriginal` and `MergeOriginalSectionIntoDuplicate` |
| -E           | --escapeSequenceRule option               | Set the escape sequence rule. Options are `IgnoreEscapeSequences`, `UseEscapeSequences` and `UseEscapeSequencesAndLineContinuation`                                            |
| -G           | --globalKeysRule option                   | Set the global keys rule. Options are `DisallowGlobalKeys` and `AllowGlobalKeys`                                                                                               |
| -D           | --nameValueDelimiterRule option           | Set the name-value delimiter rule. Options are `EqualsDelimiter`, `ColonDelimiter`, `EqualsOrColonDelimiter` and `NoDelimiter`                                                 |
| -P           | --nameValueDelimiterPreferenceRule option | Set the name-value delimiter preference rule. Options are `PreferEqualsDelimiter`, `PreferColonDelimiter` and `PreferNoDelimiter`                                              |
| -S           | --nameValueDelimiterSpacingRule option    | Set the name-value delimiter spacing rule. Options are `BothSides`, `LeftOnly`, `RightOnly` and `NoSpacing`                                                                    |
| -N           | --newlineRule option                      | Set the newline rule. Options are `DefaultEnvironmentNewline`, `LfNewLine` and `CrLfNewline`                                                                                   |
| -Q           | --quotationRule option                    | Set the quotation rule. Options are `IgnoreQuotation`, `UseQuotation` and `AlwaysUseQuotation`                                                                                 |
