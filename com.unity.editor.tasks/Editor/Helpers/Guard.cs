// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace Unity.Editor.Tasks.Helpers
{
	using System.Runtime.CompilerServices;

	[Serializable]
	internal class InstanceNotInitializedException : InvalidOperationException
	{
		public InstanceNotInitializedException(object the, string property) :
			base(String.Format(CultureInfo.InvariantCulture, "{0} is not correctly initialized, {1} is null", the?.GetType().Name, property))
		{ }

		protected InstanceNotInitializedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }
	}

	internal static class Guard
	{
		public static T EnsureNotNull<T>(this T the, object value, string propertyName)
		{
			if (value != null) return the;
			throw new InstanceNotInitializedException(the, propertyName);
		}

		public static T EnsureNotNull<T>(this T value, string name, [CallerMemberName] string caller = "")
		{
			if (value != null) return value;
			string message = String.Format(CultureInfo.InvariantCulture, "In {0}, '{1}' must not be null", caller, name);
			throw new ArgumentNullException(name, message);
		}

		public static T EnsureNotNullOrEmpty<T>(this T value, string name, [CallerMemberName] string caller = "")
			where T : IList<T>
		{
			if (value == null)
			{
				string message = String.Format(CultureInfo.InvariantCulture, "In {0}, '{1}' must not be null", caller, name);
				throw new ArgumentNullException(name, message);
			}

			if (!value.Any())
			{
				string message = String.Format(CultureInfo.InvariantCulture, "In {0}, '{1}' must not be empty", caller, name);
				throw new ArgumentNullException(name, message);
			}
			return value;
		}

		public static int EnsureNonNegative(this int value, string name, [CallerMemberName] string caller = "")
		{
			if (value > -1) return value;

			string message = String.Format(CultureInfo.InvariantCulture, "In {0}, '{1}' must be positive", caller, name);
			throw new ArgumentException(message, name);
		}

		/// <summary>
		///   Checks a string argument to ensure it isn't null or empty.
		/// </summary>
		/// <param name = "value">The argument value to check.</param>
		/// <param name = "name">The name of the argument.</param>
		/// <param name="caller"></param>
		public static string EnsureNotNullOrWhiteSpace(this string value, string name, [CallerMemberName] string caller = "")
		{
			if (value != null && value.Trim().Length > 0)
				return value;
			string message = String.Format(CultureInfo.InvariantCulture, "In {0}, '{1}' must not be empty", caller, name);
			throw new ArgumentException(message, name);
		}

		public static int EnsureInRange(this int value, int minValue, string name, [CallerMemberName] string caller = "")
		{
			if (value >= minValue) return value;
			string message = String.Format(CultureInfo.InvariantCulture,
				"In {3}, The value '{0}' for '{1}' must be greater than or equal to '{2}'",
				value, name, minValue, caller);
			throw new ArgumentOutOfRangeException(name, message);
		}

		public static int EnsureInRange(this int value, int minValue, int maxValue, string name, [CallerMemberName] string caller = "")
		{
			if (value >= minValue && value <= maxValue) return value;
			string message = String.Format(CultureInfo.InvariantCulture,
				"In {4}, The value '{0}' for '{1}' must be greater than or equal to '{2}' and less than or equal to '{3}'",
				value, name, minValue, maxValue, caller);
			throw new ArgumentOutOfRangeException(name, message);
		}

		public static bool InUnitTestRunner { get; set; }
	}
}
