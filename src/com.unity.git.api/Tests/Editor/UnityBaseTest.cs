using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;
using Unity.VersionControl.Git.IO;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace BaseTests
{
	using System;
	using Unity.Editor.Tasks.Internal.IO;

	// Unity does not support async/await tests, but it does
	// have a special type of test with a [CustomUnityTest] attribute
	// which mimicks a coroutine in EditMode. This attribute is
	// defined here so the tests can be compiled without
	// referencing Unity, and nunit on the command line
	// outside of Unity can execute the tests. Basically I don't
	// want to keep two copies of all the tests.
	public class CustomUnityTestAttribute : UnityTestAttribute
	{ }


	public partial class BaseTest : IDisposable
	{
		private LogAdapterBase existingLogger;
		private bool existingTracing;

        internal TestData StartTest([CallerMemberName] string testName = "test") => new TestData(testName, new LogFacade(testName, new UnityLogAdapter(), false));

        public BaseTest()
		{
			// set up the logger so it doesn't write exceptions to the unity log, the test runner doesn't like it
			existingLogger = LogHelper.LogAdapter;
			existingTracing = LogHelper.TracingEnabled;
			LogHelper.TracingEnabled = false;
			LogHelper.LogAdapter = new NullLogAdapter();
		}

		public void Dispose()
		{
			LogHelper.LogAdapter = existingLogger;
			LogHelper.TracingEnabled = existingTracing;
		}

		internal SPath? testApp;

		internal SPath TestApp
		{
			get
			{
				if (!testApp.HasValue)
				{
					testApp = "Packages/com.unity.editor.tasks/Tests/Helpers~/Helper.CommandLine.exe".ToSPath().Resolve();
					if (!testApp.Value.FileExists())
					{
						testApp = "Packages/com.unity.editor.tasks.tests/Helpers~/Helper.CommandLine.exe".ToSPath().Resolve();
						if (!testApp.Value.FileExists())
						{
							testApp = "Packages/com.unity.editor.tasks.tests/Tests/Helpers~/Helper.CommandLine.exe".ToSPath().Resolve();
							if (!testApp.Value.FileExists())
							{
								Debug.LogException(new InvalidOperationException(
									"Test helper binaries are missing. Build the UnityTools.sln solution once with `dotnet build` in order to set up the tests."));
								testApp = SPath.Default;
							}
						}
					}
				}
				return testApp.Value;
			}
		}
    }
}
