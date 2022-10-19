namespace Unity.Editor.Tasks
{
	using System;
	using System.Runtime.Serialization;
	using System.Threading.Tasks;

	[Serializable]
	public class ProcessException : TaskCanceledException
	{
		protected ProcessException() : base()
		{ }

		public ProcessException(int errorCode, string message) : base(message)
		{
			ErrorCode = errorCode;
		}

		public ProcessException(int errorCode, string message, Exception innerException) : base(message, innerException)
		{
			ErrorCode = errorCode;
		}

		public ProcessException(string message) : base(message)
		{ }

		public ProcessException(string message, Exception innerException) : base(message, innerException)
		{ }

		protected ProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }

		public ProcessException(ITask process) : this(process.Errors)
		{ }

		public int ErrorCode { get; }
	}
}
