using System;
using System.Threading;
using SpoiledCat;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Extensions;
using Unity.Editor.Tasks.Logging;
using UnityEditor;
using UnityEngine;
using Unity.VersionControl.Git;
using Unity.VersionControl.Git.IO;

class DugiteManifestWindow : BaseWindow
{
	[MenuItem("Git for Unity/Download latest dugite")]
	public static void Menu_DownloadLatestDugite()
	{
		LogHelper.LogAdapter = new UnityLogAdapter();

		var extensionInstallPath = Application.dataPath.ToSPath().Parent;

        var unityEnv = TheEnvironment.instance.Environment;
        var env = new ApplicationEnvironment(unityEnv);
        env.Initialize(extensionInstallPath, unityEnv);
        var platform = new Platform(env);
        platform.Initialize();

        env.InitializeRepository();

		var installer = new GitInstaller.GitInstallDetails(env.RepositoryPath, env);
		var manifest = DugiteReleaseManifest.Load(platform.TaskManager, installer.GitManifest, installer.GitManifestFeed, platform.Environment);

        var cts = new CancellationTokenSource();
		var downloader = new Downloader(platform.TaskManager, cts.Token);
		var downloadPath = env.RepositoryPath.Combine("downloads");
		foreach (var asset in manifest.Assets)
		{
			downloadPath.Combine(asset.Url.Filename).DeleteIfExists();
			downloader.QueueDownload(asset.Url, downloadPath, retryCount: 3);
		}

		downloader.Progress(p => {
			platform.TaskManager.RunInUI(() => {
				if (EditorUtility.DisplayCancelableProgressBar(p.Message, p.InnerProgress?.InnerProgress?.Message ?? p.InnerProgress?.Message ?? p.Message,
					p.Percentage))
				{
					cts.Cancel();
				}
			});
		}).FinallyInUI((success, ex) => {
			EditorUtility.ClearProgressBar();
			if (success)
				EditorUtility.DisplayDialog("Download done", downloadPath, "Ok");
			else
				EditorUtility.DisplayDialog("Error!", ex.GetExceptionMessageShort(), "Ok");

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
