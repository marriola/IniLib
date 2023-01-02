# iniq

iniq is a simple console driver for IniLib.

## Usage

```sh
iniq -c -n -s section -k key -v value -o path -C commentRule -K duplicateKeyRule -T duplicateSectionRule -E escapeSequenceRule -G globalKeysRule -D nameValueDelimiterRule -P nameValueDelimiterPreferenceRule -S nameValueDelimiterSpacingRule -N newlineRule -Q quotationRule file
```

If `file` is `-`, then iniq reads from stdin.

If only an input file is specified and none of the `-s`, `-k` or `-v` switches are used, iniq lists all sections in the file. If a section is specified, all key-value pairs in the specified section are listed. If a section and key are specified, the value of the key is printed.

If a section, key and value are specified, the key's value is either added to or changed, depending on the settings used, and the configuration is written to the specified output. If no output is specified, writes back to the input file.

The `-s` switch may be omitted when accessing global keys.

## Examples

```sh
iniq /etc/samba/smb.conf                        # Print sections in Samba config

iniq -s Share /etc/samba/smb.conf               # Print keys in Share section

iniq -n -s Share -k path /etc/samba/smb.conf	# Print the path to a Samba share
```

## Command line arguments

| Short switch | Long switch                               | Description                                                                                                                                                                    |
| ------------ | ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| -c           | --stdout                                  | Print output to stdout                                                                                                                                                         |
| -o           | --out path                                | Save output to file at path                                                                                                                                                    |
| -s           | --section sectionName                     | The section to read from or write to                                                                                                                                           |
| -k           | --key keyName                             | The key to read from or write to                                                                                                                                               |
| -v           | --value value                             | The key value to set                                                                                                                                                           |
| -n           | --newline                                 | Print a newline after printing the key value                                                                                                                                   |
| -C           | --commentRule option                      | Set the comment rule. Options are `HashAndSemicolonComments`, `HashComments` and `SemicolonComments`                                                                           |
| -K           | --duplicateKeyRule option                 | Set the duplicate key rule. Options are `DisallowDuplicateKeys`, `DuplicateKeyReplacesValue` and `DuplicateKeyAddsValue`                                                       |
| -T           | --duplicateSectionRule option             | Set the duplicate section rule. Options are `DisallowDuplicateSections`, `AllowDuplicateSections`, `MergeDuplicateSectionIntoOriginal` and `MergeOriginalSectionIntoDuplicate` |
| -E           | --escapeSequenceRule option               | Set the escape sequence rule. Options are `IgnoreEscapeSequences`, `UseEscapeSequences` and `UseEscapeSequencesAndLineContinuation`                                            |
| -G           | --globalKeysRule option                   | Set the global keys rule. Options are `DisallowGlobalKeys` and `AllowGlobalKeys`                                                                                               |
| -D           | --nameValueDelimiterRule option           | Set the name-value delimiter rule. Options are `EqualsDelimiter`, `ColonDelimiter`, `EqualsOrColonDelimiter` and `NoDelimiter`                                                 |
| -P           | --nameValueDelimiterPreferenceRule option | Set the name-value delimiter preference rule. Options are `PreferEqualsDelimiter`, `PreferColonDelimiter` and `PreferNoDelimiter`                                              |
| -S           | --nameValueDelimiterSpacingRule option    | Set the name-value delimiter spacing rule. Options are `BothSides`, `LeftOnly`, `RightOnly` and `NoSpacing`                                                                    |
| -N           | --newlineRule option                      | Set the newline rule. Options are `DefaultEnvironmentNewline`, `LfNewLine` and `CrLfNewline`                                                                                   |
| -Q           | --quotationRule option                    | Set the quotation rule. Options are `IgnoreQuotation` and `UseQuotation`                                                                                                       |
