using System;
using System.Text;
using Unity.Editor.Tasks;
using UnityEditor;
using UnityEngine;
using Unity.VersionControl.Git.Extensions;

namespace Unity.VersionControl.Git.UI
{
    using IO;

    [Serializable]
    class GitPathView : Subview
    {
        private const string GitInstallTitle = "Git installation";
        private const string PathToGit = "Path to Git";
        private const string PathToGitLfs = "Path to Git LFS";
        private const string GitPathSaveButton = "Save";
        private const string SetToBundledGitButton = "Set to bundled git";
        private const string FindSystemGitButton = "Find system git";
        private const string BrowseButton = "...";
        private const string GitInstallBrowseTitle = "Select executable";
        private const string ErrorInvalidPathMessage = "Invalid Path.";
        private const string ErrorInstallingInternalGit = "Error installing portable git.";
        private const string ErrorValidatingGitPath = "Error validating Git Path.";
        private const string ErrorGitNotFoundMessage = "Git not found.";
        private const string ErrorGitLfsNotFoundMessage = "Git LFS not found.";
        private const string ErrorMinimumGitVersionMessageFormat = "Git version {0} found. Git version {1} is required.";
        private const string ErrorMinimumGitLfsVersionMessageFormat = "Git LFS version {0} found. Git LFS version {1} is required.";

        [SerializeField] private string gitPath;
        [SerializeField] private string gitLfsPath;
        [SerializeField] private string errorMessage;
        [SerializeField] private bool resetToBundled;
        [SerializeField] private bool resetToSystem;
        [SerializeField] private bool changingManually;
        [SerializeField] private bool installSettingsHidden;

        [NonSerialized] private bool isBusy;
        [NonSerialized] private bool refresh;
        [NonSerialized] private GitInstaller.GitInstallationState installationState;
        [NonSerialized] private GitInstaller.GitInstallDetails installDetails;
        private GitInstaller.GitInstallDetails InstallDetails => installDetails ?? (installDetails = new GitInstaller.GitInstallDetails(Environment.UserCachePath, Environment));

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            refresh = true;
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        private void MaybeUpdateData()
        {
            if (refresh)
            {
                installationState = Environment.GitInstallationState;
                gitPath = installationState.GitExecutablePath;
                gitLfsPath = installationState.GitLfsExecutablePath;
                refresh = false;
            }
        }

