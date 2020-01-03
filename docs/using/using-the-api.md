# Using the API

Git for Unity provides access to a git client to help users create their own tools to assist in their workflow.

Users can separate the user interface from the API by removing `GitForUnity.dll`. All other libraries are required by the API.

## Creating an instance of `GitClient`
```cs
var defaultEnvironment = new DefaultEnvironment();
defaultEnvironment.Initialize(null, NPath.Default, NPath.Default, NPath.Default, Application.dataPath.ToNPath());

var processEnvironment = new ProcessEnvironment(defaultEnvironment);
var processManager = new ProcessManager(defaultEnvironment, processEnvironment, TaskManager.Instance.Token);

var gitClient = new GitClient(defaultEnvironment, processManager, TaskManager.Instance.Token);
```

## Full Example
This example creates a window that has a single button which commits all changes.
```cs
using System;
using System.Globalization;
using Unity.VersionControl.Git;
using UnityEditor;
using UnityEngine;

public class CustomGitEditor : EditorWindow
{
    [MenuItem("Window/Custom Git")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(CustomGitEditor));
    }

    [NonSerialized] private IPlatform platform;

    public void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (platform != null) return;

        Debug.Log("Init GitClient");

        var extensionInstallPath = ;
        var env = new ApplicationEnvironment(TheEnvironment.instance.Environment.ApplicationName);
        platform = new Platform(env);

        env.Initialize(Application.dataPath.ToSPath().Parent, TheEnvironment.instance.Environment);
        platform.Initialize();
        env.InitializeRepository();
    }

    void OnGUI()
    {
        GUILayout.Label("Custom Git Window", EditorStyles.boldLabel);

        if (GUILayout.Button("Commit Stuff"))
        {
            var message = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            var body = string.Empty;

            gitClient.AddAll(platform.TaskManager)
                .Then(gitClient.Commit(message, body))
                .Start();
        }
    }
}
```


