using IniLib;
using IniLib.Wrappers;
using System.Reflection;
using Utility.CommandLine;
using static IniLib.Node;

internal class Program
{
    [Argument('h', "help", "Show this help text")]
    private static bool ShowHelp { get; set; }

    [Argument('c', "stdout", "Output to stdout")]
    private static bool ToStdout { get; set; }

    [Argument('o', "out", "The output file path")]
    private static string OutPath { get; set; }

    [Argument('s', "section", "The section to read or write")]
    private static string SectionName { get; set; }

    [Argument('k', "key", "The key to read or write")]
    private static string KeyName { get; set; }

    [Argument('v', "value", "The value to write")]
    private static string Value { get; set; }

    [Argument('n', "newline", "Print newline after value")]
    private static bool ShowNewline { get; set; }

    [Argument('d', "delete", "Delete key or section specified")]
    private static bool Delete { get; set; }

    [Argument('y', "yes", "Do not prompt confirmation to delete")]
    private static bool Yes { get; set; }

    [Argument('C', "commentRule", "Comment rule")]
    private static string CommentRuleText { get; set; }

    [Argument('K', "duplicateKeyRule", "Duplicate key rule")]
    private static string DuplicateKeyRuleText { get; set; }

    [Argument('T', "duplicateSectionRule", "Duplicate section rule")]
    private static string DuplicateSectionRuleText { get; set; }

    [Argument('E', "escapeSequenceRule", "Escape Sequence rule")]
    private static string EscapeSequenceRuleText { get; set; }

    [Argument('G', "globalKeysRule", "Global keys rule")]
    private static string GlobalKeysRuleText { get; set; }

    [Argument('D', "nameValueDelimiterRule", "Name value delimiter rule")]
    private static string NameValueDelimiterRuleText { get; set; }

    [Argument('P', "nameValueDelimiterPreferenceRule", "Name value delimiter preference rule")]
    private static string NameValueDelimiterPreferenceRuleText { get; set; }

    [Argument('S', "nameValueDelimiterSpacingRule", "Name value delimiter spacing rule")]
    private static string NameValueDelimiterSpacingRuleText { get; set; }

    [Argument('N', "newlineRule", "Newline rule")]
    private static string NewlineRuleText { get; set; }

    [Argument('Q', "quotationRule", "Quotation rule")]
    private static string QuotationRuleText { get; set; }

    [Operands]
    private static List<string> Operands { get; set; }

    private static string InputFile => Operands?.Count < 2 ? null : Operands[1];

    private static Dictionary<string, string> SwitchToOptionDescription = new()
    {
        ["-o"] = "output path",
        ["-s"] = "section name",
        ["-k"] = "key name",
        ["-v"] = "set value",
        ["-C"] = "comment rule",
        ["-K"] = "duplicate key rule",
        ["-T"] = "duplicate section rule",
        ["-E"] = "escape sequence rule",
        ["-G"] = "global keys rule",
        ["-D"] = "name-value delimiter rule",
        ["-P"] = "name-value delimiter preference rule",
        ["-S"] = "name-value delimiter spacing rule",
        ["-N"] = "newline rule",
        ["-Q"] = "quotation rule"
    };

    private static Dictionary<char, List<string>> SwitchToRuleOptions = new()
    {
        ['C'] = new() { "HashAndSemicolonComments", "HashComments", "SemicolonComments" },
        ['K'] = new() { "DisallowDuplicateKeys", "DuplicateKeyReplacesValue", "DuplicateKeyAddsValue" },
        ['T'] = new() { "DisallowDuplicateSections", "AllowDuplicateSections", "MergeDuplicateSectionIntoOriginal", "MergeOriginalSectionIntoDuplicate" },
        ['E'] = new() { "IgnoreEscapeSequences", "UseEscapeSequences", "UseEscapeSequencesAndLineContinuation" },
        ['G'] = new() { "DisallowGlobalKeys", "AllowGlobalKeys" },
        ['D'] = new() { "EqualsDelimiter", "ColonDelimiter", "EqualsOrColonDelimiter", "NoDelimiter" },
        ['P'] = new() { "PreferEqualsDelimiter", "PreferColonDelimiter", "PreferNoDelimiter" },
        ['S'] = new() { "BothSides", "LeftOnly", "RightOnly", "NoSpacing" },
        ['N'] = new() { "DefaultEnvironmentNewline", "LfNewLine", "CrLfNewline" },
        ['Q'] = new() { "IgnoreQuotation", "UseQuotation", "AlwaysUseQuotation" }
    };

