
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;
using Unity.VersionControl.Git;
using Unity.VersionControl.Git.IO;

namespace BaseTests
{
	public partial class BaseTest
	{
		internal TestData StartTest(string testRepoName = null, bool withHttpServer = false,
            ICacheContainer cacheContainer = null,
            IFileSystem fileSystem = null,
            [CallerMemberName] string testName = "test") =>
            new TestData(testName, new LogFacade(testName, new NUnitLogAdapter(), false), testRepoName, withHttpServer,
                cacheContainer, fileSystem);

		protected async Task RunTest(Func<IEnumerator> testMethodToRun)
		{
			var scheduler = ThreadingHelper.GetUIScheduler(new ThreadSynchronizationContext(default));
			var taskStart = new Task<IEnumerator>(testMethodToRun);
			taskStart.Start(scheduler);
			var e = await RunOn(testMethodToRun, scheduler);
			while (await RunOn(s => ((IEnumerator)s).MoveNext(), e, scheduler))
			{ }
		}

		private Task<T> RunOn<T>(Func<T> method, TaskScheduler scheduler)
		{
			return Task<T>.Factory.StartNew(method, CancellationToken.None, TaskCreationOptions.None, scheduler);
		}

		private Task<T> RunOn<T>(Func<object, T> method, object state, TaskScheduler scheduler)
		{
			return Task<T>.Factory.StartNew(method, state, CancellationToken.None, TaskCreationOptions.None, scheduler);
		}

		protected SPath? testApp;
		protected SPath TestApp
		{
			get
			{
				if (!testApp.HasValue)
					testApp = System.Reflection.Assembly.GetExecutingAssembly().Location.ToSPath().Parent.Combine("Helper.CommandLine.exe");
				return testApp.Value;
			}
		}
	}
}
