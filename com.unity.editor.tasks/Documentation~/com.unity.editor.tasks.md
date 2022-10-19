# About &lt;Unity Editor Tasks&gt;

Unity.Editor.Tasks is a TPL-based threading library that simplifies running asynchronous code with explicit thread and scheduler settings.

This repository is a subset of the functionality in the [Git for Unity](https://github.com/spoiledcat/git-for-unity) repository, specifically the `Threading`, `OutputProcessors`, `Tasks`, `Process` and `IO` directories, as well as various helper classes found in the [Api](https://github.com/spoiledcat/git-for-unity/tree/master/src/com.spoiledcat.git.api/Api) source directory.

It's been split up for easier testing and consumption by the Git package and any other packages, libraries or apps that wish to use it.

 It is essentially a wrapper of `System.Threading.Tasks.Task` objects, with custom schedulers providing easy execution of tasks in a variety of scenarios:

 - UI: tasks can be scheduled to the UI thread, which uses `EditorApplication.delayCall`

 - Exclusive/Concurrent: A pair of synchronized schedulers allow for a one writer/many readers scenario, where any task with an Exclusive affinity is guaranteed to run on its own without any other Exclusive or Concurrent affinity tasks executing at the same time. Concurrent affinity tasks can run with other tasks any affinity except Exclusive.
  
  This allows tasks to safely execute code that requires locking resources without worrying about other threads touching the same resources.

- None: tasks run on the default scheduler (threadpool) without constraints.
- LongRunning: tasks run on the default scheduler (threadpool), but the task management system doesn't expect them to finish in a short time and doesn't impose task timeouts (if the task supports such a thing);

It provides easy chaining of tasks with a rx.net-like API that allows data flow from one task to the other, progress reporting, catch and finally handlers, support for wrapping async/await methods and for ensuring they run outside of play mode, and ready-made tasks for running processes and processing/streaming their output.

Standard .NET async methods can be integrated into this library and executed with specific affinities using the `TPLTask` class. Similarly, going from an Editor Task to the async/await model is just a matter of awaiting the underlying `Task` property.


## Usage examples

There are a number of tests in `src/com.unity.editor.tasks/Tests/Editor` that can provide useful guidance.

### Downloading a bunch of things in the background

```
// you'll want to keep one instance of this around.
// Initialization must happen on the UI thread so it knows how to schedule things to it
var taskManager = new TaskManager.Initialize();

// the Downloader is a TaskQueue-type task which handles firing up a series of concurrent tasks,
// aggregating all of the data from each of the tasks and returning it all together
var downloader = new Downloader(taskManager);
downloader.QueueDownload("http://something", "directory/to/store/file", retryCount: 2);

downloader.Progress(progress => { ShowProgress(progress.Message, progress.InnerProgress?.Message, progress.Percentage); });

downloader.OnStart += __ => logger.Info("Downloading assets...");
downloader.OnEnd += (___, __, success, ex) => logger.Info($"Downloader is done with result: {success}");

downloader.FinallyInUI((success, exception, results) => {
  // do something with all the things that were downloaded
});

downloader.Start();

```


 ### Chaining tasks

```
void ShowProgress(string title, string message, float pct) => TaskManager.RunInUI(() => EditorUtility.DisplayProgressBar(title, message, pct), "Updating progress");

// you'll want to keep one instance of this around.
// Initialization must happen on the UI thread so it knows how to schedule things to it
var taskManager = new TaskManager.Initialize();

EditorUtility.DisplayProgressBar("Starting", "", 0);
var chainOfTasks = new FuncTask(taskManager, () => "Do something critical and return a string.", affinity: TaskAffinity.Exclusive)

  // each task has its own progress event/handler.
  .Progress(progress => ShowProgress(progress.Message, progress.InnerProgress?.Message, progress.Percentage))

  // do something with the value the previous task produced, with Concurrent affinity.
  // This won't run if the previous one failed
  .Then(str => str.ToUpper())

  .Progress(progress => ShowProgress(progress.Message, progress.InnerProgress?.Message, progress.Percentage))

  // finally handlers will always be called. Always end a chain with a Finally* handler!
  .FinallyInUI((success, exception, value) => {
    EditorUtility.ClearProgressBar();

    if (success) {
      // do something on success
      EditorUtility.DisplayDialog("All done", value);
    } else {
      Debug.LogException(exception);
    }
  });

// start executing the whole thing
chainOfTasks.Start();

```

## The History

This library was originally written because Unity's old Mono C# profile/compilers did not support TPL and async/await, the Git client really needs to run on controlled background threads with some sort of exclusive locking mechanism, without the uncertainty of explicit async/await calls, and I really didn't have the time or the inclination to teach modern .NET developers how to code for an ancient Mono version.

The next best thing was to code modern .NET and use a version of the TPL library backported to .NET 3.5 (the highest that Unity's old mono supports) to have it running in Unity 5.6 and up. The nice thing about modern .NET is that it's pretty much all syntactic sugar. Ancient Mono versions can run the code just fine, they just can't compile it.

These days, Unity supports modern .NET and can compile all this code just fine, so this library no longer ships with .NET 3.5 support, but it still maintains its separation from Unity - the projects don't reference Unity, and any Unity integration code in this library is behind a `#if UNITY_EDITOR` define, so you can safely consume the nuget packages in any .NET environment, and Unity-specific functionality will only be available when you consume this library as a package in a Unity project.
