using IniLib;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using static IniLib.Node;

namespace IniEditor
{
    public partial class IniEditor : Form
    {
        #region "Option mappings"

        private readonly Dictionary<string, List<string>> _optionToValues = new()
        {
            [Strings.CommentRule] = new()
            {
                Strings.HashAndSemicolonComments,
                Strings.HashComments,
                Strings.SemicolonComments
            },

            [Strings.DuplicateKeyRule] = new()
            {
                Strings.DisallowDuplicateKeys,
                Strings.DuplicateKeyReplacesValue,
                Strings.DuplicateKeyAddsValue
            },

            [Strings.DuplicateSectionRule] = new()
            {
                Strings.DisallowDuplicateSections,
                Strings.AllowDuplicateSections,
                Strings.MergeDuplicateIntoOriginal,
                Strings.MergeOriginalIntoDuplicate
            },

            [Strings.EscapeSequenceRule] = new()
            {
                Strings.IgnoreEscapeSequences,
                Strings.UseEscapeSequences,
                Strings.UseEscapeSequencesAndLineContinuation
            },

            [Strings.GlobalKeysRule] = new()
            {
                Strings.DisallowGlobalKeys,
                Strings.AllowGlobalKeys
            },

            [Strings.NameValueDelimiterRule] = new()
            {
                Strings.EqualsDelimiter,
                Strings.ColonDelimiter,
                Strings.EqualsOrColonDelimiter,
                Strings.NoDelimiter
            },

            [Strings.NameValueDelimiterPreferenceRule] = new()
            {
                Strings.PreferEqualsDelimiter,
                Strings.PreferColonDelimiter,
                Strings.PreferNoDelimiter
            },

            [Strings.NameValueDelimiterSpacingRule] = new()
            {
                Strings.BothSides,
                Strings.LeftOnly,
                Strings.RightOnly,
                Strings.NoSpacing
            },

            [Strings.NewlineRule] = new()
            {
                Strings.DefaultEnvironmentNewline,
                Strings.LfNewline,
                Strings.CrLfNewline
            },

            [Strings.QuotationRule] = new()
            {
                Strings.IgnoreQuotation,
                Strings.UseQuotation,
                Strings.AlwaysUseQuotation
            }
        };

        private readonly Dictionary<string, CommentRule> _textToCommentRule = new()
        {
            [Strings.HashComments] = CommentRule.HashComments,
            [Strings.SemicolonComments] = CommentRule.SemicolonComments,
            [Strings.HashAndSemicolonComments] = CommentRule.HashAndSemicolonComments
        };

        private readonly Dictionary<string, NameValueDelimiterRule> _textToDelimiterRule = new()
        {
            [Strings.EqualsDelimiter] = NameValueDelimiterRule.EqualsDelimiter,
            [Strings.ColonDelimiter] = NameValueDelimiterRule.ColonDelimiter,
            [Strings.EqualsOrColonDelimiter] = NameValueDelimiterRule.EqualsOrColonDelimiter,
            [Strings.NoDelimiter] = NameValueDelimiterRule.NoDelimiter
        };

        private readonly Dictionary<string, NameValueDelimiterPreferenceRule> _textToDelimiterPreferenceRule = new()
        {
            [Strings.PreferEqualsDelimiter] = NameValueDelimiterPreferenceRule.PreferEqualsDelimiter,
            [Strings.PreferColonDelimiter] = NameValueDelimiterPreferenceRule.PreferColonDelimiter,
            [Strings.PreferNoDelimiter] = NameValueDelimiterPreferenceRule.PreferNoDelimiter
        };

        private readonly Dictionary<string, NameValueDelimiterSpacingRule> _textToDelimiterSpacingRule = new()
        {
            [Strings.LeftOnly] = NameValueDelimiterSpacingRule.LeftOnly,
            [Strings.RightOnly] = NameValueDelimiterSpacingRule.RightOnly,
            [Strings.BothSides] = NameValueDelimiterSpacingRule.BothSides,
            [Strings.NoSpacing] = NameValueDelimiterSpacingRule.NoSpacing
        };

        private readonly Dictionary<string, DuplicateKeyRule> _textToDuplicateKeyRule = new()
        {
            [Strings.DisallowDuplicateKeys] = DuplicateKeyRule.DisallowDuplicateKeys,
            [Strings.DuplicateKeyReplacesValue] = DuplicateKeyRule.DuplicateKeyReplacesValue,
            [Strings.DuplicateKeyAddsValue] = DuplicateKeyRule.DuplicateKeyAddsValue
        };