    private static Dictionary<string, CommentRule> TextToCommentRule = new()
    {
        ["<default>"] = CommentRule.HashAndSemicolonComments,
        ["HashAndSemicolonComments"] = CommentRule.HashAndSemicolonComments,
        ["HashComments"] = CommentRule.HashComments,
        ["SemicolonComments"] = CommentRule.SemicolonComments
    };

    private static Dictionary<string, DuplicateKeyRule> TextToDuplicateKeyRule = new()
    {   
        ["<default>"] = DuplicateKeyRule.DisallowDuplicateKeys,
        ["DisallowDuplicateKeys"] = DuplicateKeyRule.DisallowDuplicateKeys,
        ["DuplicateKeyReplacesValue"] = DuplicateKeyRule.DuplicateKeyReplacesValue,
        ["DuplicateKeyAddsValue"] = DuplicateKeyRule.DuplicateKeyAddsValue
    };

    private static Dictionary<string, DuplicateSectionRule> TextToDuplicateSectionRule = new()
    {
        ["<default>"] = DuplicateSectionRule.DisallowDuplicateSections,
        ["DisallowDuplicateSections"] = DuplicateSectionRule.DisallowDuplicateSections,
        ["AllowDuplicateSections"] = DuplicateSectionRule.AllowDuplicateSections,
        ["MergeDuplicateSectionIntoOriginal"] = DuplicateSectionRule.MergeDuplicateSectionIntoOriginal,
        ["MergeOriginalSectionIntoDuplicate"] = DuplicateSectionRule.MergeOriginalSectionIntoDuplicate
    };

    private static Dictionary<string, EscapeSequenceRule> TextToEscapeSequenceRule = new()
    {
        ["<default>"] = EscapeSequenceRule.IgnoreEscapeSequences,
        ["IgnoreEscapeSequences"] = EscapeSequenceRule.IgnoreEscapeSequences,
        ["UseEscapeSequences"] = EscapeSequenceRule.UseEscapeSequences,
        ["UseEscapeSequencesAndLineContinuation"] = EscapeSequenceRule.UseEscapeSequencesAndLineContinuation
    };

    private static Dictionary<string, GlobalKeysRule> TextToGlobalKeysRule = new()
    {
        ["<default>"] = GlobalKeysRule.DisallowGlobalKeys,
        ["DisallowGlobalKeys"] = GlobalKeysRule.DisallowGlobalKeys,
        ["AllowGlobalKeys"] = GlobalKeysRule.AllowGlobalKeys
    };

    private static Dictionary<string, NameValueDelimiterRule> TextToNameValueDelimiterRule = new()
    {
        ["<default>"] = NameValueDelimiterRule.EqualsDelimiter,
        ["EqualsDelimiter"] = NameValueDelimiterRule.EqualsDelimiter,
        ["ColonDelimiter"] = NameValueDelimiterRule.ColonDelimiter,
        ["EqualsOrColonDelimiter"] = NameValueDelimiterRule.EqualsOrColonDelimiter,
        ["NoDelimiter"] = NameValueDelimiterRule.NoDelimiter
    };

    private static Dictionary<string, NameValueDelimiterPreferenceRule> TextToNameValueDelimiterPreferenceRule = new()
    {
        ["<default>"] = NameValueDelimiterPreferenceRule.PreferEqualsDelimiter,
        ["PreferEqualsDelimiter"] = NameValueDelimiterPreferenceRule.PreferEqualsDelimiter,
        ["PreferColonDelimiter"] = NameValueDelimiterPreferenceRule.PreferColonDelimiter,
        ["PreferNoDelimiter"] = NameValueDelimiterPreferenceRule.PreferNoDelimiter
    };

    private static Dictionary<string, NameValueDelimiterSpacingRule> TextToNameValueDelimiterSpacingRule = new()
    {
        ["<default>"] = NameValueDelimiterSpacingRule.BothSides,
        ["BothSides"] = NameValueDelimiterSpacingRule.BothSides,
        ["LeftOnly"] = NameValueDelimiterSpacingRule.LeftOnly,
        ["RightOnly"] = NameValueDelimiterSpacingRule.RightOnly,
        ["NoSpacing"] = NameValueDelimiterSpacingRule.NoSpacing
    };

