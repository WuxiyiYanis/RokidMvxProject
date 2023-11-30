using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MVXUnity
{
	[InitializeOnLoad]
	public static class MvxSampleDownloader
	{
		private static Scene currentScene;

		static MvxSampleDownloader()
		{
			EditorApplication.hierarchyChanged += EditorApplication_hierarchyChanged; ;
		}

		private static void EditorApplication_hierarchyChanged()
		{
			if (Application.isPlaying)
				return;

			if (currentScene != EditorSceneManager.GetActiveScene())
			{
				currentScene = EditorSceneManager.GetActiveScene();

				if (currentScene.path.StartsWith("Assets/Plugins/Mvx2/Sample Scenes/"))
				{
					string[] assets = AssetDatabase.FindAssets("chloe_battle_rysk_h264", new[] { "Assets/StreamingAssets" });

					if (assets.Length == 0 &&
						EditorUtility.DisplayDialog(
							"Download sample MVX file?",
							"Do you want to download a sample MVX file to StreamingAssets?\n\nchloe_battle_rysk_h264.mvx (147 MB)",

							"Download", "Cancel"))
					{
						EditorUtility.DisplayCancelableProgressBar("Downloading sample", "Downloading chloe_battle_rysk_h264.mvx", 0);

						try
						{
							WebClient client = new WebClient();

							string tempPath = Path.Combine(Application.temporaryCachePath, "chloe_battle_rysk_h264.mvx");
							string importPath = Path.Combine(Application.streamingAssetsPath, "chloe_battle_rysk_h264.mvx");

							client.DownloadProgressChanged += (sender, e) =>
							{
								if (EditorUtility.DisplayCancelableProgressBar("Downloading sample", "Downloading chloe_battle_rysk_h264.mvx", e.ProgressPercentage / 100f))
								{
									client.CancelAsync();
									EditorUtility.ClearProgressBar();
								}
							};

							client.DownloadFileCompleted += (sender, e) =>
							{
								if (e.Error == null && !e.Cancelled)
								{
									Debug.Log("Importing downloaded file to " + importPath);
									if (!Directory.Exists(Application.streamingAssetsPath))
									{
										Directory.CreateDirectory(Application.streamingAssetsPath);
									}
									File.Move(tempPath, importPath);
								}
								else if (e.Error != null)
								{
									Debug.LogError(e.Error);
								}
								else if (e.Cancelled)
								{
									Debug.LogWarning("Download cancelled");
								}

								client.Dispose();
								EditorUtility.ClearProgressBar();
							};

							Debug.Log("Downloading file to temp: " + tempPath); //if it downloads directly to streaming assets it is imported on each progress write as it is being downloaded
							client.DownloadFileAsync(new System.Uri("https://mv-syk-uploads.s3.amazonaws.com/chloe_battle_rysk_h264.mvx"), tempPath);
						}
						finally
						{
							EditorUtility.ClearProgressBar();
						}
					}
				}
			}
		}
	}
}