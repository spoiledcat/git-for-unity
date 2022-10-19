// Copyright 2016-2019 Andreia Gaita
// Copyright 2015-2018 GitHub
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace Unity.Editor.Tasks.Extensions
{
	public static class ArrayExtensions
	{
		public static string Join<T>(this T[] arr, string separator = ",")
		{
			return string.Join(separator, arr);
		}

		public static string Join<T>(this IEnumerable<T> arr, string separator = ",")
		{
			return string.Join(separator, arr);
		}

		public static string Join(this IEnumerable arr, string separator = ",")
		{
			return string.Join(separator, arr);
		}
	}
	public static class StringExtensions
	{
		public static bool Contains(this string s, string expectedSubstring, StringComparison comparison)
		{
			return s.IndexOf(expectedSubstring, comparison) > -1;
		}

		public static bool ContainsAny(this string s, IEnumerable<char> characters)
		{
			return s.IndexOfAny(characters.ToArray()) > -1;
		}

		public static string ToNullIfEmpty(this string s)
		{
			return String.IsNullOrEmpty(s) ? null : s;
		}

		public static string ToEmptyIfNull(this string s)
		{
			return String.IsNullOrEmpty(s) ? String.Empty : s;
		}

		public static bool StartsWith(this string s, char c)
		{
			if (String.IsNullOrEmpty(s)) return false;
			return s.First() == c;
		}

		public static string RightAfter(this string s, string search)
		{
			if (s == null) return null;
			int lastIndex = s.IndexOf(search, StringComparison.OrdinalIgnoreCase);
			if (lastIndex < 0)
				return null;

			return s.Substring(lastIndex + search.Length);
		}

		public static string RightAfterLast(this string s, string search)
		{
			if (s == null) return null;
			int lastIndex = s.LastIndexOf(search, StringComparison.OrdinalIgnoreCase);
			if (lastIndex < 0)
				return null;

			return s.Substring(lastIndex + search.Length);
		}

		public static string LeftBeforeLast(this string s, string search)
		{
			if (s == null) return null;
			int lastIndex = s.LastIndexOf(search, StringComparison.OrdinalIgnoreCase);
			if (lastIndex < 0)
				return null;

			return s.Substring(0, lastIndex);
		}

		public static string[] SplitLast(this string s, char separator)
		{
			if (s == null) return Array.Empty<string>();
			int lastIndex = s.LastIndexOf(separator);
			if (lastIndex < 0 || lastIndex == s.Length - 1) return new string[] { s };
			return new string[] { s.Substring(0, lastIndex), s.Substring(lastIndex + 1) };
		}

		public static string TrimEnd(this string s, string suffix)
		{
			if (s == null) return null;
			if (!s.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
				return s;

			return s.Substring(0, s.Length - suffix.Length);
		}

		/// <summary>
		/// Pretty much the same things as `String.Join` but used when appending to an already delimited string. If the values passed
		/// in are empty, it does not prepend the delimeter. Otherwise, it prepends with the delimiter.
		/// </summary>
		/// <param name="separator">The separator character</param>
		/// <param name="values">The set values to join</param>
		public static string JoinForAppending(this IEnumerable<string> values, string separator)
		{
			return values.Any()
				? separator + String.Join(separator, values.ToArray())
				: string.Empty;
		}

		public static string RightAfter(this string s, char search)
		{
			if (s == null) return null;
			int lastIndex = s.IndexOf(search);
			if (lastIndex < 0)
				return null;

			return s.Substring(lastIndex + 1);
		}

		public static string RightAfterLast(this string s, char search)
		{
			if (s == null) return null;
			int lastIndex = s.LastIndexOf(search);
			if (lastIndex < 0)
				return null;

			return s.Substring(lastIndex + 1);
		}

		public static string LeftBeforeLast(this string s, char search)
		{
			if (s == null) return null;
			int lastIndex = s.LastIndexOf(search);
			if (lastIndex < 0)
				return null;

			return s.Substring(0, lastIndex);
		}

		public static string InQuotes(this string str)
		{
			if (str == null) return "\"\"";
			var needsStartQuote = !str.StartsWith("\"");
			var needsEndQuote = !str.EndsWith("\"");
			if (needsStartQuote) str = "\"" + str;
			if (needsEndQuote) str += "\"";
			return str;
		}
	}
}
