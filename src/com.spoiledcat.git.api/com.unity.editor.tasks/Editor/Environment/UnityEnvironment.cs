using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Unity.Editor.Tasks
{
	public interface IEnvironment
	{
		IEnvironment Initialize(string projectPath,
			string unityVersion = null,
			string EditorApplication_applicationPath = default,
			string EditorApplication_applicationContentsPath = default);

		string ExpandEnvironmentVariables(string name);
		string GetEnvironmentVariable(string v);
		string GetEnvironmentVariableKey(string name);

		string Path { get; set; }
		string NewLine { get; }
		string ExecutableExtension { get; }
		bool IsWindows { get; }
		bool IsLinux { get; }
		bool IsMac { get; }
		bool Is32Bit { get; }
		string UnityVersion { get; }
		string UnityApplication { get; }
		string UnityApplicationContents { get; }
		string UnityProjectPath { get; }
		string ApplicationName { get; }
	}

	public class UnityEnvironment : IEnvironment
	{
		public UnityEnvironment(string applicationName)
		{
			ApplicationName = applicationName;
		}

		public virtual IEnvironment Initialize(
			string projectPath,
			string unityVersion = null,
			string EditorApplication_applicationPath = default,
			string EditorApplication_applicationContentsPath = default
		)
		{
			UnityProjectPath = projectPath;
			UnityVersion = unityVersion;
			UnityApplication = EditorApplication_applicationPath;
			UnityApplicationContents = EditorApplication_applicationContentsPath;

			return this;
		}

		public string ExpandEnvironmentVariables(string name)
		{
			var key = GetEnvironmentVariableKey(name);
			return Environment.ExpandEnvironmentVariables(key);
		}

		public string GetEnvironmentVariable(string name)
		{
			var key = GetEnvironmentVariableKey(name);
			return Environment.GetEnvironmentVariable(key);
		}

		public string GetEnvironmentVariableKey(string name)
		{
			return GetEnvironmentVariableKeyInternal(name);
		}

		private static string GetEnvironmentVariableKeyInternal(string name)
		{
			return Environment.GetEnvironmentVariables().Keys.Cast<string>()
										.FirstOrDefault(k => string.Compare(name, k, true, CultureInfo.InvariantCulture) == 0) ?? name;
		}

		public string ApplicationName { get; }
		public string UnityVersion { get; set; }
		public string UnityApplication { get; set; }
		public string UnityApplicationContents { get; set; }
		public string UnityProjectPath { get; set; }

		public string Path { get; set; } = Environment.GetEnvironmentVariable(GetEnvironmentVariableKeyInternal("PATH"));

		public string NewLine => Environment.NewLine;

		public bool Is32Bit => IntPtr.Size == 4;

		public string ExecutableExtension => IsWindows ? ".exe" : string.Empty;

		private bool? isLinux;
		private bool? isMac;
		private bool? isWindows;
		public bool IsWindows
		{
			get
			{
				if (isWindows.HasValue)
					return isWindows.Value;
				return Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX;
			}
			set => isWindows = value;
		}

		public bool IsLinux
		{
			get
			{
				if (isLinux.HasValue)
					return isLinux.Value;
				return Environment.OSVersion.Platform == PlatformID.Unix && Directory.Exists("/proc");
			}
			set => isLinux = value;
		}

		public bool IsMac
		{
			get
			{
				if (isMac.HasValue)
					return isMac.Value;
				// most likely it'll return the proper id but just to be on the safe side, have a fallback
				return Environment.OSVersion.Platform == PlatformID.MacOSX ||
						(Environment.OSVersion.Platform == PlatformID.Unix && !Directory.Exists("/proc"));
			}
			set => isMac = value;
		}

	}
}
