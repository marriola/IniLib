namespace IniLib

type Options =
    { commentRule: CommentRule
      duplicateKeyRule: DuplicateKeyRule
      duplicateSectionRule: DuplicateSectionRule
      escapeSequenceRule: EscapeSequenceRule
      globalPropertiesRule: GlobalPropertiesRule
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
          globalPropertiesRule = DisallowGlobalProperties
          nameValueDelimiterRule = EqualsDelimiter
          nameValueDelimiterPreferenceRule = NameValueDelimiterPreferenceRule.DefaultPreferenceRule EqualsDelimiter
          nameValueDelimiterSpacingRule = NameValueDelimiterSpacingRule.DefaultSpacingRule EqualsDelimiter
          newlineRule = DefaultEnvironmentNewline
          quotationRule = IgnoreQuotation }

    member this.WithCommentRule commentRule = { this with commentRule = commentRule }

    member this.WithDuplicateKeyRule duplicateKeyRule = { this with duplicateKeyRule = duplicateKeyRule }
    
    member this.WithDuplicateSectionRule duplicateSectionRule = { this with duplicateSectionRule = duplicateSectionRule }

    member this.WithEscapeSequenceRule escapeSequenceRule = { this with escapeSequenceRule = escapeSequenceRule }

    member this.WithGlobalPropertiesRule globalPropertiesRule = { this with globalPropertiesRule = globalPropertiesRule }

    member this.WithNameValueDelimiterRule nameValueDelimiterRule =
        { this with nameValueDelimiterRule = nameValueDelimiterRule
                    nameValueDelimiterPreferenceRule = NameValueDelimiterPreferenceRule.DefaultPreferenceRule nameValueDelimiterRule
                    nameValueDelimiterSpacingRule = NameValueDelimiterSpacingRule.DefaultSpacingRule nameValueDelimiterRule }

    member this.WithNewlineRule newlineType = { this with newlineRule = newlineType }

    member this.WithQuotationRule quotationType = { this with quotationRule = quotationType }

    static member withCommentRule commentRule options = { options with commentRule = commentRule }

    static member withDuplicateKeyRule duplicateKeyRule options = { options with duplicateKeyRule = duplicateKeyRule }
    
    static member withDuplicateSectionRule duplicateSectionRule options = { options with duplicateSectionRule = duplicateSectionRule }

    static member withEscapeSequenceRule escapeSequenceRule options = { options with escapeSequenceRule = escapeSequenceRule }

    static member withGlobalPropertiesRule globalPropertiesRule options = { options with globalPropertiesRule = globalPropertiesRule }

    static member withNameValueDelimiterRule nameValueDelimiterRule options =
        { options with nameValueDelimiterRule = nameValueDelimiterRule
                       nameValueDelimiterPreferenceRule = NameValueDelimiterPreferenceRule.DefaultPreferenceRule nameValueDelimiterRule
                       nameValueDelimiterSpacingRule = NameValueDelimiterSpacingRule.DefaultSpacingRule nameValueDelimiterRule }

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
and GlobalPropertiesRule =
    | DisallowGlobalProperties
    | AllowGlobalProperties
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
    static member DefaultPreferenceRule = function
        | EqualsOrColonDelimiter
        | EqualsDelimiter -> PreferEqualsDelimiter
        | ColonDelimiter -> PreferColonDelimiter
        | NoDelimiter -> PreferNoDelimiter
and NameValueDelimiterSpacingRule =
    | BothSides
    | RightOnly
    | LeftOnly
with
    static member DefaultSpacingRule = function
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