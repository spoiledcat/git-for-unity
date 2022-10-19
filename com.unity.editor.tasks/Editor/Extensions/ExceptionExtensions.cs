// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Reflection;
using System.Threading;

#if (!NET35)
using System.Runtime.ExceptionServices;
#endif

namespace Unity.Editor.Tasks
{
	public static class ExceptionExtensions
	{
		private static Action<Exception> saveStackTraceForThrowing;

		/// <summary>
		/// Represents exceptions we should never attempt to catch and ignore.
		/// </summary>
		/// <param name="exception">The exception being thrown.</param>
		/// <returns></returns>
		public static bool IsCriticalException(this Exception exception)
		{
			if (exception == null) throw new ArgumentNullException(nameof(exception));

			return exception.IsFatalException()
				|| exception is AppDomainUnloadedException
				|| exception is BadImageFormatException
				|| exception is CannotUnloadAppDomainException
				|| exception is InvalidProgramException
				|| exception is NullReferenceException
				|| exception is ArgumentException;
		}

		/// <summary>
		/// Represents exceptions we should never attempt to catch and ignore when executing third party plugin code.
		/// This is not as extensive as a proposed IsCriticalException method that I want to write for our own code.
		/// </summary>
		/// <param name="exception">The exception being thrown.</param>
		/// <returns></returns>
		public static bool IsFatalException(this Exception exception)
		{
			if (exception == null) throw new ArgumentNullException(nameof(exception));

			return exception is StackOverflowException
				|| exception is OutOfMemoryException
				|| exception is ThreadAbortException
				|| exception is AccessViolationException;
		}

		public static bool CanRetry(this Exception exception)
		{
			return !exception.IsCriticalException()
				&& !(exception is ObjectDisposedException);
		}

		public static void Rethrow(this Exception exception)
		{
#if NET35
			SaveStackTraceForThrowing(exception);
			throw exception;
#else
			ExceptionDispatchInfo.Capture(exception).Throw();
#endif
		}

		private static Action<Exception> SaveStackTraceForThrowing {
			get {
				if (saveStackTraceForThrowing == null)
				{
					// in mono, FixRemotingException saves the original stacktrace
					// in .net < 4.0, InternalPreserveStackTrace saves it
					// but .net also has a FixRemotingException method, so try InternalPreserveStackTrace first
					var method = typeof(Exception).GetMethod( "InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic );
					if (method == null) // maybe it's mono
						typeof(Exception).GetMethod( "FixRemotingException", BindingFlags.Instance | BindingFlags.NonPublic );
					saveStackTraceForThrowing = (Action<Exception>)Delegate.CreateDelegate(typeof(Action<Exception>), method);
				}
				return saveStackTraceForThrowing;
			}
		}
	}
}
