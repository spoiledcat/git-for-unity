#if !UNITY_EDITOR

// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace Unity.Editor.Tasks.EditorStubs
{
	public class SerializeFieldAttribute : Attribute
	{}
	public class ScriptableSingleton<T>
		where T : class, new()
	{
		private static T _instance;
		public static T instance => _instance ?? (_instance = new T());

		protected void Save(bool flush)
		{ }
	}
    public static class Application
	{
		public static string productName { get; } = "DefaultApplication";
		public static string unityVersion { get; set; } = "2019.2.1f1";
		public static string projectPath { get; set; }
	}

	public static class EditorApplication
	{
		public static string applicationPath { get; set; }
		public static string applicationContentsPath { get; set; }
	}
}
#endif
