using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Extensions;
using Unity.Editor.Tasks.Logging;
using NUnit.Framework;

namespace BaseTests
{
	using System.Threading;
	using Unity.Editor.Tasks.Internal.IO;

	internal class TestData : IDisposable
	{
		public readonly Stopwatch Watch;
		public readonly ILogging Logger;
		public readonly SPath TestPath;
		public readonly string TestName;
		public readonly ITaskManager TaskManager;
		public readonly IEnvironment Environment;
		public readonly IProcessManager ProcessManager;
		private readonly CancellationTokenSource cts;

		public TestData(string testName, ILogging logger)
		{
			TestName = testName;
			Logger = logger;
			Watch = new Stopwatch();
			TestPath = SPath.CreateTempDirectory(testName);
			TaskManager = new TaskManager();
			cts = CancellationTokenSource.CreateLinkedTokenSource(TaskManager.Token);

			try
			{
				TaskManager.Initialize();
			}
			catch
			{
				// we're on the nunit sync context, which can't be used to create a task scheduler
				// so use a different context as the main thread. The test won't run on the main nunit thread
				TaskManager.Initialize(new MainThreadSynchronizationContext(cts.Token));
			}

			Environment = new UnityEnvironment(testName);
			InitializeEnvironment();
			ProcessManager = new ProcessManager(Environment);

			Logger.Trace($"START {testName}");
			Watch.Start();
		}

		private void InitializeEnvironment()
		{
			var projectPath = TestPath.Combine("project").EnsureDirectoryExists();

#if UNITY_EDITOR
			Environment.Initialize(projectPath, TheEnvironment.instance.Environment.UnityVersion, TheEnvironment.instance.Environment.UnityApplication, TheEnvironment.instance.Environment.UnityApplicationContents);
			return;
#endif

			SPath unityPath, unityContentsPath;
			unityPath = CurrentExecutionDirectory;

			while (!unityPath.IsEmpty && !unityPath.DirectoryExists(".Editor"))
				unityPath = unityPath.Parent;

			if (!unityPath.IsEmpty)
			{
				unityPath = unityPath.Combine(".Editor");
				unityContentsPath = unityPath.Combine("Data");
			}
			else
			{
				unityPath = unityContentsPath = SPath.Default;
			}

			Environment.Initialize(projectPath, "2019.2", unityPath, unityContentsPath);
		}

		public void Dispose()
		{
			Watch.Stop();

			ProcessManager.Dispose();
			if (SynchronizationContext.Current is IMainThreadSynchronizationContext ourContext)
				ourContext.Dispose();

			TaskManager.Dispose();
			Logger.Trace($"STOP {TestName} :{Watch.ElapsedMilliseconds}ms");
		}

		internal SPath CurrentExecutionDirectory => System.Reflection.Assembly.GetExecutingAssembly().Location.ToSPath().Parent;
	}

	public partial class BaseTest
	{
		protected const int Timeout = 30000;
		protected const int RandomSeed = 120938;



		protected void StartTrackTime(Stopwatch watch, ILogging logger, string message = "")
		{
			if (!string.IsNullOrEmpty(message))
				logger.Trace(message);
			watch.Reset();
			watch.Start();
		}

		protected void StopTrackTimeAndLog(Stopwatch watch, ILogging logger)
		{
			watch.Stop();
			logger.Trace($"Time: {watch.ElapsedMilliseconds}");
		}

		protected ActionTask GetTask(ITaskManager taskManager, TaskAffinity affinity, int id, Action<int> body)
		{
			return new ActionTask(taskManager, _ => body(id)) { Affinity = affinity };
		}

		protected static IEnumerable<object> StartAndWaitForCompletion(params ITask[] tasks)
		{
			foreach (var task in tasks) task.Start();
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;
		}

		protected static IEnumerable<object> StartAndWaitForCompletion(IEnumerable<ITask> tasks)
		{
			foreach (var task in tasks) task.Start();
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;
		}

		protected static IEnumerable<object> WaitForCompletion(params ITask[] tasks)
		{
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;
		}

		protected static IEnumerable<object> WaitForCompletion(IEnumerable<ITask> tasks)
		{
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;
		}

		protected static IEnumerable<object> WaitForCompletion(IEnumerable<Task> tasks)
		{
			while (!tasks.All(x => x.IsCompleted)) yield return null;
		}

		protected static IEnumerable<object> WaitForCompletion(params Task[] tasks)
		{
			while (!tasks.All(x => x.IsCompleted)) yield return null;
		}
	}


	internal static class TestExtensions
	{
		public static void Matches(this IEnumerable actual, IEnumerable expected)
		{
			CollectionAssert.AreEqual(expected, actual, $"{Environment.NewLine}expected:{expected.Join()}{Environment.NewLine}actual  :{actual.Join()}{Environment.NewLine}");
		}

		public static void Matches<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
		{
			CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(), $"{Environment.NewLine}expected:{expected.Join()}{Environment.NewLine}actual  :{actual.Join()}{Environment.NewLine}");
		}

		public static void MatchesUnsorted(this IEnumerable actual, IEnumerable expected)
		{
			CollectionAssert.AreEquivalent(expected, actual, $"{Environment.NewLine}expected:{expected.Join()}{Environment.NewLine}actual  :{actual.Join()}{Environment.NewLine}");
		}

		public static void MatchesUnsorted<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
		{
			CollectionAssert.AreEquivalent(expected.ToArray(), actual.ToArray(), $"{Environment.NewLine}expected:{expected.Join()}{Environment.NewLine}actual  :{actual.Join()}{Environment.NewLine}");
		}

		public static void Matches(this string actual, string expected) => Assert.AreEqual(expected, actual);
		public static void Matches(this int actual, int expected) => Assert.AreEqual(expected, actual);
		public static void Matches(this SPath actual, SPath expected) => Assert.AreEqual(expected, actual);
	}

	static class KeyValuePair
	{
		public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
		{
			return new KeyValuePair<TKey, TValue>(key, value);
		}
	}

}
