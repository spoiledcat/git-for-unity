using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.VersionControl.Git.UI
{
    class Styles
    {
        public const float BaseSpacing = 10f,
                           BrowseButtonWidth = 25f,
                           BroadModeLimit = 500f,
                           NarrowModeLimit = 300f,
                           ModeNotificationDelay = .5f,
                           BroadModeBranchesMinWidth = 200f,
                           BroadModeBranchesRatio = .4f,
                           InitialStateAreaWidth = 200f,
                           LocksEntryHeight = 42f,
                           LocksSummaryHeight = 5f,
                           LocksUserHeight = 5f,
                           LocksDateHeight = 5f,
                           HistoryEntryHeight = 40f,
                           HistorySummaryHeight = 8f,
                           HistoryDetailsHeight = 16f,
                           HistoryEntryPadding = 16f,
                           HistoryChangesIndentation = 17f,
                           CommitAreaMinHeight = 16f,
                           CommitAreaDefaultRatio = .4f,
                           CommitAreaMaxHeight = 12 * 15f,
                           CommitAreaPadding = 5f,
                           PublishViewSpacingHeight = 5f,
                           MinCommitTreePadding = 20f,
                           TreePadding = 12f,
                           TreeIndentation = 12f,
                           TreeRootIndentation = -5f,
                           TreeVerticalSpacing = 3f,
                           CommitIconSize = 16f,
                           CommitIconHorizontalPadding = -5f,
                           BranchListIndentation = 20f,
                           BranchListSeparation = 15f,
                           RemotesTotalHorizontalMargin = 37f,
                           RemotesNameRatio = .2f,
                           RemotesUserRatio = .2f,
                           RemotesHostRation = .5f,
                           RemotesAccessRatio = .1f,
                           GitIgnoreRulesTotalHorizontalMargin = 33f,
                           GitIgnoreRulesSelectorWidth = 14f,
                           GitIgnoreRulesEffectRatio = .2f,
                           GitIgnoreRulesFileRatio = .3f,
                           GitIgnoreRulesLineRatio = .5f;

        const int
            FoldoutContentRightPadding = 6,
            FoldoutContentBottomPadding = 4,
            FoldoutIndentation = 15;

        public const int HalfSpacing = (int)(BaseSpacing / 2);

        private const string WarningLabel = "<b>Warning:</b> {0}";

        private static GUIStyle label,
                                boldLabel,
                                centeredErrorLabel,
                                errorLabel,
                                deletedFileLabel,
                                longMessageStyle,
                                headerBoxStyle,
                                headerStyle,
                                headerBranchLabelStyle,
                                headerUrlLabelStyle,
                                headerRepoLabelStyle,
                                fileHistoryLogTitleStyle,
                                headerTitleStyle,
                                headerDescriptionStyle,
                                toolbarButtonStyle,
                                historyLockStyle,
                                historyEntrySummaryStyle,
                                historyEntryDetailsStyle,
                                historyEntryDetailsRightStyle,
                                historyFileTreeBoxStyle,
                                commitFileAreaStyle,
                                commitButtonStyle,
                                textFieldStyle,
                                boldCenteredLabel,
                                centeredLabel,
                                commitDescriptionFieldStyle,
                                toggleMixedStyle,
                                authHeaderBoxStyle,
                                lockedFileRowSelectedStyle,
                                lockedFileRowStyle,
                                genericTableBoxStyle,
                                historyDetailsTitleStyle,
                                historyDetailsMetaInfoStyle,
                                genericBoxStyle,
                                hyperlinkStyle,
                                selectedArea,
                                selectedLabel,
                                progressAreaBackStyle,
                                labelNoWrap,
                                invisibleLabel,
                                locksViewLockedByStyle,
                                locksViewLockedBySelectedStyle;

        public static Texture2D GetFileStatusIcon(GitFileStatus status, bool isLocked)
        {
            if (isLocked)
            {
                switch (status)
                {
                    case GitFileStatus.Modified:
                        return Utility.GetIcon("locked");

                    default:
                        return Utility.GetIcon("locked-by-person");
                }
            }

            switch (status)
            {
                case GitFileStatus.Modified:
                    return Utility.GetIcon("modified");
                case GitFileStatus.Added:
                    return Utility.GetIcon("added");
                case GitFileStatus.Deleted:
                    return Utility.GetIcon("removed");
                case GitFileStatus.Renamed:
                    return Utility.GetIcon("renamed");
                case GitFileStatus.Untracked:
                    return Utility.GetIcon("untracked");
            }

            return null;
        }

        public static void BeginInitialStateArea(string title, string message)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.MaxWidth(InitialStateAreaWidth));
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Label(message, LongMessageStyle);
        }

        public static void EndInitialStateArea()
        {
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        public static bool InitialStateActionButton(string label)
        {
            bool result;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            result = GUILayout.Button(label, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            return result;
        }

        public static void Warning(string message)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(string.Format(WarningLabel, message), LongMessageStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static GUIStyle HistoryFileTreeBoxStyle
        {
            get
            {
                if (historyFileTreeBoxStyle == null)
                {
                    var padding = new RectOffset((int)HistoryChangesIndentation, 0, 0, 0);

                    historyFileTreeBoxStyle = new GUIStyle();
                    historyFileTreeBoxStyle.padding = padding;
                }

                return historyFileTreeBoxStyle;
            }
        }

        public static GUIStyle SelectedArea
        {
            get
            {
                if (selectedArea == null)
                {
                    selectedArea = new GUIStyle(GUI.skin.label);
                    //selectedArea.name = "SelectedArea";

                    var hierarchyStyle = GUI.skin.FindStyle("PR Label");
                    selectedArea.normal.background = hierarchyStyle.onFocused.background;
                    selectedArea.focused.background = hierarchyStyle.onFocused.background;
                }
                return selectedArea;
            }
        }

        public static GUIStyle Label
        {
            get
            {
                if (label == null)
                {
                    label = new GUIStyle(GUI.skin.label);
                    //label.name = "CustomLabel";

                    var hierarchyStyle = GUI.skin.FindStyle("PR Label");
                    label.onNormal.background = hierarchyStyle.onNormal.background;
                    label.onNormal.textColor = hierarchyStyle.onNormal.textColor;
                    label.onFocused.background = hierarchyStyle.onFocused.background;
                    label.onFocused.textColor = hierarchyStyle.onFocused.textColor;
                    label.wordWrap = true;
                }
                return label;
            }
        }

        public static GUIStyle LabelNoWrap
        {
            get
            {
                if (labelNoWrap == null)
                {
                    labelNoWrap = new GUIStyle(GUI.skin.label);
                    //labelNoWrap.name = "LabelNoWrap";

                    var hierarchyStyle = GUI.skin.FindStyle("PR Label");
                    labelNoWrap.onNormal.background = hierarchyStyle.onNormal.background;
                    labelNoWrap.onNormal.textColor = hierarchyStyle.onNormal.textColor;
                    labelNoWrap.onFocused.background = hierarchyStyle.onFocused.background;
                    labelNoWrap.onFocused.textColor = hierarchyStyle.onFocused.textColor;
                    labelNoWrap.wordWrap = false;
                }
                return labelNoWrap;
            }
        }

        public static GUIStyle InvisibleLabel
        {
            get
            {
                if (invisibleLabel == null)
                {
                    invisibleLabel = new GUIStyle(GUI.skin.label);
                    //invisibleLabel.name = "InvisibleLabel";

                    var hierarchyStyle = GUI.skin.FindStyle("PR Label");
                    invisibleLabel.onNormal.background = hierarchyStyle.onNormal.background;
                    invisibleLabel.onNormal.textColor = new Color(255, 0, 0, 0);
                    invisibleLabel.onFocused.background = hierarchyStyle.onFocused.background;
                    invisibleLabel.onFocused.textColor = new Color(255, 0, 0, 0);
                    invisibleLabel.wordWrap = true;
                }
                return invisibleLabel;
            }
        }

        public static GUIStyle SelectedLabel
        {
            get
            {
                if (selectedLabel == null)
                {
                    selectedLabel = new GUIStyle(GUI.skin.label);
                    //selectedLabel.name = "SelectedLabel";

                    var hierarchyStyle = GUI.skin.FindStyle("PR Label");
                    selectedLabel.onNormal.background = hierarchyStyle.onFocused.background;
                    selectedLabel.onNormal.textColor = hierarchyStyle.onFocused.textColor;
                    selectedLabel.onFocused.background = hierarchyStyle.onFocused.background;
                    selectedLabel.onFocused.textColor = hierarchyStyle.onFocused.textColor;
                    selectedLabel.normal.background = hierarchyStyle.onFocused.background;
                    selectedLabel.normal.textColor = hierarchyStyle.onFocused.textColor;
                    selectedLabel.wordWrap = true;
                }
                return selectedLabel;
            }
        }

        public static GUIStyle HeaderBranchLabelStyle
        {
            get
            {
                if (headerBranchLabelStyle == null)
                {
                    headerBranchLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                    //headerBranchLabelStyle.name = "HeaderBranchLabelStyle";
                    headerBranchLabelStyle.margin = new RectOffset(0, 0, 0, 0);
                    headerBranchLabelStyle.wordWrap = true;
                }
                return headerBranchLabelStyle;
            }
        }

        public static GUIStyle HeaderRepoLabelStyle
        {
            get
            {
                if (headerRepoLabelStyle == null)
                {
                    headerRepoLabelStyle = new GUIStyle(EditorStyles.label);
                    //headerRepoLabelStyle.name = "HeaderRepoLabelStyle";
                    headerRepoLabelStyle.margin = new RectOffset(0, 0, 0, 0);
                    headerRepoLabelStyle.wordWrap = true;
                }
                return headerRepoLabelStyle;
            }
        }

        public static GUIStyle HeaderUrlLabelStyle
        {
            get
            {
                if (headerUrlLabelStyle == null)
                {
                    headerUrlLabelStyle = new GUIStyle(EditorStyles.label);
                    //headerUrlLabelStyle.name = "HeaderUrlLabelStyle";
                    headerUrlLabelStyle.margin = new RectOffset(0, 0, 0, 0);
                    headerUrlLabelStyle.fontStyle = FontStyle.Italic;
                }
                return headerUrlLabelStyle;
            }
        }

        public static GUIStyle HeaderTitleStyle
        {
            get
            {
                if (headerTitleStyle == null)
                {
                    headerTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                    //headerTitleStyle.name = "HeaderTitleStyle";
                    headerTitleStyle.margin = new RectOffset(0, 0, 0, 0);
                    headerTitleStyle.wordWrap = true;
                }
                return headerTitleStyle;
            }
        }

        public static GUIStyle HeaderDescriptionStyle
        {
            get
            {
                if (headerDescriptionStyle == null)
                {
                    headerDescriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                    //headerDescriptionStyle.name = "HeaderDescriptionStyle";
                    headerDescriptionStyle.margin = new RectOffset(0, 0, 0, 0);
                }
                return headerDescriptionStyle;
            }
        }

        public static GUIStyle HeaderBoxStyle
        {
            get
            {
                if (headerBoxStyle == null)
                {
                    headerBoxStyle = new GUIStyle("IN BigTitle");
                    //headerBoxStyle.name = "HeaderBoxStyle";
                    headerBoxStyle.padding = new RectOffset(5, 5, 5, 5);
                    headerBoxStyle.margin = new RectOffset(0, 0, 0, 0);
                }
                return headerBoxStyle;
            }
        }

        public static GUIStyle HeaderStyle
        {
            get
            {
                if (headerStyle == null)
                {
                    headerStyle = new GUIStyle("IN BigTitle");
                    //headerStyle.name = "HeaderStyle";
                    headerStyle.margin = new RectOffset(0, 0, 0, 0);
                    headerStyle.padding = new RectOffset(0, 0, 0, 0);
                }
                return headerStyle;
            }
        }

        public static GUIStyle BoldLabel
        {
            get
            {
                if (boldLabel == null)
                {
                    boldLabel = new GUIStyle(Label);
                    //boldLabel.name = "CustomBoldLabel";

                    boldLabel.fontStyle = FontStyle.Bold;
                }
                return boldLabel;
            }
        }

        public static GUIStyle DeletedFileLabel
        {
            get
            {
                if (deletedFileLabel == null)
                {
                    deletedFileLabel = new GUIStyle(EditorStyles.label);
                    //deletedFileLabel.name = "DeletedFileLabel";
                    deletedFileLabel.normal.textColor = Color.gray;
                }
                return deletedFileLabel;
            }
        }

        public static GUIStyle ErrorLabel
        {
            get
            {
                if (errorLabel == null)
                {
                    errorLabel = new GUIStyle(EditorStyles.wordWrappedLabel);
                    //errorLabel.name = "ErrorLabel";
                    errorLabel.normal.textColor = Color.red;
                }
                return errorLabel;
            }
        }

        public static GUIStyle CenteredErrorLabel
        {
            get
            {
                if (centeredErrorLabel == null)
                {
                    centeredErrorLabel = new GUIStyle(EditorStyles.wordWrappedLabel);
                    centeredErrorLabel.alignment = TextAnchor.MiddleCenter;
                    //centeredErrorLabel.name = "CenteredErrorLabel";
                    centeredErrorLabel.normal.textColor = Color.red;
                }
                return centeredErrorLabel;
            }
        }

        public static GUIStyle LongMessageStyle
        {
            get
            {
                if (longMessageStyle == null)
                {
                    longMessageStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                    //longMessageStyle.name = "LongMessageStyle";
                    longMessageStyle.richText = true;
                }
                return longMessageStyle;
            }
        }

        public static GUIStyle ToolbarButtonStyle
        {
            get
            {
                if (toolbarButtonStyle == null)
                {
                    toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
                    //toolbarButtonStyle.name = "HistoryToolbarButtonStyle";
                    toolbarButtonStyle.richText = true;
                }
                return toolbarButtonStyle;
            }
        }

        public static GUIStyle LockButtonStyle
        {
            get
            {
                if (historyLockStyle == null)
                {
                    historyLockStyle = new GUIStyle(GUI.skin.FindStyle("IN LockButton"));
                    //historyLockStyle.name = "LockStyle";
                }
                historyLockStyle.margin = new RectOffset(3, 3, 2, 2);
                return historyLockStyle;
            }
        }

        private static GUIStyle historyEntryLine;
        public static GUIStyle HistoryEntryLine
        {
            get
            {
                if (historyEntryLine == null)
                {
                    historyEntryLine = new GUIStyle("TV Selection");
                }

                return historyEntryLine;
            }
        }

        public static GUIStyle HistoryEntrySummaryStyle
        {
            get
            {
                if (historyEntrySummaryStyle == null)
                {
                    historyEntrySummaryStyle = new GUIStyle("TV Line");
                    historyEntrySummaryStyle.contentOffset = new Vector2(BaseSpacing * 2, 0);
                }
                return historyEntrySummaryStyle;
            }
        }

        public static GUIStyle HistoryEntryDetailsStyle
        {
            get
            {
                if (historyEntryDetailsStyle == null)
                {
                    historyEntryDetailsStyle = new GUIStyle("TV Line");
                    var c = historyEntryDetailsStyle.normal.textColor;
                    historyEntryDetailsStyle.normal.textColor = new Color(c.r, c.g, c.b, c.a * 0.8f);
                    c = historyEntryDetailsStyle.focused.textColor;
                    historyEntryDetailsStyle.focused.textColor = new Color(c.r, c.g, c.b, c.a * 0.8f);
                    historyEntryDetailsStyle.contentOffset = new Vector2(BaseSpacing * 2, 0);
                    HistoryEntryDetailsStyle.fontSize = HistoryEntryDetailsStyle.fontSize - 1;
                }
                return historyEntryDetailsStyle;
            }
        }

        public static GUIStyle HistoryEntryDetailsRightStyle
        {
            get
            {
                if (historyEntryDetailsRightStyle == null)
                {
                    historyEntryDetailsRightStyle = new GUIStyle(HistoryEntryDetailsStyle);
                    //historyEntryDetailsRightStyle.name = "HistoryEntryDetailsRightStyle";
                }
                historyEntryDetailsRightStyle.alignment = TextAnchor.MiddleRight;
                return historyEntryDetailsRightStyle;
            }
        }

        public static GUIStyle LockedFileRowStyle
        {
            get
            {
                if (lockedFileRowStyle == null)
                {
                    lockedFileRowStyle = new GUIStyle();
                    //lockedFileRowStyle.name = "LockedFileRowStyle";
                    lockedFileRowStyle.padding = new RectOffset(2, 2, 1, 1);
                }
                return lockedFileRowStyle;
            }
        }

        public static GUIStyle LockedFileRowSelectedStyle
        {
            get
            {
                if (lockedFileRowSelectedStyle == null)
                {
                    var hierarchyStyle = GUI.skin.FindStyle("PR Label");
                    lockedFileRowSelectedStyle = new GUIStyle(LockedFileRowStyle);
                    //lockedFileRowSelectedStyle.name = "LockedFileRowSelectedStyle";
                    lockedFileRowSelectedStyle.normal.background = hierarchyStyle.onFocused.background;
                    lockedFileRowSelectedStyle.normal.textColor = hierarchyStyle.onFocused.textColor;
                }
                return lockedFileRowSelectedStyle;
            }
        }

        public static GUIStyle GenericTableBoxStyle
        {
            get
            {
                if (genericTableBoxStyle == null)
                {
                    genericTableBoxStyle = new GUIStyle(GUI.skin.box);
                    //genericTableBoxStyle.name = "GenericTableBoxStyle";
                }
                return genericTableBoxStyle;
            }
        }

        public static GUIStyle HistoryDetailsTitleStyle
        {
            get
            {
                if (historyDetailsTitleStyle == null)
                {
                    historyDetailsTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                    //historyDetailsTitleStyle.name = "HistoryDetailsTitleStyle";
                    historyDetailsTitleStyle.wordWrap = true;
                }
                return historyDetailsTitleStyle;
            }
        }

        public static GUIStyle HistoryDetailsMetaInfoStyle
        {
            get
            {
                if (historyDetailsMetaInfoStyle == null)
                {
                    historyDetailsMetaInfoStyle = new GUIStyle(EditorStyles.miniLabel);
                    //historyDetailsMetaInfoStyle.name = "HistoryDetailsMetaInfoStyle";
                    historyDetailsMetaInfoStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
                    var c = EditorStyles.miniLabel.normal.textColor;
                    historyDetailsMetaInfoStyle.normal.textColor = new Color(c.r, c.g, c.b, c.a * 0.8f);

                    historyDetailsMetaInfoStyle.onNormal.background = Label.onNormal.background;
                    historyDetailsMetaInfoStyle.onNormal.textColor = Label.onNormal.textColor;
                    historyDetailsMetaInfoStyle.onFocused.background = Label.onFocused.background;
                    historyDetailsMetaInfoStyle.onFocused.textColor = Label.onFocused.textColor;
                }
                return historyDetailsMetaInfoStyle;
            }
        }

        public static GUIStyle LocksViewLockedByStyle
        {
            get
            {
                if (locksViewLockedByStyle == null)
                {
                    locksViewLockedByStyle = new GUIStyle(EditorStyles.miniLabel);
                    //locksViewLockedByStyle.name = "LocksViewLockedByStyle";
                    var hierarchyStyle = GUI.skin.FindStyle("PR Label");
                    locksViewLockedByStyle.onNormal.background = hierarchyStyle.onNormal.background;
                    locksViewLockedByStyle.onNormal.textColor = hierarchyStyle.onNormal.textColor;
                    locksViewLockedByStyle.onFocused.background = hierarchyStyle.onFocused.background;
                    locksViewLockedByStyle.onFocused.textColor = hierarchyStyle.onFocused.textColor;
                }
                return locksViewLockedByStyle;
            }
        }

        public static GUIStyle LocksViewLockedBySelectedStyle
        {
            get
            {
                if (locksViewLockedBySelectedStyle == null)
                {
                    locksViewLockedBySelectedStyle = new GUIStyle(EditorStyles.miniLabel);
                    //locksViewLockedBySelectedStyle.name = "LocksViewLockedBySelectedStyle";
                    var hierarchyStyle = GUI.skin.FindStyle("PR Label");
                    locksViewLockedBySelectedStyle.onNormal.textColor = hierarchyStyle.onNormal.textColor;
                    locksViewLockedBySelectedStyle.onNormal.background = hierarchyStyle.onFocused.background;
                    locksViewLockedBySelectedStyle.onNormal.textColor = hierarchyStyle.onNormal.textColor;
                    locksViewLockedBySelectedStyle.onFocused.background = hierarchyStyle.onFocused.background;
                    locksViewLockedBySelectedStyle.onFocused.textColor = hierarchyStyle.onNormal.textColor;
                    locksViewLockedBySelectedStyle.normal.background = hierarchyStyle.onFocused.background;
                    locksViewLockedBySelectedStyle.normal.textColor = hierarchyStyle.onNormal.textColor;
                }
                return locksViewLockedBySelectedStyle;
            }
        }

        public static GUIStyle CommitFileAreaStyle
        {
            get
            {
                if (commitFileAreaStyle == null)
                {
                    commitFileAreaStyle = new GUIStyle(GUI.skin.box);
                    //commitFileAreaStyle.name = "CommitFileAreaStyle";
                    commitFileAreaStyle.margin = new RectOffset(0, 0, 0, 0);
                    commitFileAreaStyle.padding = new RectOffset(0, 0, 2, 2);
                }
                return commitFileAreaStyle;
            }
        }

        public static GUIStyle CommitButtonStyle
        {
            get
            {
                if (commitButtonStyle == null)
                {
                    commitButtonStyle = new GUIStyle(GUI.skin.button);
                    //commitButtonStyle.name = "CommitButtonStyle";
                    commitButtonStyle.richText = true;
                    commitButtonStyle.wordWrap = true;
                }
                return commitButtonStyle;
            }
        }

        public static GUIStyle TextFieldStyle
        {
            get
            {
                if (textFieldStyle == null)
                {
                    textFieldStyle = new GUIStyle(GUI.skin.textField);
                    //textFieldStyle.name = "TextFieldStyle";
                    textFieldStyle.fixedHeight = 21;
                    textFieldStyle.padding = new RectOffset(HalfSpacing, HalfSpacing, 4, 0);
                }
                return textFieldStyle;
            }
        }

        public static GUIStyle ProgressAreaBackStyle
        {
            get
            {
                if (progressAreaBackStyle == null)
                {
                    progressAreaBackStyle = new GUIStyle(GUI.skin.FindStyle("ProgressBarBack"));
                    //progressAreaBackStyle.name = "ProgressAreaBackStyle";
                    //progressAreaBackStyle.normal.background = Utility.GetTextureFromColor(new Color(194f/255f, 194f/255f, 194f/255f));
                    progressAreaBackStyle.margin = new RectOffset(0, 0, 0, 0);
                    progressAreaBackStyle.padding = new RectOffset(0, 0, 0, 0);
                }
                return progressAreaBackStyle;
            }
        }

        public static GUIStyle CenteredLabel
        {
            get
            {
                if (centeredLabel == null)
                {
                    centeredLabel = new GUIStyle(EditorStyles.wordWrappedLabel);
                    centeredLabel.alignment = TextAnchor.MiddleCenter;
                }
                return centeredLabel;
            }
        }

        public static GUIStyle BoldCenteredLabel
        {
            get
            {
                if (boldCenteredLabel == null)
                {
                    boldCenteredLabel = new GUIStyle(EditorStyles.boldLabel);
                    //boldCenteredLabel.name = "BoldCenteredLabelStyle";
                    boldCenteredLabel.alignment = TextAnchor.MiddleCenter;
                    boldCenteredLabel.wordWrap = true;
                }
                return boldCenteredLabel;
            }
        }

        public static GUIStyle CommitDescriptionFieldStyle
        {
            get
            {
                if (commitDescriptionFieldStyle == null)
                {
                    commitDescriptionFieldStyle = new GUIStyle(GUI.skin.textArea);
                    //commitDescriptionFieldStyle.name = "CommitDescriptionFieldStyle";
                    commitDescriptionFieldStyle.padding = new RectOffset(HalfSpacing, HalfSpacing, HalfSpacing, HalfSpacing);
                    commitDescriptionFieldStyle.wordWrap = true;
                }
                return commitDescriptionFieldStyle;
            }
        }

        public static GUIStyle ToggleMixedStyle
        {
            get
            {
                if (toggleMixedStyle == null)
                {
                    toggleMixedStyle = GUI.skin.FindStyle("ToggleMixed");
                }
                return toggleMixedStyle;
            }
        }

        public static GUIStyle AuthHeaderBoxStyle
        {
            get
            {
                if (authHeaderBoxStyle == null)
                {
                    authHeaderBoxStyle = new GUIStyle(HeaderBoxStyle);
                    //authHeaderBoxStyle.name = "AuthHeaderBoxStyle";
                }
                return authHeaderBoxStyle;
            }
        }

        public static GUIStyle GenericBoxStyle
        {
            get
            {
                if (genericBoxStyle == null)
                {
                    genericBoxStyle = new GUIStyle();
                    genericBoxStyle.padding = new RectOffset(5, 5, 5, 5);
                }
                return genericBoxStyle;
            }
        }

        public static GUIStyle HyperlinkStyle
        {
            get
            {
                if (hyperlinkStyle == null)
                {
                    hyperlinkStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                    hyperlinkStyle.normal.textColor = new Color(0, 0, 0xEE);
                }
                return hyperlinkStyle;
            }
        }

        public static GUIStyle FileHistoryLogTitleStyle
        {
            get
            {
                if (fileHistoryLogTitleStyle == null)
                {
                    fileHistoryLogTitleStyle = new GUIStyle(EditorStyles.largeLabel);
                    //fileHistoryLogTitleStyle.name = "FileHistoryLogTitleStyle";
                    fileHistoryLogTitleStyle.margin = new RectOffset(0, 0, 0, 0);
                }
                return fileHistoryLogTitleStyle;
            }
        }

        public static Texture2D ActiveBranchIcon
        {
            get
            {
                return Utility.GetIcon(GetIconName("current-branch-indicator"));
            }
        }

        public static Texture2D BranchIcon
        {
            get
            {
                return Utility.GetIcon(GetIconName("branch"));
            }
        }

        public static Texture2D TrackingBranchIcon
        {
            get
            {
                return Utility.GetIcon("tracked-branch-indicator", "tracked-branch-indicator");
            }
        }

        public static Texture2D FavoriteIconOn
        {
            get
            {
                return Utility.GetIcon("favorite-branch-indicator", "favorite-branch-indicator");
            }
        }

        public static Texture2D FavoriteIconOff
        {
            get
            {
                return FolderIcon;
            }
        }

        public static Texture2D SmallLogo
        {
            get
            {
                return Utility.GetIcon(GetIconName("small-logo"));
            }
        }

        public static Texture2D BigLogo
        {
            get
            {
                return Utility.GetIcon(GetIconName("big-logo"));
            }
        }

        public static Texture2D MergeIcon
        {
            get
            {
                return Utility.GetIcon(GetIconName("git-merge"));
            }
        }

        public static Texture2D DotIcon
        {
            get
            {
                return Utility.GetIcon(GetIconName("dot"));
            }
        }

        public static Texture2D LocalCommitIcon
        {
            get
            {
                return Utility.GetIcon(GetIconName("local-commit"));
            }
        }

        public static Texture2D FolderIcon
        {
            get
            {
                return EditorGUIUtility.FindTexture("Folder Icon");
            }
        }

        public static Texture2D RepoIcon
        {
            get
            {
                return Utility.GetIcon(GetIconName("repo"));
            }
        }

        public static Texture2D LockIcon
        {
            get
            {
                return Utility.GetIcon(GetIconName("lock"));
            }
        }

        public static Texture2D EmptyStateInit
        {
            get
            {
                return Utility.GetIcon("empty-state-init");
            }
        }

        public static Texture2D DropdownListIcon
        {
            get
            {
                return Utility.GetIcon("dropdown-list-icon");
            }
        }

        public static Texture2D GlobeIcon
        {
            get
            {
                return Utility.GetIcon("globe", invertColors: Utility.IsDarkTheme);
            }
        }

        public static Texture2D SpinnerInside
        {
            get
            {
                return Utility.GetIcon("spinner-inside");
            }
        }

        public static Texture2D SpinnerOutside
        {
            get
            {
                return Utility.GetIcon("spinner-outside");
            }
        }

        public static Texture2D Code
        {
            get
            {
                return Utility.GetIcon("code");
            }
        }

        public static Texture2D Rocket
        {
            get
            {
                return Utility.GetIcon("rocket");
            }
        }

        public static Texture2D Merge
        {
            get
            {
                return Utility.GetIcon("merge");
            }
        }

        public static Texture2D SpinnerInsideInverted
        {
            get
            {
                return Utility.GetIcon("spinner-inside", invertColors: true);
            }
        }

        public static Texture2D SpinnerOutsideInverted
        {
            get
            {
                return Utility.GetIcon("spinner-outside", invertColors: true);
            }
        }

        public static Texture2D CodeInverted
        {
            get
            {
                return Utility.GetIcon("code", invertColors: true);
            }
        }

        public static Texture2D RocketInverted
        {
            get
            {
                return Utility.GetIcon("rocket", invertColors: true);
            }
        }

        public static Texture2D MergeInverted
        {
            get
            {
                return Utility.GetIcon("merge", invertColors: true);
            }
        }
        private static GUIStyle foldout;
        public static GUIStyle Foldout
        {
            get
            {
                if (foldout == null)
                {
                    foldout = new GUIStyle(EditorStyles.foldout);
                    //foldout.name = "CustomFoldout";

                    foldout.focused.textColor = Color.white;
                    foldout.onFocused.textColor = Color.white;
                    foldout.focused.background = foldout.active.background;
                    foldout.onFocused.background = foldout.onActive.background;
                }

                return foldout;
            }
        }

        private static GUIStyle treeNode;
        public static GUIStyle TreeNode
        {
            get
            {
                if (treeNode == null)
                {
                    treeNode = new GUIStyle(GUI.skin.label);
                    //treeNode.name = "Custom TreeNode";

                    var greyTexture = Utility.GetTextureFromColor(Color.gray);

                    treeNode.focused.background = greyTexture;
                    treeNode.focused.textColor = Color.white;
                }

                return treeNode;
            }
        }

        private static GUIStyle activeTreeNode;
        public static GUIStyle ActiveTreeNode
        {
            get
            {
                if (activeTreeNode == null)
                {
                    activeTreeNode = new GUIStyle(TreeNode);
                    //activeTreeNode.name = "Custom Active TreeNode";

                    activeTreeNode.fontStyle = FontStyle.Bold;
                }

                return activeTreeNode;
            }
        }

        private static GUIStyle focusedTreeNode;
        public static GUIStyle FocusedTreeNode
        {
            get
            {
                if (focusedTreeNode == null)
                {
                    focusedTreeNode = new GUIStyle(TreeNode);
                    //focusedTreeNode.name = "Custom Focused TreeNode";

                    var blueColor = new Color(62f / 255f, 125f / 255f, 231f / 255f);
                    var blueTexture = Utility.GetTextureFromColor(blueColor);

                    focusedTreeNode.focused.background = blueTexture;
                }

                return focusedTreeNode;
            }
        }

        private static GUIStyle focusedActiveTreeNode;
        public static GUIStyle FocusedActiveTreeNode
        {
            get
            {
                if (focusedActiveTreeNode == null)
                {
                    focusedActiveTreeNode = new GUIStyle(FocusedTreeNode);
                    //focusedActiveTreeNode.name = "Custom Focused Active TreeNode";

                    focusedActiveTreeNode.fontStyle = FontStyle.Bold;
                }

                return focusedActiveTreeNode;
            }
        }

        private static GUIStyle lockPathStyle;
        public static GUIStyle LockPathStyle
        {
            get
            {
                if (lockPathStyle == null)
                {
                    lockPathStyle = new GUIStyle(GUI.skin.label);
                    //lockPathStyle.name = "Custom LockPathStyle";

                    lockPathStyle.fontSize = 11;
                }

                return lockPathStyle;
            }
        }

        private static GUIStyle lockMetaDataStyle;
        public static GUIStyle LockMetaDataStyle
        {
            get
            {
                if (lockMetaDataStyle == null)
                {
                    lockMetaDataStyle = new GUIStyle(GUI.skin.label);
                    //lockMetaDataStyle.name = "Custom LockMetaDataStyle";

                    lockMetaDataStyle.fontSize = 10;
                }

                return lockMetaDataStyle;
            }
        }

        private static GUIStyle indentedBox;
        public static GUIStyle IndentedBox
        {
            get
            {
                if (indentedBox == null)
                {
                    indentedBox = new GUIStyle
                    {
                        padding = new RectOffset(FoldoutIndentation, 0, 0, 0),
                    };
                }

                return indentedBox;
            }
        }

        private static GUIStyle foldoutContent;
        public static GUIStyle FoldoutContent
        {
            get
            {
                if (foldoutContent == null)
                {
                    foldoutContent = new GUIStyle
                    {
                        padding = new RectOffset(FoldoutIndentation, FoldoutContentRightPadding, 0, FoldoutContentBottomPadding),
                    };
                }

                return foldoutContent;
            }
        }

        private static string GetIconName(string name) => Utility.IsDarkTheme ? name + "-light" : name;
    }

    public static partial class Controls
    {
        public static bool Foldout(bool value, string label)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            var ret = EditorGUILayout.Toggle(value, Styles.Foldout);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            var rect = GUILayoutUtility.GetLastRect();
            rect.x += 20;
            rect.width -= 20;

            EditorGUI.LabelField(rect, label, Styles.BoldLabel);

            return ret;
        }

        public static bool FoldoutScope(bool open, string title, Action draw)
        {
            var ret = Foldout(open, title);

            if (ret)
            {
                GUILayout.BeginVertical(Styles.FoldoutContent, GUILayout.ExpandWidth(true));
                draw();
                GUILayout.EndVertical();
            }

            return ret;
        }

        public static void DoControl<T>(T val, Func<T, T> draw, Action<T> update)
        {
            EditorGUI.BeginChangeCheck();
            T updated = draw(val);
            if (EditorGUI.EndChangeCheck())
            {
                update(updated);
            }
        }
    }
}
