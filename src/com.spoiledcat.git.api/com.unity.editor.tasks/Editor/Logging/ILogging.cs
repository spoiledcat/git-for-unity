// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace Unity.Editor.Tasks.Logging
{
	public interface ILogging
	{
		bool TracingEnabled { get; set; }

		void Info(string message);
		void Info(string format, params object[] objects);
		void Info(Exception ex, string message);
		void Info(Exception ex);
		void Info(Exception ex, string format, params object[] objects);

		void Debug(string message);
		void Debug(string format, params object[] objects);
		void Debug(Exception ex);
		void Debug(Exception ex, string message);
		void Debug(Exception ex, string format, params object[] objects);

		void Trace(string message);
		void Trace(string format, params object[] objects);
		void Trace(Exception ex);
		void Trace(Exception ex, string message);
		void Trace(Exception ex, string format, params object[] objects);

		void Warning(string message);
		void Warning(string format, params object[] objects);
		void Warning(Exception ex);
		void Warning(Exception ex, string message);
		void Warning(Exception ex, string format, params object[] objects);

		void Error(string message);
		void Error(string format, params object[] objects);
		void Error(Exception ex);
		void Error(Exception ex, string message);
		void Error(Exception ex, string format, params object[] objects);
	}
}
