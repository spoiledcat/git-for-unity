using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SpoiledCat;
using UnityEditor;
using UnityEngine;
using Unity.VersionControl.Git;

class DugiteManifestWindow : BaseWindow
{
	[MenuItem("Git for Unity/Download latest dugite")]
	public static void Menu_DownloadLatestDugite()
	{
		LogHelper.LogAdapter = new UnityLogAdapter();

		var unityAssetsPath = Application.dataPath;
		var unityApplication = EditorApplication.applicationPath;
		var unityApplicationContents = EditorApplication.applicationContentsPath;
		var extensionInstallPath = Application.dataPath.ToNPath().Parent;
		var unityVersion = Application.unityVersion;
		var env = new DefaultEnvironment();
		env.Initialize(unityVersion, extensionInstallPath, unityApplication.ToNPath(),
			unityApplicationContents.ToNPath(), unityAssetsPath.ToNPath());
		env.InitializeRepository();
		TaskManager.Instance.Initialize(new UnityUIThreadSynchronizationContext());

        var installer = new GitInstaller.GitInstallDetails(env.RepositoryPath, env);
		var manifest = DugiteReleaseManifest.Load(installer.GitManifest, GitInstaller.GitInstallDetails.GitPackageFeed, env);

		var downloader = new Downloader();
		var downloadPath = env.RepositoryPath.Combine("downloads");
		foreach (var asset in manifest.Assets)
		{
			downloadPath.Combine(asset.Url.Filename).DeleteIfExists();
			downloader.QueueDownload(asset.Url, downloadPath, retryCount: 3);
		}

		downloader.Progress(p => {
			TaskManager.Instance.RunInUI(() => {
				if (EditorUtility.DisplayCancelableProgressBar(p.Message, p.InnerProgress?.InnerProgress?.Message ?? p.InnerProgress?.Message ?? p.Message,
					p.Percentage))
				{
					downloader.Cancel();
				}
			});
		}).FinallyInUI((success, ex) => {
			EditorUtility.ClearProgressBar();
			if (success)
				EditorUtility.DisplayDialog("Download done", downloadPath, "Ok");
			else
				EditorUtility.DisplayDialog("Error!", ex.GetExceptionMessageOnly(), "Ok");

		}).Start();
	}
}

class UnityLogAdapter : LogAdapterBase
{
	private string GetMessage(string context, string message)
	{
		var time = DateTime.Now.ToString("HH:mm:ss tt");
		var threadId = Thread.CurrentThread.ManagedThreadId;
		return string.Format("{0} [{1,2}] {2} {3}", time, threadId, context, message);
	}

	public override void Info(string context, string message)
	{
		UnityEngine.Debug.Log(GetMessage(context, message));
	}

	public override void Debug(string context, string message)
	{
		UnityEngine.Debug.Log(GetMessage(context, message));
	}

	public override void Trace(string context, string message)
	{
		UnityEngine.Debug.Log(GetMessage(context, message));
	}

	public override void Warning(string context, string message)
	{
		UnityEngine.Debug.LogWarning(GetMessage(context, message));
	}

	public override void Error(string context, string message)
	{
		UnityEngine.Debug.LogError(GetMessage(context, message));
	}
}
