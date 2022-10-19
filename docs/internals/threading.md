# Threading, what is even

## Important considerations

There are two parts to this codebase - `com.spoiledcat.git.api`, which is the base API that includes the low level git client api plus higher level abstractions like `Repository` on top of it; and `com.spoiledcat.git.ui`, which is the UI code.

It's important to understand that the API library is a pure .NET library with no dependencies on Unity libraries. This makes it easier to develop and test with standard .net tooling on any platform without having to pull in Unity, and has some impact on how the threading model works.

The UI library brings in all the Unity dependencies and implements/registers handlers for any Unity-specific things, like logging to the Unity console and registering callbacks to the Unity UI thread task scheduler.

## Threading model

All threading is done via the Task Parallel Library (the `System.Threading.Tasks` namespace, TPL for short). The API and UI libraries together work on a "three threads" threading model, with three separate Task Schedulers:

1. UI task scheduler - This task scheduler is set up from the UI/main thread. Any calls like `RunOnUI` will execute on this task scheduler, and is used when a task affinity is set to `UI`.
2. Exclusive task scheduler - This task scheduler is used when a task affinity is set to `Exclusive`. Exclusive tasks run one at a time, and if an exclusive task is running, no concurrent tasks are running. Tasks that are marked exclusive are usually ones that write to the git database (like `git commit` or `git pull`), to ensure some amount of atomicity to these operations.
3. Concurrent task scheduler - This task scheduler is used by default if no affinity is set or if it's set to `Concurrent`. Tasks are executed in parallel.

### Task chains

Tasks can have dependencies, forming a chain, usually with a `Finally` task at the end. When you start a task on the chain (usually the end one), the dependency tree is traversed all the way to the first task that hasn't been executed yet, and all tasks get executed sequentially down the tree, each executing in the scheduler corresponding to the individual task affinity, and each optionally receiving the previous task's output. This forms a pipeline of tasks that take data, process and output data, each running in a specific task scheduler (so you can have a task that runs on the concurrent scheduler, and then another task that takes the previous task output and runs on the ui scheduler, and then another task that runs on the exclusive scheduler, and so on). If a task fails, no other task down the tree is executed *except* for `Finally` tasks, which always get called. `Catch` handlers are also always executed if a task fails, but they are called in the same thread as the failing task (they are callbacks, not separate tasks), and can be used to optionally "unfail" a task, allowing a chain to continue even if something fails in the middle.

### TPL in Unity

TPL relies on Task Schedulers. The Exclusive and Concurrent task schedulers are custom, so they create their own threads and synchronization contexts to run things on. To run things on an existing thread created externally, however (like the Unity UI thread), we need a synchronization context, which handles posting and sending actions to a thread or threads, and then we can create a task scheduler via `TaskScheduler.FromCurrentSynchronizationContext()`.

It's standard in .NET for the main thread to have a default synchronization context available. https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Scripting/UnitySynchronizationContext.cs is Unity's default synchronization context (note: before 2018.2, Unity did not ship with a default context). For a synchronization context to work, something must pump it in order for things to get executed (in Unity's case, by having the runtime call [`ExecuteTasks`](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Scripting/UnitySynchronizationContext.cs#L94).

Unfortunately, afaict, Unity only pumps the synchronization context when the Editor is in play mode. This means that any tasks scheduled on a task scheduler based on this synchronization context won't run outside of play mode unless the sync context is manually pumped.

An easier way to have a synchronization context that is guaranteed to always be pumped on the UI thread is to use `EditorApplication.delayCall`, which schedules delegates to be executed in the ui thread. https://github.com/Unity-Technologies/Git-for-Unity/blob/master/src/com.spoiledcat.git.ui/UI/Threading/SingleThreadSynchronizationContext.cs is an implementation of a sync context that does just that.

As mentioned above, the way to obtain a task scheduler from a sync context is to call `TaskScheduler.FromCurrentSynchronizationContext()`, but in order for this call to work, the current synchronization context must be replaced. We should be careful about trampling over the existing synchronization context, however, because other code might be relying on whatever is set, and we only really need it to create the task scheduler object. [`ThreadingHelper.GetUIScheduler`](https://github.com/Unity-Technologies/Git-for-Unity/blob/master/src/com.spoiledcat.git.api/Api/Threading/ThreadingHelper.cs#L20) does the work of temporarily switching out the synchronization context in order to obtain a usable task scheduler and then restoring the original context.

### TL;DR Initialization

To initialize the threading model properly in the Unity editor, the following steps need to happen:

```
// this can happen in any thread, and doesn't rely on Unity APIs
var taskManager = new TaskManager();

[...]

// this can happen in any thread, and relies on Unity APIs
var unityUISynchronizationContext = new MainThreadSynchronizationContext();

[...]

// this should happen in the UI thread, so task manager knows the thread id of the main thread

taskManager.Initialize(unityUISynchronizationContext);
```

If you already have a task scheduler and want to use it instead (in tests for example)

```
// this can happen in any thread, and doesn't rely on Unity APIs
var taskManager = new TaskManager();

[...]

// this should happen in the UI thread, so task manager knows the thread id of the main thread

taskManager.Initialize(uiTaskScheduler);
```
