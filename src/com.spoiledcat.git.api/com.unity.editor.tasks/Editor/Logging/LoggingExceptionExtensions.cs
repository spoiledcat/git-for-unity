// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Linq;

namespace Unity.Editor.Tasks.Extensions
{
	public static class LoggingExceptionExtensions
	{
		public static string GetExceptionMessage(this Exception ex)
		{
			var message = GetExceptionMessageShort(ex);

			message += Environment.NewLine + "=======";

			var caller = Environment.StackTrace;
			var stack = caller.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			message += Environment.NewLine + string.Join(Environment.NewLine, stack.Skip(1).SkipWhile(x => x.Contains(nameof(GetExceptionMessage)) || x.Contains("LogFacade")).ToArray());
			return message;
		}

		public static string GetExceptionMessageShort(this Exception ex)
		{
			if (ex == null) return String.Empty;
			var message = ex.ToString();
			var inner = ex.InnerException;
			while (inner != null)
			{
				message += Environment.NewLine + inner.ToString();
				inner = inner.InnerException;
			}
			return message;
		}
	}
}