    private static Dictionary<string, NewlineRule> TextToNewlineRule = new()
    {
        ["<default>"] = NewlineRule.DefaultEnvironmentNewline,
        ["DefaultEnvironmentNewline"] = NewlineRule.DefaultEnvironmentNewline,
        ["LfNewLine"] = NewlineRule.LfNewline,
        ["CrLfNewline"] = NewlineRule.CrLfNewline
    };

    private static Dictionary<string, QuotationRule> TextToQuotationRule = new()
    {
        ["<default>"] = QuotationRule.IgnoreQuotation,
        ["IgnoreQuotation"] = QuotationRule.IgnoreQuotation,
        ["UseQuotation"] = QuotationRule.UseQuotation,
        ["AlwaysUseQuotation"] = QuotationRule.AlwaysUseQuotation
    };

    private static CommentRule CommentRule => TextToCommentRule[CommentRuleText ?? "<default>"];
    
    private static DuplicateKeyRule DuplicateKeyRule => TextToDuplicateKeyRule[DuplicateKeyRuleText ?? "<default>"];
    
    private static DuplicateSectionRule DuplicateSectionRule => TextToDuplicateSectionRule[DuplicateSectionRuleText ?? "<default>"];
    
    private static EscapeSequenceRule EscapeSequenceRule => TextToEscapeSequenceRule[EscapeSequenceRuleText ?? "<default>"];
    
    private static GlobalKeysRule GlobalKeysRule => TextToGlobalKeysRule[GlobalKeysRuleText ?? "<default>"];
    
    private static NameValueDelimiterRule NameValueDelimiterRule => TextToNameValueDelimiterRule[NameValueDelimiterRuleText ?? "<default>"];
    
    private static NameValueDelimiterPreferenceRule NameValueDelimiterPreferenceRule => TextToNameValueDelimiterPreferenceRule[NameValueDelimiterPreferenceRuleText ?? "<default>"];
    
    private static NameValueDelimiterSpacingRule NameValueDelimiterSpacingRule => TextToNameValueDelimiterSpacingRule[NameValueDelimiterSpacingRuleText ?? "<default>"];
    
    private static NewlineRule NewlineRule => TextToNewlineRule[NewlineRuleText ?? "<default>"];
    
    private static QuotationRule QuotationRule => TextToQuotationRule[QuotationRuleText ?? "<default>"];

