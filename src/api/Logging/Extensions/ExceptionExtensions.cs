using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.VersionControl.Git
{
    public static class ExceptionExtensions
    {
        public static string GetExceptionMessage(this Exception ex)
        {
            var message = ex.ToString();
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += Environment.NewLine + inner.ToString();
                inner = inner.InnerException;
            }
            var caller = Environment.StackTrace;
            var stack = caller.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            message += Environment.NewLine + "=======";
            message += Environment.NewLine + String.Join(Environment.NewLine, stack.Skip(1).SkipWhile(x => x.Contains("Git.Logging")).ToArray());
            return message;
        }

        public static string GetExceptionMessageShort(this Exception ex)
        {
	        var exceptions = new List<Exception> { ex };
	        var inner = ex.InnerException;
	        while (inner != null)
	        {
		        exceptions.Add(inner);
		        inner = inner.InnerException;
	        }

	        var message = string.Join(Environment.NewLine, exceptions.Select(x => x.Message).ToArray());
	        message += Environment.NewLine + exceptions.Last().StackTrace;
            return message;
        }

        public static string GetExceptionMessageOnly(this Exception ex)
        {
	        var exceptions = new List<Exception> { ex };
	        var inner = ex.InnerException;
	        while (inner != null)
	        {
		        exceptions.Add(inner);
		        inner = inner.InnerException;
	        }

	        var message = string.Join(Environment.NewLine, exceptions.Select(x => x.Message).ToArray());
	        return message;
        }
    }
}
