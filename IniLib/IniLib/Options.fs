namespace IniLib

type Options =
    { commentRule: CommentRule
      duplicateKeyRule: DuplicateKeyRule
      duplicateSectionRule: DuplicateSectionRule
      escapeSequenceRule: EscapeSequenceRule
      globalKeysRule: GlobalKeysRule
      nameValueDelimiterRule: NameValueDelimiterRule
      nameValueDelimiterPreferenceRule: NameValueDelimiterPreferenceRule
      nameValueDelimiterSpacingRule: NameValueDelimiterSpacingRule
      newlineRule: NewlineRule
      quotationRule: QuotationRule }
with
    static member defaultOptions =
        { commentRule = HashAndSemicolonComments
          duplicateKeyRule = DuplicateKeyReplacesValue
          duplicateSectionRule = DisallowDuplicateSections
          escapeSequenceRule = IgnoreEscapeSequences
          globalKeysRule = DisallowGlobalKeys
          nameValueDelimiterRule = EqualsDelimiter
          nameValueDelimiterPreferenceRule = NameValueDelimiterPreferenceRule.DefaultFor EqualsDelimiter
          nameValueDelimiterSpacingRule = NameValueDelimiterSpacingRule.DefaultFor EqualsDelimiter
          newlineRule = DefaultEnvironmentNewline
          quotationRule = IgnoreQuotation }

    member this.WithCommentRule commentRule = { this with commentRule = commentRule }

    member this.WithDuplicateKeyRule duplicateKeyRule = { this with duplicateKeyRule = duplicateKeyRule }
    
    member this.WithDuplicateSectionRule duplicateSectionRule = { this with duplicateSectionRule = duplicateSectionRule }

    member this.WithEscapeSequenceRule escapeSequenceRule = { this with escapeSequenceRule = escapeSequenceRule }

    member this.WithGlobalKeysRule globalKeysRule = { this with globalKeysRule = globalKeysRule }

    member this.WithNameValueDelimiterRule nameValueDelimiterRule =
        { this with nameValueDelimiterRule = nameValueDelimiterRule
                    nameValueDelimiterPreferenceRule = NameValueDelimiterPreferenceRule.DefaultFor nameValueDelimiterRule
                    nameValueDelimiterSpacingRule = NameValueDelimiterSpacingRule.DefaultFor nameValueDelimiterRule }

    member this.WithNameValueDelimiterSpacingRule nameValueDelimiterSpacingRule = { this with nameValueDelimiterSpacingRule = nameValueDelimiterSpacingRule }

    member this.WithNameValueDelimiterPreferenceRule nameValueDelimiterPreferenceRule = { this with nameValueDelimiterPreferenceRule = nameValueDelimiterPreferenceRule }

    member this.WithNewlineRule newlineType = { this with newlineRule = newlineType }

    member this.WithQuotationRule quotationType = { this with quotationRule = quotationType }

    static member withCommentRule commentRule options = { options with commentRule = commentRule }

    static member withDuplicateKeyRule duplicateKeyRule options = { options with duplicateKeyRule = duplicateKeyRule }
    
    static member withDuplicateSectionRule duplicateSectionRule options = { options with duplicateSectionRule = duplicateSectionRule }

    static member withEscapeSequenceRule escapeSequenceRule options = { options with escapeSequenceRule = escapeSequenceRule }

    static member withGlobalKeysRule globalKeysRule options = { options with globalKeysRule = globalKeysRule }

    static member withNameValueDelimiterRule nameValueDelimiterRule options =
        { options with nameValueDelimiterRule = nameValueDelimiterRule
                       nameValueDelimiterPreferenceRule = NameValueDelimiterPreferenceRule.DefaultFor nameValueDelimiterRule
                       nameValueDelimiterSpacingRule = NameValueDelimiterSpacingRule.DefaultFor nameValueDelimiterRule }

    static member withNameValueDelimiterSpacingRule nameValueDelimiterSpacingRule options = { options with nameValueDelimiterSpacingRule = nameValueDelimiterSpacingRule }

    static member withNameValueDelimiterPreferenceRule nameValueDelimiterPreferenceRule options = { options with nameValueDelimiterPreferenceRule = nameValueDelimiterPreferenceRule }

    static member withNewlineRule newlineType options = { options with newlineRule = newlineType }

    static member withQuotationRule quotationType options = { options with quotationRule = quotationType }


and CommentRule =
    | HashAndSemicolonComments
    | HashComments
    | SemicolonComments
and DuplicateKeyRule =
    | DisallowDuplicateKeys
    | DuplicateKeyReplacesValue
    | DuplicateKeyAddsValue
and DuplicateSectionRule =
    | DisallowDuplicateSections
    | AllowDuplicateSections
    | MergeDuplicateSectionIntoOriginal
    | MergeOriginalSectionIntoDuplicate
and EscapeSequenceRule =
    | IgnoreEscapeSequences
    | UseEscapeSequences
    | UseEscapeSequencesAndLineContinuation
and GlobalKeysRule =
    | DisallowGlobalKeys
    | AllowGlobalKeys
and NameValueDelimiterRule =
    | EqualsDelimiter
    | ColonDelimiter
    | EqualsOrColonDelimiter
    | NoDelimiter
and NameValueDelimiterPreferenceRule =
    | PreferEqualsDelimiter
    | PreferColonDelimiter
    | PreferNoDelimiter
with
    static member DefaultFor = function
        | EqualsOrColonDelimiter
        | EqualsDelimiter -> PreferEqualsDelimiter
        | ColonDelimiter -> PreferColonDelimiter
        | NoDelimiter -> PreferNoDelimiter
and NameValueDelimiterSpacingRule =
    | BothSides
    | RightOnly
    | LeftOnly
    | NoSpacing
with
    static member DefaultFor = function
        | EqualsDelimiter -> BothSides
        | ColonDelimiter -> RightOnly
        | EqualsOrColonDelimiter -> BothSides
        | NoDelimiter -> LeftOnly
and NewlineRule =
    | DefaultEnvironmentNewline
    | LfNewline
    | CrLfNewline
with
    member this.toText () =
        match this with
        | DefaultEnvironmentNewline -> System.Environment.NewLine
        | LfNewline -> "\n"
        | CrLfNewline -> "\r\n"
and QuotationRule =
    | IgnoreQuotation
    | UseQuotation
    | AlwaysUseQuotation