    private static Options Options
    {
        get
        {
            var options = Options.defaultOptions;
            options = options.WithCommentRule(CommentRule);
            options = options.WithDuplicateKeyRule(DuplicateKeyRule);
            options = options.WithDuplicateSectionRule(DuplicateSectionRule);
            options = options.WithEscapeSequenceRule(EscapeSequenceRule);
            options = options.WithGlobalKeysRule(GlobalKeysRule);
            options = options.WithNameValueDelimiterRule(NameValueDelimiterRule);
            if (NameValueDelimiterPreferenceRuleText != null)
            {
                options = options.WithNameValueDelimiterPreferenceRule(NameValueDelimiterPreferenceRule);
            }
            if (NameValueDelimiterSpacingRuleText != null)
            {
                options = options.WithNameValueDelimiterSpacingRule(NameValueDelimiterSpacingRule);
            }
            options = options.WithNewlineRule(NewlineRule);
            options = options.WithQuotationRule(QuotationRule);

            return options;
        }
    }

    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage(args);
            return;
        }

        var oldArgs = new string[args.Length];
        args.CopyTo(oldArgs, 0);
        Arguments.Populate();

        if (ShowHelp || Operands == null || Operands.Count < 2)
        {
            ShowUsage(oldArgs);
            return;
        }

        var config = InputFile switch
        {
            "-" => FromConsole(),
            string path => ConfigurationWrapper.FromFile(path, Options)
        };

        if (!string.IsNullOrWhiteSpace(Value))
        {
            SetValue(config);
        }
        else if (!string.IsNullOrWhiteSpace(KeyName))
        {
            if (Delete)
            {
                DeleteKey(config);
            }
            else
            {
                GetValue(config);
            }
        }
        else if (!string.IsNullOrWhiteSpace(SectionName))
        {
            if (Delete)
            {
                DeleteSection(config);
            }
            else
            {
                GetSection(config);
            }
        }
        else if (InputFile == "-")
        {
            OutputConfiguration(config);
        }
        else
        {
            PrintSections(config);
        }
    }

    private static ConfigurationWrapper FromConsole()
    {
        var content = Console.In.ReadToEnd();

        if (!content.Contains("\n"))
        {
            content = content.Replace("\r", "\n");
        }

        return ConfigurationWrapper.FromText(content, Options);
    }

    private static void OutputConfiguration(ConfigurationWrapper config)
    {
        if (!string.IsNullOrEmpty(OutPath))
        {
            config.WriteToFile(OutPath, Options);
        }
        else if (ToStdout || InputFile == "-")
        {
            Console.Write(config.ToString());
        }
        else
        {
            // Write back to input file if no other output specified
            config.WriteToFile(InputFile, Options);
        }
    }

    private static void PrintSections(ConfigurationWrapper config)
    {
        foreach (var section in config.GetSections())
        {
            Console.WriteLine(section);
        }
    }

    private static void DeleteKey(ConfigurationWrapper config)
    {
        var sectionName = SectionName ?? "<global>";

        if (!Yes)
        {
            Console.Write($"Delete key '{KeyName}' from section '{sectionName}'? ");

            if (!Console.ReadLine().Trim().ToUpper().StartsWith("Y"))
            {
                return;
            }
        }

        config[sectionName].Remove(KeyName);
        OutputConfiguration(config);
    }

    private static void DeleteSection(ConfigurationWrapper config)
    {
        var sectionName = SectionName ?? "<global>";

        if (!Yes)
        {
            Console.Write($"Delete section '{sectionName}'? ");

            if (!Console.ReadLine().Trim().ToUpper().StartsWith("Y"))
            {
                return;
            }
        }

        config.RemoveSection(sectionName);
        OutputConfiguration(config);
    }

    private static void GetSection(ConfigurationWrapper config)
    {
        if (string.IsNullOrWhiteSpace(SectionName))
        {
            throw new KeyNotFoundException(nameof(SectionName));
        }

        var section = config.TryGetSectionNode(SectionName);

        if (section == null)
        {
            return;
        }

        var keys = section.Item2
            .OfType<SectionNode>()
            .SelectMany(n => n.children.OfType<KeyNode>());

        foreach (var node in keys)
        {
            Console.WriteLine($"{node.name}={node.value}");
        }
    }

    private static void GetValue(ConfigurationWrapper config)
    {
        if (string.IsNullOrWhiteSpace(KeyName))
        {
            throw new KeyNotFoundException(nameof(KeyName));
        }
        else if (Options.globalKeysRule == GlobalKeysRule.DisallowGlobalKeys && string.IsNullOrWhiteSpace(SectionName))
        {
            throw new KeyNotFoundException(nameof(SectionName));
        }

        var sectionName = SectionName ?? "<global>";
        var result = config.TryGetSectionNode(sectionName);
        if (result?.Item1?.TryGetValue(KeyName, out string value) == true)
        {
            Console.Write(string.Join(", ", config[sectionName][KeyName]));
            if (ShowNewline)
            {
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"Key '{KeyName}' not found in section '{sectionName}'");
        }
    }

    private static void SetValue(ConfigurationWrapper config)
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            throw new KeyNotFoundException(nameof(Value));
        }
        else if (string.IsNullOrWhiteSpace(KeyName))
        {
            throw new KeyNotFoundException(nameof(KeyName));
        }
        else if (Options.globalKeysRule == GlobalKeysRule.DisallowGlobalKeys && string.IsNullOrWhiteSpace(SectionName))
        {
            throw new KeyNotFoundException(nameof(SectionName));
        }

        config[SectionName ?? "<global>"][KeyName] = Value;
        OutputConfiguration(config);
    }

    private static void ShowUsage(string[] args)
    {
        var path = System.Reflection.Assembly.GetAssembly(typeof(Program)).Location;
        var binaryName = Path.GetFileNameWithoutExtension(path);
        var helpAttributes = Arguments.GetArgumentInfo(typeof(Program));
        var optionDescriptions = string.Join(" ", SwitchToOptionDescription.Select(kvp => $"{kvp.Key} [{kvp.Value}]"));

        Console.WriteLine($"usage: {binaryName} -c {optionDescriptions} file\n");

        foreach (var item in helpAttributes)
        {
            var switches = $"-{item.ShortName}, --{item.LongName}";
            var options = SwitchToRuleOptions.ContainsKey(item.ShortName) ? SwitchToRuleOptions[item.ShortName] : new List<string>();
            var result = $"{switches,-35}\t{item.HelpText}";
            Console.WriteLine(result);

            foreach (var option in options)
            {
                Console.WriteLine(new String(' ', 43) + "- " + option);
            }
        }
    }
}
