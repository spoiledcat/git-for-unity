// Copyright 2016-2019 Andreia Gaita
// Copyright 2015-2018 GitHub
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.IO;

namespace Unity.Editor.Tasks.Extensions
{
	using Internal.IO;

	internal static class PathExtensions
	{
		public static string ToMD5(this SPath path)
		{
			byte[] computeHash;
			using (var hash = System.Security.Cryptography.MD5.Create())
			{
				using (var stream = path.OpenRead())
				{
					computeHash = hash.ComputeHash(stream);
				}
			}

			return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
		}

		public static string ToSha256(this SPath path)
		{
			byte[] computeHash;
			using (var hash = System.Security.Cryptography.SHA256.Create())
			{
				using (var stream = path.OpenRead())
				{
					computeHash = hash.ComputeHash(stream);
				}
			}

			return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
		}
	}
}
