// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace Unity.Editor.Tasks
{

#if UNITY_EDITOR
	using UnityEngine;
	using UnityEditor;
#else
	using EditorStubs;
#endif

    using Internal.IO;

	public sealed class TheEnvironment : ScriptableSingleton<TheEnvironment>
	{
		[NonSerialized] private IEnvironment environment;
		[SerializeField] private string unityApplication;
		[SerializeField] private string unityApplicationContents;
		[SerializeField] private string unityVersion;
		[SerializeField] private string projectPath;

		public void Flush()
		{
			unityApplication = Environment.UnityApplication;
			unityApplicationContents = Environment.UnityApplicationContents;
			unityVersion = Environment.UnityVersion;
			Save(true);
		}

		public static string ApplicationName { get; set; }

		public IEnvironment Environment
		{
			get
			{
				if (environment == null)
				{
					environment = new UnityEnvironment(ApplicationName ?? Application.productName);
					if (projectPath == null)
					{
#if UNITY_EDITOR
						projectPath = ".".ToSPath().Resolve().ToString(SlashMode.Forward);
#else
						projectPath = Application.projectPath;
#endif
						unityVersion = Application.unityVersion;
						unityApplication = EditorApplication.applicationPath;
						unityApplicationContents = EditorApplication.applicationContentsPath;
					}

					environment.Initialize(projectPath, unityVersion, unityApplication, unityApplicationContents);
					Flush();
				}
				return environment;
			}
		}
	}
}