        private readonly Dictionary<string, DuplicateSectionRule> _textToDuplicateSectionRule = new()
        {
            [Strings.DisallowDuplicateSections] = DuplicateSectionRule.DisallowDuplicateSections,
            [Strings.AllowDuplicateSections] = DuplicateSectionRule.AllowDuplicateSections,
            [Strings.MergeDuplicateIntoOriginal] = DuplicateSectionRule.MergeDuplicateSectionIntoOriginal,
            [Strings.MergeOriginalIntoDuplicate] = DuplicateSectionRule.MergeOriginalSectionIntoDuplicate
        };

        private readonly Dictionary<string, EscapeSequenceRule> _textToEscapeSequenceRule = new()
        {
            [Strings.IgnoreEscapeSequences] = EscapeSequenceRule.IgnoreEscapeSequences,
            [Strings.UseEscapeSequences] = EscapeSequenceRule.UseEscapeSequences,
            [Strings.UseEscapeSequencesAndLineContinuation] = EscapeSequenceRule.UseEscapeSequencesAndLineContinuation
        };

        private readonly Dictionary<string, QuotationRule> _textToQuotationRule = new()
        {
            [Strings.IgnoreQuotation] = QuotationRule.IgnoreQuotation,
            [Strings.UseQuotation] = QuotationRule.UseQuotation,
            [Strings.AlwaysUseQuotation] = QuotationRule.AlwaysUseQuotation
        };

        private readonly Dictionary<string, NewlineRule> _textToNewlineRule = new()
        {
            [Strings.DefaultEnvironmentNewline] = NewlineRule.DefaultEnvironmentNewline,
            [Strings.LfNewline] = NewlineRule.LfNewline,
            [Strings.CrLfNewline] = NewlineRule.CrLfNewline
        };

        private readonly Dictionary<string, GlobalKeysRule> _textToGlobalKeysRule = new()
        {
            [Strings.DisallowGlobalKeys] = GlobalKeysRule.DisallowGlobalKeys,
            [Strings.AllowGlobalKeys] = GlobalKeysRule.AllowGlobalKeys
        };

        private readonly Dictionary<NameValueDelimiterRule, string> _delimiterRuleToText = new()
        {
            [NameValueDelimiterRule.EqualsDelimiter] = Strings.EqualsDelimiter,
            [NameValueDelimiterRule.ColonDelimiter] = Strings.ColonDelimiter,
            [NameValueDelimiterRule.EqualsOrColonDelimiter] = Strings.EqualsOrColonDelimiter,
            [NameValueDelimiterRule.NoDelimiter] = Strings.NoDelimiter
        };

        private readonly Dictionary<NameValueDelimiterSpacingRule, string> _delimiterSpacingRuleToText = new()
        {
            [NameValueDelimiterSpacingRule.BothSides] = Strings.BothSides,
            [NameValueDelimiterSpacingRule.LeftOnly] = Strings.LeftOnly,
            [NameValueDelimiterSpacingRule.RightOnly] = Strings.RightOnly,
            [NameValueDelimiterSpacingRule.NoSpacing] = Strings.NoSpacing
        };

        private readonly Dictionary<NameValueDelimiterPreferenceRule, string> _delimiterPreferenceRuleToText = new()
        {
            [NameValueDelimiterPreferenceRule.PreferEqualsDelimiter] = Strings.PreferEqualsDelimiter,
            [NameValueDelimiterPreferenceRule.PreferColonDelimiter] = Strings.PreferColonDelimiter,
            [NameValueDelimiterPreferenceRule.PreferNoDelimiter] = Strings.PreferNoDelimiter
        };

        #endregion

        private readonly List<CommonFileDialogFilter> INI_FILTER = new()
        {
            new CommonFileDialogFilter("INI files", "*.ini"),
            new CommonFileDialogFilter("Conf files", "*.conf"),
            new CommonFileDialogFilter("Properties files", "*.properties"),
            new CommonFileDialogFilter("All files", "*.*")
        };

        private Dictionary<string, CommonFileDialogComboBox> _optionToComboBox = new();
        private Dictionary<string, int> _optionToSelectedIndex = new();

        private const string DEFAULT_FILE_PATH = "untitled.ini";

