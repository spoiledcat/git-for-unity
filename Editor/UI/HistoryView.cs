using System;
using System.Collections.Generic;
using System.Linq;
using SpoiledCat.Git;
using Unity.Editor.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.VersionControl.Git.UI
{
    using IO;

    abstract class HistoryBase : Subview
    {
        protected const string CommitDetailsTitle = "Commit details";
        protected const string ClearSelectionButton = "×";

        protected abstract HistoryControl HistoryControl { get; set; }
        protected abstract GitLogEntry SelectedEntry { get; set; }
        protected abstract ChangesTree TreeChanges { get; set; }
        protected abstract Vector2 DetailsScroll { get; set; }

        protected void BuildHistoryControl(int loadAhead, List<GitLogEntry> gitLogEntries)
        {
            if (HistoryControl == null)
            {
                HistoryControl = new HistoryControl();
            }

            HistoryControl.Load(loadAhead, gitLogEntries);
            if (!SelectedEntry.Equals(GitLogEntry.Default)
                && SelectedEntry.CommitID != HistoryControl.SelectedGitLogEntry.CommitID)
            {
                SelectedEntry = GitLogEntry.Default;
            }
        }

        protected void BuildTreeChanges()
        {
            TreeChanges.PathSeparator = SPath.FileSystem.DirectorySeparatorChar.ToString();
            TreeChanges.Load(SelectedEntry.changes.Select(entry => new GitStatusEntryTreeData(entry)));
            Redraw();
        }

        protected void RevertCommit()
        {
            var dialogTitle = "Revert commit";
            var dialogBody = string.Format(@"Are you sure you want to revert the following commit:""{0}""", SelectedEntry.Summary);

            if (EditorUtility.DisplayDialog(dialogTitle, dialogBody, "Revert", "Cancel"))
            {
                Repository
                    .Revert(SelectedEntry.CommitID)
                    .FinallyInUI((success, e) => {
                        if (!success)
                        {
                            EditorUtility.DisplayDialog(dialogTitle,
                                "Error reverting commit: " + e.Message, Localization.Cancel);
                        }
                        AssetDatabase.Refresh();
                    })
                    .Start();
            }
        }

        protected void HistoryDetailsEntry(GitLogEntry entry)
        {
            GUILayout.BeginVertical(Styles.HeaderBoxStyle);
            GUILayout.Label(entry.Summary, Styles.HistoryDetailsTitleStyle);

            GUILayout.Space(-3);

            GUILayout.BeginHorizontal();
            GUILayout.Label(entry.PrettyTimeString, Styles.HistoryDetailsMetaInfoStyle);
            GUILayout.Label(entry.AuthorName, Styles.HistoryDetailsMetaInfoStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(3);
            GUILayout.EndVertical();
        }

        protected void DoHistoryGui(Rect rect, Action<GitLogEntry> historyControlRightClick = null,
            Action<ChangesTreeNode> changesTreeRightClick = null)
        {
            if (HistoryControl != null)
            {
                var historyControlRect = new Rect(0f, 0f, Position.width, Position.height - rect.height);

                var requiresRepaint = HistoryControl.Render(historyControlRect, HasFocus,
                    singleClick: entry => {
                        SelectedEntry = entry;
                        BuildTreeChanges();
                    },
                    doubleClick: entry => {

                    },
                    rightClick: historyControlRightClick);

                if (requiresRepaint)
                    Redraw();
            }

            DoProgressGUI();

            if (!SelectedEntry.Equals(GitLogEntry.Default))
            {
                // Top bar for scrolling to selection or clearing it
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    if (GUILayout.Button(CommitDetailsTitle, Styles.ToolbarButtonStyle))
                    {
                        HistoryControl.ScrollTo(HistoryControl.SelectedIndex);
                    }

                    if (GUILayout.Button(ClearSelectionButton, Styles.ToolbarButtonStyle, GUILayout.ExpandWidth(false)))
                    {
                        SelectedEntry = GitLogEntry.Default;
                        HistoryControl.SelectedIndex = -1;
                    }
                }
                GUILayout.EndHorizontal();

                // Log entry details - including changeset tree (if any changes are found)
                DetailsScroll = GUILayout.BeginScrollView(DetailsScroll, GUILayout.Height(250));
                {
                    HistoryDetailsEntry(SelectedEntry);

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    GUILayout.Label("Files changed", EditorStyles.boldLabel);
                    GUILayout.Space(-5);

                    rect = GUILayoutUtility.GetLastRect();
                    GUILayout.BeginHorizontal(Styles.HistoryFileTreeBoxStyle);
                    GUILayout.BeginVertical();
                    {
                        var borderLeft = Styles.Label.margin.left;
                        var treeControlRect = new Rect(rect.x + borderLeft, rect.y, Position.width - borderLeft * 2,
                            Position.height - rect.height + Styles.CommitAreaPadding);
                        var treeRect = new Rect(0f, 0f, 0f, 0f);
                        if (TreeChanges != null)
                        {
                            treeRect = TreeChanges.Render(treeControlRect, DetailsScroll,
                                singleClick: node => { },
                                doubleClick: node => { },
                                rightClick: changesTreeRightClick);

                            if (TreeChanges.RequiresRepaint)
                                Redraw();
                        }

                        GUILayout.Space(treeRect.y - treeControlRect.y);
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                }
                GUILayout.EndScrollView();
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            AttachHandlers(Repository);
            ValidateCachedData(Repository);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DetachHandlers(Repository);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        public override void OnFocusChanged()
        {
            base.OnFocusChanged();
            var hasFocus = HasFocus;
            if (TreeChanges.ViewHasFocus != hasFocus)
            {
                TreeChanges.ViewHasFocus = hasFocus;
                Redraw();
            }
        }

        protected abstract void AttachHandlers(IRepository repository);
        protected abstract void DetachHandlers(IRepository repository);
        protected abstract void ValidateCachedData(IRepository repository);
        protected virtual void MaybeUpdateData()
        {
            if (FirstRender && TreeChanges != null)
            {
                    TreeChanges.ViewHasFocus = HasFocus;
            }

            TreeChanges?.UpdateIcons(Styles.FolderIcon);
        }
    }

    [Serializable]
    class HistoryView : HistoryBase
    {
        [SerializeField] private bool currentLogHasUpdate;
        [SerializeField] private bool currentTrackingStatusHasUpdate;

        [SerializeField] private List<GitLogEntry> logEntries = new List<GitLogEntry>();

        [SerializeField] private int statusAhead;

        [SerializeField] private CacheUpdateEvent lastLogChangedEvent;
        [SerializeField] private CacheUpdateEvent lastTrackingStatusChangedEvent;

        [SerializeField] private HistoryControl historyControl;
        [SerializeField] private GitLogEntry selectedEntry = GitLogEntry.Default;
        [SerializeField] private ChangesTree treeChanges = new ChangesTree { DisplayRootNode = false };
        [SerializeField] private Vector2 detailsScroll;

        public override void Refresh()
        {
            base.Refresh();
            Refresh(CacheType.GitLog);
            Refresh(CacheType.GitAheadBehind);
        }

        private void RepositoryOnTrackingStatusChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastTrackingStatusChangedEvent.Equals(cacheUpdateEvent))
            {
                ReceivedEvent(cacheUpdateEvent.cacheType);
                lastTrackingStatusChangedEvent = cacheUpdateEvent;
                currentTrackingStatusHasUpdate = true;
                Redraw();
            }
        }

        private void RepositoryOnLogChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLogChangedEvent.Equals(cacheUpdateEvent))
            {
                ReceivedEvent(cacheUpdateEvent.cacheType);
                lastLogChangedEvent = cacheUpdateEvent;
                currentLogHasUpdate = true;
                Redraw();
            }
        }

        protected override void AttachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.TrackingStatusChanged += RepositoryOnTrackingStatusChanged;
            repository.LogChanged += RepositoryOnLogChanged;
        }

        protected override void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.TrackingStatusChanged -= RepositoryOnTrackingStatusChanged;
            repository.LogChanged -= RepositoryOnLogChanged;
        }

        protected override void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitLog, lastLogChangedEvent);
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitAheadBehind, lastTrackingStatusChangedEvent);
        }

        protected override void MaybeUpdateData()
        {
            if (TreeChanges != null)
            {
                TreeChanges.FolderStyle = Styles.Foldout;
                TreeChanges.TreeNodeStyle = Styles.TreeNode;
                TreeChanges.ActiveTreeNodeStyle = Styles.ActiveTreeNode;
                TreeChanges.FocusedTreeNodeStyle = Styles.FocusedTreeNode;
                TreeChanges.FocusedActiveTreeNodeStyle = Styles.FocusedActiveTreeNode;
                TreeChanges.UpdateIcons(Styles.FolderIcon);
            }

            if (Repository == null)
            {
                return;
            }

            if (currentTrackingStatusHasUpdate)
            {
                currentTrackingStatusHasUpdate = false;

                statusAhead = Repository.CurrentAhead;
            }

            if (currentLogHasUpdate)
            {
                currentLogHasUpdate = false;

                logEntries = Repository.CurrentLog;

                BuildHistoryControl(statusAhead, logEntries);
            }
        }

        public override void OnGUI()
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            DoHistoryGui(lastRect, entry => {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Revert"), false, RevertCommit);
                menu.ShowAsContext();
            }, node => {
                var menu = CreateChangesTreeContextMenu(node);
                menu.ShowAsContext();
            });
        }

        protected override HistoryControl HistoryControl
        {
            get { return historyControl; }
            set { historyControl = value; }
        }

        protected override GitLogEntry SelectedEntry
        {
            get { return selectedEntry; }
            set { selectedEntry = value; }
        }

        protected override ChangesTree TreeChanges
        {
            get { return treeChanges; }
            set { treeChanges = value; }
        }

        protected override Vector2 DetailsScroll
        {
            get { return detailsScroll; }
            set { detailsScroll = value; }
        }

        private GenericMenu CreateChangesTreeContextMenu(ChangesTreeNode node)
        {
            var genericMenu = new GenericMenu();

            genericMenu.AddItem(new GUIContent("Show History"), false, () => { });

            return genericMenu;
        }
    }

    [Serializable]
    class FileHistoryView : HistoryBase
    {
        [SerializeField] private bool currentFileLogHasUpdate;
        [SerializeField] private bool currentStatusEntriesHasUpdate;

        [SerializeField] private GitFileLog gitFileLog;

        [SerializeField] private HistoryControl historyControl;
        [SerializeField] private GitLogEntry selectedEntry = GitLogEntry.Default;
        [SerializeField] private ChangesTree treeChanges = new ChangesTree { DisplayRootNode = false };
        [SerializeField] private Vector2 detailsScroll;
        [SerializeField] private List<GitStatusEntry> gitStatusEntries = new List<GitStatusEntry>();

        [SerializeField] private CacheUpdateEvent lastStatusEntriesChangedEvent;
        [SerializeField] private CacheUpdateEvent lastFileLogChangedEvent;

        public override void Refresh()
        {
            base.Refresh();
            Refresh(CacheType.GitLog);
            Refresh(CacheType.GitAheadBehind);
        }

        private void RepositoryOnFileLogChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastFileLogChangedEvent.Equals(cacheUpdateEvent))
            {
                ReceivedEvent(cacheUpdateEvent.cacheType);
                lastFileLogChangedEvent = cacheUpdateEvent;
                currentFileLogHasUpdate = true;
                Redraw();
            }
        }

        private void RepositoryOnStatusEntriesChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastStatusEntriesChangedEvent.Equals(cacheUpdateEvent))
            {
                ReceivedEvent(cacheUpdateEvent.cacheType);
                lastStatusEntriesChangedEvent = cacheUpdateEvent;
                currentStatusEntriesHasUpdate = true;
                Redraw();
            }
        }

        protected override void AttachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.FileLogChanged += RepositoryOnFileLogChanged;
            repository.StatusEntriesChanged += RepositoryOnStatusEntriesChanged;
        }

        protected override void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.FileLogChanged -= RepositoryOnFileLogChanged;
            repository.FileLogChanged -= RepositoryOnStatusEntriesChanged;
        }

        protected override void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitFileLog, lastFileLogChangedEvent);
        }

        protected override void MaybeUpdateData()
        {
            if (Repository == null)
            {
                return;
            }

            if (currentFileLogHasUpdate)
            {
                currentFileLogHasUpdate = false;

                gitFileLog = Repository.CurrentFileLog;

                BuildHistoryControl(0, gitFileLog.LogEntries);
            }

            if (currentStatusEntriesHasUpdate)
            {
                currentStatusEntriesHasUpdate = false;

                gitStatusEntries = Repository.CurrentChanges;
            }
        }

        public override void OnGUI()
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            DoHistoryGui(lastRect, entry => {
                GenericMenu menu = new GenericMenu();
                string checkoutPrompt = string.Format("Checkout revision {0}", entry.ShortID);
                menu.AddItem(new GUIContent(checkoutPrompt), false, () => Checkout(entry.commitID));
                menu.ShowAsContext();
            }, node => {
            });
        }

        protected override HistoryControl HistoryControl
        {
            get { return historyControl; }
            set { historyControl = value; }
        }

        protected override GitLogEntry SelectedEntry
        {
            get { return selectedEntry; }
            set { selectedEntry = value; }
        }

        protected override ChangesTree TreeChanges
        {
            get { return treeChanges; }
            set { treeChanges = value; }
        }

        protected override Vector2 DetailsScroll
        {
            get { return detailsScroll; }
            set { detailsScroll = value; }
        }

        private const string ConfirmCheckoutTitle = "Discard Changes?";
        private const string ConfirmCheckoutMessage = "You've made changes to file '{0}'.  Overwrite these changes with the historical version?";
        private const string ConfirmCheckoutOK = "Overwrite";
        private const string ConfirmCheckoutCancel = "Cancel";

        protected void Checkout(string commitId)
        {
            var promptUser = gitStatusEntries.Count > 0 && gitStatusEntries.Any(statusEntry => gitFileLog.Path.Equals(statusEntry.Path.ToSPath()));

            if (!promptUser || EditorUtility.DisplayDialog(ConfirmCheckoutTitle, string.Format(ConfirmCheckoutMessage, gitFileLog.Path), ConfirmCheckoutOK, ConfirmCheckoutCancel))
            {
                Repository.CheckoutVersion(commitId, new string[] { gitFileLog.Path })
                          .ThenInUI(AssetDatabase.Refresh)
                          .Start();
            }
        }
    }
}
