// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace Unity.Editor.Tasks.Logging
{
	public static class LogHelper
	{
		private static readonly LogAdapterBase nullLogAdapter = new NullLogAdapter();

		private static bool tracingEnabled;

		private static LogAdapterBase logAdapter = nullLogAdapter;

		private static ILogging instance;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static ILogging GetLogger<T>() => GetLogger(typeof(T));

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static ILogging GetLogger(Type type) => GetLogger(type.Name);

		public static ILogging GetLogger(string context) => new LogFacade($"<{context ?? "Global"}>");

		public static bool TracingEnabled
		{
			get => tracingEnabled;
			set
			{
				if (tracingEnabled != value)
				{
					tracingEnabled = value;
					LogAdapter.Info("Global", "Trace Logging " + (value ? "Enabled" : "Disabled"));
				}
			}
		}

		public static bool Verbose { get; set; }

		public static LogAdapterBase LogAdapter
		{
			get => logAdapter;
			set { logAdapter = value ?? nullLogAdapter; }
		}
	}
}
