// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Threading;

namespace Unity.Editor.Tasks.Logging
{
	public class UnityLogAdapter : LogAdapterBase
	{
		public override void Info(string context, string message)
		{
#if UNITY_EDITOR
			UnityEngine.Debug.Log(GetMessage(context, message));
#endif
		}

		public override void Debug(string context, string message)
		{
#if UNITY_EDITOR
			UnityEngine.Debug.Log(GetMessage(context, message));
#endif
		}

		public override void Trace(string context, string message)
		{
#if UNITY_EDITOR
			UnityEngine.Debug.Log(GetMessage(context, message));
#endif
		}

		public override void Warning(string context, string message)
		{
#if UNITY_EDITOR
			UnityEngine.Debug.LogWarning(GetMessage(context, message));
#endif
		}

		public override void Error(string context, string message)
		{
#if UNITY_EDITOR
			UnityEngine.Debug.LogError(GetMessage(context, message));
#endif
		}

		private string GetMessage(string context, string message)
		{
			var time = DateTime.Now.ToString("HH:mm:ss tt");
			var threadId = Thread.CurrentThread.ManagedThreadId;
			return string.Format("{0} [{1,2}] {2} {3}", time, threadId, context, message);
		}
	}
}