        public override void OnGUI()
        {
            // Install path
            installSettingsHidden = !Controls.FoldoutScope(!installSettingsHidden, GitInstallTitle, () =>
            {
                EditorGUI.BeginDisabledGroup(IsBusy || Parent.IsBusy);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginHorizontal();
                        {
                            EditorGUI.BeginChangeCheck();
                            {
                                gitPath = EditorGUILayout.TextField(PathToGit, gitPath);
                                gitPath = gitPath != null ? gitPath.Trim() : gitPath;
                                if (GUILayout.Button(BrowseButton, EditorStyles.miniButton,
                                        GUILayout.Width(Styles.BrowseButtonWidth)))
                                {
                                    GUI.FocusControl(null);

                                    var newPath = EditorUtility.OpenFilePanel(GitInstallBrowseTitle,
                                        !String.IsNullOrEmpty(gitPath) ? gitPath.ToSPath().Parent : "",
                                        Environment.ExecutableExtension.TrimStart('.'));

                                    if (!string.IsNullOrEmpty(newPath))
                                    {
                                        gitPath = newPath.ToSPath().ToString();
                                    }
                                }
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                changingManually = ViewHasChanges;
                            }
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            EditorGUI.BeginChangeCheck();
                            {
                                gitLfsPath = EditorGUILayout.TextField(PathToGitLfs, gitLfsPath);
                                gitLfsPath = gitLfsPath != null ? gitLfsPath.Trim() : gitLfsPath;
                                if (GUILayout.Button(BrowseButton, EditorStyles.miniButton,
                                        GUILayout.Width(Styles.BrowseButtonWidth)))
                                {
                                    GUI.FocusControl(null);

                                    var newPath = EditorUtility.OpenFilePanel(GitInstallBrowseTitle,
                                        !String.IsNullOrEmpty(gitLfsPath) ? gitLfsPath.ToSPath().Parent : "",
                                        Environment.ExecutableExtension.TrimStart('.'));

                                    if (!string.IsNullOrEmpty(newPath))
                                    {
                                        gitLfsPath = newPath.ToSPath().ToString();
                                    }
                                }
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                changingManually = ViewHasChanges;
                                errorMessage = "";
                            }
                        }
                        GUILayout.EndHorizontal();

                    }
                    GUILayout.EndVertical();

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                    GUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(!changingManually && !resetToBundled && !resetToSystem);
                        {
                            if (GUILayout.Button(GitPathSaveButton, GUILayout.ExpandWidth(false)))
                            {
                                GUI.FocusControl(null);
                                isBusy = true;
                                ValidateAndSetGitInstallPath();
                            }
                        }
                        EditorGUI.EndDisabledGroup();

                        // disable the button if the paths are already pointing to the bundled git
                        // both on windows, only lfs on mac
                        EditorGUI.BeginDisabledGroup(
                            (!Environment.IsWindows || gitPath == InstallDetails.GitExecutablePath) &&
                            gitLfsPath == InstallDetails.GitLfsExecutablePath);
                        {
                            if (GUILayout.Button(SetToBundledGitButton, GUILayout.ExpandWidth(false)))
                            {
                                GUI.FocusControl(null);

                                if (Environment.IsWindows)
                                    gitPath = InstallDetails.GitExecutablePath;
                                gitLfsPath = InstallDetails.GitLfsExecutablePath;
                                resetToBundled = ViewHasChanges;
                                resetToSystem = false;
                                changingManually = false;
                                errorMessage = "";
                            }
                        }
                        EditorGUI.EndDisabledGroup();

                        //Find button - for attempting to locate a new install
                        if (GUILayout.Button(FindSystemGitButton, GUILayout.ExpandWidth(false)))
                        {
                            GUI.FocusControl(null);
                            isBusy = true;
                            TaskManager.With(() =>
                                {
                                    var gitInstaller = new GitInstaller(Platform);
                                    return gitInstaller.FindSystemGit(new GitInstaller.GitInstallationState());
                                }, "Locating git...")
                                .FinallyInUI((success, ex, state) =>
                                {
                                    if (success)
                                    {
                                        if (state.GitIsValid)
                                        {
                                            gitPath = state.GitExecutablePath;
                                        }

                                        if (state.GitLfsIsValid)
                                        {
                                            gitLfsPath = state.GitLfsExecutablePath;
                                        }
                                    }
                                    else
                                    {
                                        Logger.Error(ex);
                                    }

                                    isBusy = false;
                                    resetToBundled = false;
                                    resetToSystem = ViewHasChanges;
                                    changingManually = false;
                                    errorMessage = "";
                                    Redraw();
                                })
                                .Start();
                        }
                    }
                    GUILayout.EndHorizontal();

                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(errorMessage, Styles.ErrorLabel);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                EditorGUI.EndDisabledGroup();
            });
        }

        private void ValidateAndSetGitInstallPath()
        {
            if (resetToBundled)
            {
                TaskManager.With(() => {
                        var state = Environment.GitDefaultInstallation.GetDefaults();
                        var gitInstaller = new GitInstaller(Platform, state);
                        state = gitInstaller.RunSynchronously();
                        if (state.GitIsValid && state.GitLfsIsValid)
                        {
                            Manager.SetupGit(state);
                            Manager.RestartRepository();
                        }
                        return state;
                    }, "Setting up git... ")
                    .FinallyInUI((success, exception, state) =>
                    {
                        if (!success)
                        {
                            Logger.Error(exception, ErrorInstallingInternalGit);
                            errorMessage = ErrorValidatingGitPath;
                        }
                        else
                        {
                            refresh = true;
                        }
                        isBusy = false;
                        resetToBundled = false;
                        resetToSystem = false;
                        changingManually = false;
                        Redraw();
                    }).Start();
            }
            else
            {
                var newState = new GitInstaller.GitInstallationState();
                newState.GitExecutablePath = gitPath.ToSPath();
                newState.GitLfsExecutablePath = gitLfsPath.ToSPath();
                var installer = new GitInstaller(Platform, newState);
                installer.Progress(UpdateProgress);

                TaskManager.With(() =>
                    {
                        return installer.RunSynchronously();
                    })
                    .Then(state =>
                    {
                        if (state.GitIsValid && state.GitLfsIsValid)
                        {
                            Manager.SetupGit(state);
                            Manager.RestartRepository();
                        }
                        return state;
                    })
                    .FinallyInUI((success, ex, state) =>
                    {
                        if (!success)
                        {
                            Logger.Error(ex, ErrorValidatingGitPath);
                            return;
                        }

                        if (!state.GitIsValid || !state.GitLfsIsValid)
                        {
                            var errorMessageStringBuilder = new StringBuilder();
                            Logger.Warning(
                                "Software versions do not meet minimums Git:{0} (Minimum:{1}) GitLfs:{2} (Minimum:{3})",
                                state.GitVersion, Constants.MinimumGitVersion, state.GitLfsVersion,
                                Constants.MinimumGitLfsVersion);

                            if (state.GitVersion == TheVersion.Default)
                            {
                                errorMessageStringBuilder.Append(ErrorGitNotFoundMessage);
                            }
                            else if (state.GitLfsVersion == TheVersion.Default)
                            {
                                errorMessageStringBuilder.Append(ErrorGitLfsNotFoundMessage);
                            }
                            else
                            {
                                if (state.GitVersion < Constants.MinimumGitVersion)
                                {
                                    errorMessageStringBuilder.AppendFormat(ErrorMinimumGitVersionMessageFormat,
                                        state.GitVersion, Constants.MinimumGitVersion);
                                }

                                if (state.GitLfsVersion < Constants.MinimumGitLfsVersion)
                                {
                                    if (errorMessageStringBuilder.Length > 0)
                                    {
                                        errorMessageStringBuilder.Append(Environment.NewLine);
                                    }

                                    errorMessageStringBuilder.AppendFormat(ErrorMinimumGitLfsVersionMessageFormat,
                                        state.GitLfsVersion, Constants.MinimumGitLfsVersion);
                                }
                            }

                            errorMessage = errorMessageStringBuilder.ToString();
                        }
                        else
                        {
                            Logger.Trace("Software versions meet minimums Git:{0} GitLfs:{1}",
                                state.GitVersion,
                                state.GitLfsVersion);

                            refresh = true;
                        }
                        isBusy = false;
                        resetToBundled = false;
                        resetToSystem = false;
                        changingManually = false;
                        Redraw();

                    }).Start();
            }
        }

        public bool ViewHasChanges
        {
            get
            {
                return gitPath != installationState.GitExecutablePath || gitLfsPath != installationState.GitLfsExecutablePath;
            }
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}
