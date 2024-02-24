using System;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.VersionControl.Git.UI
{
    [Serializable]
    class SettingsView : Subview
    {
        private const string GitRepositoryTitle = "Repository Configuration";

        private const string GitRepositoryRemoteLabel = "Remote";
        private const string GitRepositorySave = "Save Repository";

        private const string GeneralSettingsTitle = "General";

        private const string WebTimeoutLabel = "Timeout of web requests";
        private const string GitTimeoutLabel = "Timeout of git commands";

        private const string DebugSettingsTitle = "Debug";
        private const string EnableTraceLoggingLabel = "Enable Trace Logging";

        private const string UISceneHierarchySettingsTitle = "UI - Scene Hierarchy";
        private const string UIProjectViewSettingsTitle = "UI - Project Window";
        private const string IconsEnabledToggleLabel = "Show Git Status Icons";
        private const string HierarchyIconsIndentToggleLabel = "Align to end of label";
        private const string HierarchyIconsIndentToggleTooltip = "You probably don't want this";
        private static GUIContent hierarchyIconsIndentToggleContent;
        private static GUIContent HierarchyIconsIndentToggleContent => hierarchyIconsIndentToggleContent ?? (hierarchyIconsIndentToggleContent = new GUIContent(HierarchyIconsIndentToggleLabel, HierarchyIconsIndentToggleTooltip));

        private const string HierarchyIconsOffsetLabel = "Offset";
        private const string HierarchyIconsOffsetRightTooltip = "Offset from the right edge of the hierarchy window. Increase this value to move icons away from the edge.";
        private const string HierarchyIconsOffsetLeftTooltip = "Offset from the left edge of the hierarchy window.";
        private static GUIContent hierarchyIconsOffsetRightContent;
        private static GUIContent HierarchyIconsOffsetRightContent => hierarchyIconsOffsetRightContent ?? (hierarchyIconsOffsetRightContent = new GUIContent(HierarchyIconsOffsetLabel, HierarchyIconsOffsetRightTooltip));
        private static GUIContent hierarchyIconsOffsetLeftContent;
        private static GUIContent HierarchyIconsOffsetLeftContent => hierarchyIconsOffsetLeftContent ?? (hierarchyIconsOffsetLeftContent = new GUIContent(HierarchyIconsOffsetLabel, HierarchyIconsOffsetLeftTooltip));

        private const string HierarchyIconsAlignmentLabel = "Align icons to";
        private const string HierarchyIconsAlignmentTooltip = "Align the icons to the left or right of the hiearchy entry. Note that the icons will visually overlap the scene object buttons when aligned on the left, but the buttons will still work.";
        private static GUIContent hierarchyIconsAlignmentContent;
        private static GUIContent HierarchyIconsAlignmentContent => hierarchyIconsAlignmentContent ?? (hierarchyIconsAlignmentContent = new GUIContent(HierarchyIconsAlignmentLabel, HierarchyIconsAlignmentTooltip));

        private const string DefaultRepositoryRemoteName = "origin";

        [NonSerialized] private bool currentRemoteHasUpdate;
        [NonSerialized] private bool isBusy;

        [SerializeField] private GitPathView gitPathView = new GitPathView();
        [SerializeField] private bool hasRemote;
        [SerializeField] private CacheUpdateEvent lastCurrentRemoteChangedEvent;
        [SerializeField] private string newRepositoryRemoteUrl;
        [SerializeField] private string repositoryRemoteName;
        [SerializeField] private string repositoryRemoteUrl;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private UserSettingsView userSettingsView = new UserSettingsView();

        [SerializeField] private bool repositorySettingsHidden;
        [SerializeField] private bool generalSettingsHidden;
        [SerializeField] private bool debugSettingsHidden;
        [SerializeField] private bool uiSceneSettingsHidden;
        [SerializeField] private bool uiProjectSettingsHidden;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            gitPathView.InitializeView(this);
            userSettingsView.InitializeView(this);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            gitPathView.OnEnable();
            userSettingsView.OnEnable();
            AttachHandlers(Repository);

            if (Repository != null)
            {
                ValidateCachedData(Repository);
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            gitPathView.OnDisable();
            userSettingsView.OnDisable();
            DetachHandlers(Repository);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            userSettingsView.OnDataUpdate();
            gitPathView.OnDataUpdate();

            MaybeUpdateData();
        }

        public override void Refresh()
        {
            base.Refresh();
            gitPathView.Refresh();
            userSettingsView.Refresh();
            Refresh(CacheType.RepositoryInfo);
        }

        public override void OnGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            {
                userSettingsView.OnGUI();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                if (Repository != null)
                {
                    OnRepositorySettingsGUI();
                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                }

                gitPathView.OnGUI();
                OnGeneralSettingsGui();
                OnLoggingSettingsGui();
                OnUISettingsGui();
            }

            GUILayout.EndScrollView();

            DoProgressGUI();
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.CurrentRemoteChanged += RepositoryOnCurrentRemoteChanged;
        }

        private void RepositoryOnCurrentRemoteChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCurrentRemoteChangedEvent.Equals(cacheUpdateEvent))
            {
                lastCurrentRemoteChangedEvent = cacheUpdateEvent;
                currentRemoteHasUpdate = true;
                Redraw();
            }
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.CurrentRemoteChanged -= RepositoryOnCurrentRemoteChanged;
        }

        private void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.RepositoryInfo, lastCurrentRemoteChangedEvent);
        }

        private void MaybeUpdateData()
        {
            if (Repository == null)
                return;

            if (currentRemoteHasUpdate)
            {
                currentRemoteHasUpdate = false;
                var currentRemote = Repository.CurrentRemote;
                hasRemote = currentRemote.HasValue && !String.IsNullOrEmpty(currentRemote.Value.Url);
                if (!hasRemote)
                {
                    repositoryRemoteName = DefaultRepositoryRemoteName;
                    newRepositoryRemoteUrl = repositoryRemoteUrl = string.Empty;
                }
                else
                {
                    repositoryRemoteName = currentRemote.Value.Name;
                    newRepositoryRemoteUrl = repositoryRemoteUrl = currentRemote.Value.Url;
                }
            }
        }

        private void OnRepositorySettingsGUI()
        {
            repositorySettingsHidden = !Controls.FoldoutScope(!repositorySettingsHidden, GitRepositoryTitle, () =>
            {
                EditorGUI.BeginDisabledGroup(IsBusy);
                {
                    newRepositoryRemoteUrl = EditorGUILayout.TextField(GitRepositoryRemoteLabel + ": " + repositoryRemoteName, newRepositoryRemoteUrl);
                    var needsSaving = newRepositoryRemoteUrl != repositoryRemoteUrl && !String.IsNullOrEmpty(newRepositoryRemoteUrl);

                    EditorGUI.BeginDisabledGroup(!needsSaving);
                    {
                        if (GUILayout.Button(GitRepositorySave, GUILayout.ExpandWidth(false)))
                        {
                            try
                            {
                                isBusy = true;
                                Repository.SetupRemote(repositoryRemoteName, newRepositoryRemoteUrl)
                                    .FinallyInUI((_, __) =>
                                    {
                                        isBusy = false;
                                        Redraw();
                                    })
                                    .Start();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                            }
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.EndDisabledGroup();
            });
        }

        private void OnLoggingSettingsGui()
        {
            debugSettingsHidden = !Controls.FoldoutScope(!debugSettingsHidden, DebugSettingsTitle, () =>
            {
                Controls.DoControl(LogHelper.TracingEnabled,
                    value => EditorGUILayout.Toggle(EnableTraceLoggingLabel, value),
                    value =>
                    {
                        LogHelper.TracingEnabled = value;
                        Manager.UserSettings.Set(Constants.TraceLoggingKey, value);
                    });
            });
        }

        private void OnGeneralSettingsGui()
        {
            generalSettingsHidden = !Controls.FoldoutScope(!generalSettingsHidden, GeneralSettingsTitle, () =>
            {
                Controls.DoControl(ApplicationConfiguration.WebTimeout,
                    value => EditorGUILayout.IntField(WebTimeoutLabel, value),
                    value =>
                    {
                        ApplicationConfiguration.WebTimeout = value;
                        Manager.UserSettings.Set(Constants.WebTimeoutKey, value);
                    });

                Controls.DoControl(ApplicationConfiguration.GitTimeout,
                    value => EditorGUILayout.IntField(GitTimeoutLabel, value),
                    value =>
                    {
                        ApplicationConfiguration.GitTimeout = value;
                        Manager.UserSettings.Set(Constants.GitTimeoutKey, value);
                    });
            });
        }

        private void OnUISettingsGui()
        {
            bool dirty = false;

            uiSceneSettingsHidden = !Controls.FoldoutScope(!uiSceneSettingsHidden, UISceneHierarchySettingsTitle, () =>
            {
                Controls.DoControl(ApplicationConfiguration.HierarchyIconsEnabled,
                    value => EditorGUILayout.Toggle(IconsEnabledToggleLabel, value),
                    value =>
                    {
                        ApplicationConfiguration.HierarchyIconsEnabled = value;
                        Manager.UserSettings.Set(Constants.HierarchyIconsEnabledKey, value);
                        dirty = true;
                    });

                Controls.DoControl(ApplicationConfiguration.HierarchyIconsAlignment,
                    value => (ApplicationConfiguration.HierarchyIconAlignment) EditorGUILayout.EnumPopup(HierarchyIconsAlignmentContent, value),
                    value =>
                    {
                        ApplicationConfiguration.HierarchyIconsAlignment = value;
                        Manager.UserSettings.Set(Constants.HierarchyIconsAlignmentKey, value);
                        dirty = true;
                    });

                if (ApplicationConfiguration.HierarchyIconsAlignment == ApplicationConfiguration.HierarchyIconAlignment.Right)
                {
                    Controls.DoControl(ApplicationConfiguration.HierarchyIconsIndented,
                        value => EditorGUILayout.Toggle(HierarchyIconsIndentToggleContent, value),
                        value =>
                        {
                            ApplicationConfiguration.HierarchyIconsIndented = value;
                            Manager.UserSettings.Set(Constants.HierarchyIconsIndentedKey, value);
                            dirty = true;
                        });

                    Controls.DoControl(ApplicationConfiguration.HierarchyIconsOffsetRight,
                        value => EditorGUILayout.IntSlider(HierarchyIconsOffsetRightContent, value, 0, 200),
                        value =>
                        {
                            ApplicationConfiguration.HierarchyIconsOffsetRight = value;
                            Manager.UserSettings.Set(Constants.HierarchyIconsOffsetRightKey, value);
                            dirty = true;
                        });
                }
                else
                {
                    Controls.DoControl(ApplicationConfiguration.HierarchyIconsOffsetLeft,
                        value => EditorGUILayout.IntSlider(HierarchyIconsOffsetLeftContent, value, -16, 16),
                        value =>
                        {
                            ApplicationConfiguration.HierarchyIconsOffsetLeft = value;
                            Manager.UserSettings.Set(Constants.HierarchyIconsOffsetLeftKey, value);
                            dirty = true;
                        });
                }
            });

            if (dirty)
            {
                EditorApplication.RepaintHierarchyWindow();
            }

            dirty = false;

            uiProjectSettingsHidden = !Controls.FoldoutScope(!uiProjectSettingsHidden, UIProjectViewSettingsTitle, () =>
            {
                Controls.DoControl(ApplicationConfiguration.ProjectIconsEnabled,
                    value => EditorGUILayout.Toggle(IconsEnabledToggleLabel, value),
                    value =>
                    {
                        ApplicationConfiguration.ProjectIconsEnabled = value;
                        Manager.UserSettings.Set(Constants.ProjectIconsEnabledKey, value);
                        dirty = true;
                    });

                if (dirty)
                {
                    EditorApplication.RepaintProjectWindow();
                }
            });
        }

        public override bool IsBusy => isBusy || userSettingsView.IsBusy || gitPathView.IsBusy;
    }
}
