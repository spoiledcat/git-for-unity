// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Runtime.Serialization;

namespace Unity.Editor.Tasks
{
	[Serializable]
	public class NotReadyException : Exception
	{
		public NotReadyException() : base()
		{ }

		public NotReadyException(string message) : base(message)
		{ }

		public NotReadyException(string message, Exception innerException) : base(message, innerException)
		{ }

		protected NotReadyException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }
	}
}