        private string _filePath = DEFAULT_FILE_PATH;
        private Encoding _fileEncoding = Encoding.UTF8;
        private Options _options = Options.defaultOptions;
        private Configuration.Configuration _configuration = Configuration.empty;
        private Regex _unixNewline = new("(?<!\\r)\\n");
        private TreeNode? _mainNode = null;
        private TreeNode? _clickedNode = null;
        private bool _isNewNode = false;
        private bool _isChanged = false;

        public IniEditor()
        {
            InitializeComponent();
            menuStrip1.Renderer = new MyMenuItemRenderer();
            ResetDocument();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CheckForChanges())
            {
                e.Cancel = true;
            }
        }

        #region Tree view events

        private void tvKeys_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
            {
                tvKeys.SelectedNode.Toggle();
                e.Handled = true;
            }
            else if (e.KeyChar == '\r')
            {
                BeginEdit(tvKeys.SelectedNode);
                e.Handled = true;
            }
        }

        private void tvKeys_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            SelectTreeNode(e.Node);
        }

        private void tvKeys_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectTreeNode(e.Node);
        }

        private void tvKeys_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            BeginEdit(e.Node);
        }

        private void BeginEdit(TreeNode node)
        {
            if (node.Name == "section"
                || node.Name == "key"
                || node.Name == "value")
            {
                node.BeginEdit();
            }
        }

        private void tvKeys_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.CancelEdit || e.Node == null)
            {
                return;
            }

            tbContent.DeselectAll();

            Node highlightedNode = null;

            switch (e.Node.Name)
            {
                case "section":
                    if (_isNewNode)
                    {
                        if (string.IsNullOrWhiteSpace(e.Label))
                        {
                            e.Node.Remove();
                        }
                        else
                        {
                            e.Node.Text = e.Label;
                        }
                    }
                    else if (e.Label != null)
                    {
                        Time("Renamed in {0}ms", () => _configuration = Configuration.renameSection(_options, e.Node.Text, e.Label, _configuration));
                        highlightedNode = Configuration.getSectionNode(e.Label, _configuration)[0];
                    }
                    break;

                case "key":
                    if (string.IsNullOrWhiteSpace(e.Label))
                    {
                        e.CancelEdit = true;
                        break;
                    }

                    if (_isNewNode)
                    {
                        var sectionName = e.Node.Parent.Text;
                        e.Node.Text = e.Label;
                        Time("Added in {0}ms", () => _configuration = Configuration.add(_options, sectionName, e.Label, string.Empty, _configuration));
                        e.Node.Expand();
                        e.Node.Nodes[0].BeginEdit();
                        highlightedNode = Configuration.getNode(sectionName, e.Label, _configuration);
                    }
                    else if (e.Label != null)
                    {
                        var keyName = e.Node.Text;
                        var sectionName = e.Node.Parent.Text;
                        Time("Renamed in {0}ms", () => _configuration = Configuration.renameKey(_options, sectionName, keyName, e.Label, _configuration));
                        highlightedNode = Configuration.getNode(sectionName, e.Label, _configuration);
                    }
                    break;

                case "value":
                    if (e.Label != null)
                    {
                        var keyName = e.Node.Parent.Text;
                        var sectionName = e.Node.Parent.Parent.Text;
                        if (string.IsNullOrEmpty(e.Label))
                        {
                            e.CancelEdit = true;
                            e.Node.Text = "<empty>";
                        }
                        Time("Added in {0}ms", () => _configuration = Configuration.add(_options, sectionName, keyName, e.Label, _configuration));
                        var keyNode = Configuration.getNode(sectionName, keyName, _configuration) as KeyNode;
                        highlightedNode = keyNode.children.OfType<KeyValueNode>().First();
                    }
                    break;
            }

            _isChanged = true;
            _isNewNode = false;
            LoadConfigurationText();
            UpdateTitle();
            SelectConfigurationNode(highlightedNode);
        }

        private void tvSyntaxTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var text = tbContent.Text;
            var node = e.Node.Tag as Node;
            var (startLine, startColumn) = Node.position(node);
            var (endLine, endColumn) = Node.endPosition(node);
            var startOffset = CountOffset(text, startLine, startColumn);
            var endOffset = CountOffset(text, endLine, endColumn);
            tbContent.Select(startOffset, endOffset - startOffset);
        }

        #endregion

        #region "Top menu events"

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckForChanges())
            {
                ResetDocument();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CheckForChanges())
            {
                return;
            }

            var openFile = new CommonOpenFileDialog { RestoreDirectory = true };
            SetupFileDialogOptions(openFile);

            if (openFile.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SaveFileDialogOptions();
                _options = GetOptions();
                _filePath = openFile.FileName;
                UpdateTitle();
                LoadConfiguration();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveConfiguration();
        }

        private void saveasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveConfiguration(true);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion

        #region "Context menu events"

        private void addSectionToolStripMenuItem_top_Click(object sender, EventArgs e)
        {
            var sectionNode = CreateSectionNode(string.Empty);
            _mainNode.Nodes.Add(sectionNode);
            _isNewNode = true;
            _mainNode.Expand();
            sectionNode.BeginEdit();
        }

        private void collapseAllToolStripMenuItem_top_Click(object sender, EventArgs e)
        {
            _mainNode.Collapse();
        }

        private void expandAllToolStripMenuItem_top_Click(object sender, EventArgs e)
        {
            _mainNode.ExpandAll();
        }

        private void expandAllToolStripMenuItem_sectionAndKey_Click(object sender, EventArgs e)
        {
            _clickedNode?.ExpandAll();
        }

        private void collapseAllToolStripMenuItem_sectionAndKey_Click(object sender, EventArgs e)
        {
            _clickedNode?.Collapse();
        }

        private void addToolStripMenuItem_sectionAndKey_Click(object sender, EventArgs e)
        {
            if (_clickedNode == null)
            {
                return;
            }

            var sectionNode = _clickedNode.Name switch
            {
                "key" => _clickedNode.Parent,
                "section" => _clickedNode
            };

            var keyNode = CreateKeyNode(string.Empty);
            var valueNode = CreateValueNode(string.Empty);
            keyNode.Nodes.Add(valueNode);
            sectionNode.Nodes.Add(keyNode);
            sectionNode.Expand();
            _isChanged = true;
            _isNewNode = true;
            keyNode.BeginEdit();
        }

        private void renameToolStripMenuItem_sectionAndKey_Click(object sender, EventArgs e)
        {
            _clickedNode?.BeginEdit();
        }

        private void deleteToolStripMenuItem_sectionAndKey_Click(object sender, EventArgs e)
        {
            if (_clickedNode == null)
            {
                return;
            }

            switch (_clickedNode.Name)
            {
                case "key":
                    {
                        var keyName = _clickedNode.Text;
                        var sectionName = _clickedNode.Parent.Text;
                        Time("Removed in {0}ms", () => _configuration = Configuration.removeKey(_options, sectionName, keyName, _configuration));
                    }
                    break;

                case "section":
                    {
                        var sectionName = _clickedNode.Text;
                        Time("Removed in {0}ms", () => _configuration = Configuration.removeSection(_options, sectionName, _configuration));
                    }
                    break;

                default:
                    return;
            }

            _clickedNode.Parent.Nodes.Remove(_clickedNode);
            _isChanged = true;
            LoadConfigurationText();
        }

        private void editToolStripMenuItem_keyValue_Click(object sender, EventArgs e)
        {
            _clickedNode?.BeginEdit();
        }

        #endregion

        #region "Utilities"

        private void SetupFileDialogOptions(CommonFileDialog fileDialog)
        {
            INI_FILTER.ForEach(f => fileDialog.Filters.Add(f));

            foreach (var pair in _optionToValues)
            {
                var combo = new CommonFileDialogComboBox(pair.Key);
                pair.Value.ForEach(item => combo.Items.Add(new CommonFileDialogComboBoxItem(item)));
                combo.SelectedIndex = _optionToSelectedIndex.ContainsKey(pair.Key) ? _optionToSelectedIndex[pair.Key] : 0;
                fileDialog.Controls.Add(combo);
                _optionToComboBox[pair.Key] = combo;
            }

            this.NameValueDelimiterRuleComboBoxChanged(null, null);
            _optionToComboBox[Strings.NameValueDelimiterRule].SelectedIndexChanged += NameValueDelimiterRuleComboBoxChanged;
            _optionToComboBox[Strings.NameValueDelimiterPreferenceRule].SelectedIndexChanged += NameValueDelimiterPreferenceComboBoxChanged;
        }

        private void NameValueDelimiterRuleComboBoxChanged(object? sender, EventArgs e)
        {
            var selectedIndex = _optionToComboBox[Strings.NameValueDelimiterRule].SelectedIndex;
            var delimiterRuleText = _optionToValues[Strings.NameValueDelimiterRule][selectedIndex];
            var delimiterRule = _textToDelimiterRule[delimiterRuleText];

            SelectDefaultPreferenceRule(delimiterRule);
            SelectDefaultSpacingRule(delimiterRule);
        }

        private void NameValueDelimiterPreferenceComboBoxChanged(object? sender, EventArgs e)
        {
            var delimiterIndex = _optionToComboBox[Strings.NameValueDelimiterRule].SelectedIndex;
            var delimiterRuleText = _optionToValues[Strings.NameValueDelimiterRule][delimiterIndex];
            var delimiterRule = _textToDelimiterRule[delimiterRuleText];

            var preferenceIndex = _optionToComboBox[Strings.NameValueDelimiterPreferenceRule].SelectedIndex;
            var preferenceRuleText = _optionToValues[Strings.NameValueDelimiterPreferenceRule][preferenceIndex];
            var preferenceRule = _textToDelimiterPreferenceRule[preferenceRuleText];

            if (preferenceRule == NameValueDelimiterPreferenceRule.PreferNoDelimiter
                && delimiterRule != NameValueDelimiterRule.NoDelimiter)
            {
                SelectDefaultPreferenceRule(delimiterRule);
            }
        }

        private void SelectDefaultPreferenceRule(NameValueDelimiterRule delimiterRule)
        {
            var defaultPreferenceOption = NameValueDelimiterPreferenceRule.DefaultFor.Invoke(delimiterRule);
            var preferenceIndex = _optionToValues[Strings.NameValueDelimiterPreferenceRule].IndexOf(_delimiterPreferenceRuleToText[defaultPreferenceOption]);
            _optionToComboBox[Strings.NameValueDelimiterPreferenceRule].SelectedIndex = preferenceIndex;
            _optionToComboBox[Strings.NameValueDelimiterPreferenceRule].Enabled = delimiterRule == NameValueDelimiterRule.EqualsOrColonDelimiter;
        }

        private void SelectDefaultSpacingRule(NameValueDelimiterRule delimiterRule)
        {
            var defaultSpacingOption = NameValueDelimiterSpacingRule.DefaultFor.Invoke(delimiterRule);
            var spacingIndex = _optionToValues[Strings.NameValueDelimiterSpacingRule].IndexOf(_delimiterSpacingRuleToText[defaultSpacingOption]);
            _optionToComboBox[Strings.NameValueDelimiterSpacingRule].SelectedIndex = spacingIndex;
            _optionToComboBox[Strings.NameValueDelimiterSpacingRule].Enabled = delimiterRule != NameValueDelimiterRule.NoDelimiter;
        }

        private void SaveFileDialogOptions()
        {
            foreach (var comboPair in _optionToComboBox)
            {
                _optionToSelectedIndex[comboPair.Key] = comboPair.Value.SelectedIndex;
            }
        }

        private Options GetOptions()
        {
            string GetOptionText(string key)
            {
                var i = _optionToSelectedIndex[key];
                return _optionToValues[key][i];
            }

            return new Options(
                commentRule: _textToCommentRule[GetOptionText(Strings.CommentRule)],
                duplicateKeyRule: _textToDuplicateKeyRule[GetOptionText(Strings.DuplicateKeyRule)],
                duplicateSectionRule: _textToDuplicateSectionRule[GetOptionText(Strings.DuplicateSectionRule)],
                escapeSequenceRule: _textToEscapeSequenceRule[GetOptionText(Strings.EscapeSequenceRule)],
                globalKeysRule: _textToGlobalKeysRule[GetOptionText(Strings.GlobalKeysRule)],
                nameValueDelimiterRule: _textToDelimiterRule[GetOptionText(Strings.NameValueDelimiterRule)],
                nameValueDelimiterPreferenceRule: _textToDelimiterPreferenceRule[GetOptionText(Strings.NameValueDelimiterPreferenceRule)],
                nameValueDelimiterSpacingRule: _textToDelimiterSpacingRule[GetOptionText(Strings.NameValueDelimiterSpacingRule)],
                newlineRule: _textToNewlineRule[GetOptionText(Strings.NewlineRule)],
                quotationRule: _textToQuotationRule[GetOptionText(Strings.QuotationRule)]);
        }

        private void ResetDocument()
        {
            _configuration = Configuration.empty;
            tbContent.Clear();
            tvSyntaxTree.Nodes.Clear();
            tvKeys.Nodes.Clear();
            _mainNode = new TreeNode { Text = "Untitled", Name = "root" };
            _mainNode.ContextMenuStrip = topContextMenuStrip;
            tvKeys.Nodes.Add(_mainNode);
            _filePath = DEFAULT_FILE_PATH;
            _fileEncoding = Encoding.Default;
            _isChanged = false;
            UpdateTitle();
        }

        private void LoadConfigurationText()
        {
            var configText = Configuration.toText(_options, _configuration);
            tbContent.Text = _unixNewline.Replace(configText, "\r\n");
        }

        private void UpdateTitle()
        {
            if (Text.Contains(" - "))
            {
                Text = Text.Substring(0, Text.IndexOf(" - "));
            }

            if (!string.IsNullOrEmpty(_filePath))
            {
                var changedIndicator = _isChanged ? "*" : string.Empty;
                var fileName = Path.GetFileName(_filePath);
                Text += $" - {fileName}{changedIndicator}";
                _mainNode.Text = fileName;
            }
        }

        private TreeNode CreateSectionNode(string name)
        {
            var node = new TreeNode { Text = name, Name = "section" };
            node.ContextMenuStrip = sectionAndKeyContextMenuStrip;
            return node;
        }

        private TreeNode CreateKeyNode(string name)
        {
            var node = new TreeNode { Text = name, Name = $"key" };
            node.ContextMenuStrip = sectionAndKeyContextMenuStrip;
            return node;
        }

        private TreeNode CreateValueNode(string value)
        {
            var node = new TreeNode { Text = string.IsNullOrWhiteSpace(value) ? "<empty>" : value, Name = $"value" };
            node.ContextMenuStrip = keyValueMenuStrip;
            return node;
        }

        private Stopwatch _stopwatch = new();

        private void Time(string format, Action action)
        {
            _stopwatch.Restart();
            action();
            _stopwatch.Stop();
            toolStripStatusLabel1.Text = string.Format(format, _stopwatch.ElapsedMilliseconds);
        }

        private void LoadConfiguration()
        {
            using (var stream = new StreamReader(_filePath, true))
            {
                stream.Peek();
                _fileEncoding = stream.CurrentEncoding;

                try
                {
                    Time("Parsed in {0}ms", () => _configuration = Configuration.fromStreamReader(_options ?? Options.defaultOptions, stream));
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            _isChanged = false;
            _mainNode.Text = Path.GetFileName(_filePath);
            _mainNode.Nodes.Clear();
            tvSyntaxTree.Nodes.Clear();
            LoadConfigurationText();
            tbContent.Select(0, 0);
            tbContent.ScrollToCaret();

            foreach (var section in Configuration.sections(_configuration))
            {
                var sectionNode = CreateSectionNode(section);

                foreach (var key in Configuration.keys(section, _configuration))
                {
                    var keyNode = CreateKeyNode(key);
                    var valueNode = CreateValueNode(Configuration.get(section, key, _configuration));
                    keyNode.Nodes.Add(valueNode);
                    sectionNode.Nodes.Add(keyNode);
                }

                _mainNode.Nodes.Add(sectionNode);
            }

            _mainNode.Expand();
        }

        private void SaveConfiguration(bool showSaveDialog = false)
        {
            if (_filePath == null || showSaveDialog)
            {
                var saveFile = new CommonSaveFileDialog { RestoreDirectory = true };
                SetupFileDialogOptions(saveFile);

                if (saveFile.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    SaveFileDialogOptions();
                    _options = GetOptions();
                    _filePath = saveFile.FileName;
                }
            }

            Configuration.writeToFile(_options, _filePath, _configuration);
            LoadConfigurationText();
            _isChanged = false;
            UpdateTitle();
        }

        private void SelectConfigurationNode(Node? node)
        {
            if (node == null)
            {
                return;
            }

            BuildSyntaxTree(node);

            IEnumerable<Node>? textNodes = node switch
            {
                KeyNode keyNode when keyNode.children is [KeyNameNode keyNameNode, ..] => keyNameNode.children.OfType<ReplaceableTokenNode>(),
                KeyNameNode keyNameNode => keyNameNode.children.OfType<ReplaceableTokenNode>(),
                KeyValueNode keyValueNode => keyValueNode.children.OfType<ReplaceableTokenNode>(),
                SectionNode sectionNode =>
                    // Select section heading if there is one, otherwise select the first key
                    sectionNode.children is [SectionHeadingNode sectionHeadingNode, ..]
                        ? new List<Node> { sectionHeadingNode }
                        : sectionNode.children.Take(1),
                _ => null
            };

            if (textNodes == null)
            {
                return;
            }

            var (line, column) = Node.position(textNodes.First());
            var (lastLine, lastColumn) = Node.endPosition(textNodes.Last());
            var startOffset = CountOffset(tbContent.Text, line, column);
            var endOffset = CountOffset(tbContent.Text, lastLine, lastColumn);

            tbContent.Select(startOffset, endOffset - startOffset);
        }

        private void SelectTreeNode(TreeNode treeNode)
        {
            _clickedNode = treeNode;

            switch (_clickedNode.Name)
            {
                case "root":
                    SelectConfigurationNode(_configuration.Item1);
                    break;

                case "section":
                    {
                        var sectionName = _clickedNode.Text;
                        var node = Configuration.tryGetSectionNode(sectionName, _configuration);
                        SelectConfigurationNode(node?.Value[0]);
                    }
                    break;

                case "key":
                    {
                        var keyName = _clickedNode.Text;
                        var sectionName = _clickedNode.Parent.Text;
                        var node = Configuration.getNode(sectionName, keyName, _configuration);
                        SelectConfigurationNode(node);
                    }
                    break;

                case "value":
                    {
                        var keyName = _clickedNode.Parent.Text;
                        var sectionName = _clickedNode.Parent.Parent.Text;
                        var node = Configuration.getNode(sectionName, keyName, _configuration);

                        if (node is KeyNode keyNode)
                        {
                            var valueNode = keyNode.children.OfType<KeyValueNode>().FirstOrDefault();
                            SelectConfigurationNode(valueNode);
                        }
                    }
                    break;
            }
        }

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);

        private const int WM_SETREDRAW = 11;

        private static void SuspendDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, false, 0);
        }

        private static void ResumeDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, true, 0);
            parent.Refresh();
        }
        private void BuildSyntaxTree(Node node)
        {
            SuspendDrawing(tvSyntaxTree);
            tvSyntaxTree.Nodes.Clear();
            var rootNode = Inner(node);
            rootNode.ExpandAll();
            rootNode.EnsureVisible();
            ResumeDrawing(tvSyntaxTree);

            TreeNode Inner(Node node, TreeNode parent = null)
            {
                var treeNode = new TreeNode { Text = node.ToString(), Tag = node };

                if (parent == null)
                {
                    tvSyntaxTree.Nodes.Add(treeNode);
                }
                else
                {
                    parent.Nodes.Add(treeNode);
                }

                var children = node switch
                {
                    RootNode rootNode => rootNode.children.ToList(),
                    KeyNode keyNode => keyNode.children.ToList(),
                    KeyNameNode keyNameNode => keyNameNode.children.ToList(),
                    KeyValueNode keyValueNode => keyValueNode.children.ToList(),
                    SectionNode sectionNode => sectionNode.children.ToList(),
                    SectionHeadingNode sectionHeadingNode => sectionHeadingNode.children.ToList(),
                    CommentNode commentNode => commentNode.children.ToList(),
                    _ => new List<Node> { }
                };

                foreach (var child in children)
                {
                    Inner(child, treeNode);
                }

                return treeNode;
            }
        }

        private int CountOffset(string text, int line, int column)
        {
            int i = 0;

            for (int j = 1, k = 1; j < line || k < column; i++)
            {
                if (i < text.Length && text[i] == '\n')
                {
                    j++;
                    k = 1;
                }
                else
                {
                    k++;
                }
            }

            return i;
        }

        private bool CheckForChanges()
        {
            if (_isChanged)
            {
                switch (MessageBox.Show("Do you want to save your changes?", "Unsaved changes", MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Yes:
                        SaveConfiguration();
                        break;

                    case DialogResult.No:
                        break;

                    case DialogResult.Cancel:
                        return true;
                }
            }

            return false;
        }

        #endregion
    }
}
