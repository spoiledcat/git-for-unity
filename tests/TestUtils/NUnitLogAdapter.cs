using System;
using System.Threading;
using Unity.Editor.Tasks.Logging;

namespace BaseTests
{
	public class NUnitLogAdapter : LogAdapterBase
	{
		public override void Info(string context, string message)
		{
			WriteLine(context, message);
		}

		public override void Debug(string context, string message)
		{
			WriteLine(context, message);
		}

		public override void Trace(string context, string message)
		{
			WriteLine(context, message);
		}

		public override void Warning(string context, string message)
		{
			WriteLine(context, message);
		}

		public override void Error(string context, string message)
		{
			WriteLine(context, message);
		}

		private string GetMessage(string context, string message)
		{
			var time = DateTime.Now.ToString("HH:mm:ss.fff tt");
			var threadId = Thread.CurrentThread.ManagedThreadId;
			return string.Format("{0} [{1,2}] {2} {3}", time, threadId, context, message);
		}

		private void WriteLine(string context, string message)
		{
			NUnit.Framework.TestContext.Progress.WriteLine(GetMessage(context, message));
		}
	}
}
