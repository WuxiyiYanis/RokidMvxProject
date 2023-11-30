using UnityEngine;
using UnityEngine.Networking;
using System.IO;

namespace MVXUnity
{
    /// <summary> Class responsible for preparing Mvx2 files for streaming. </summary>
    /// <remarks> On some platforms it may be necessary to process the referenced file before it can be accessed. </remarks>
    public class MvxFileAccessor
    {
        private string m_currentOriginalPath = null;
        private string m_currentProcessedPath = null;

        public void Reset()
        {
            m_currentOriginalPath = null;
            m_currentProcessedPath = null;
        }

        public string PrepareFile(string path)
        {
            if (path == m_currentOriginalPath)
                return m_currentProcessedPath;

            if (Path.IsPathRooted(path))
            {
                m_currentOriginalPath = path;
                m_currentProcessedPath = path;
                return m_currentProcessedPath;
            }

            m_currentOriginalPath = path;
            m_currentProcessedPath = ProcessStreamingAssetsFile(path);
            return m_currentProcessedPath;
        }

        private static string ProcessStreamingAssetsFile(string pathInStreamingAssets)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            return ProcessStreamingAssetsFile_Android(pathInStreamingAssets);
#else
            return ProcessStreamingAssetsFile_Default(pathInStreamingAssets);
#endif
        }

        private static string ProcessStreamingAssetsFile_Default(string pathInStreamingAssets)
        {
            // no processing required here, only compose full path
            return Path.Combine(Application.streamingAssetsPath, pathInStreamingAssets).Replace('\\', '/');
        }

#if !UNITY_EDITOR && UNITY_ANDROID
        private static string ProcessStreamingAssetsFile_Android(string pathInStreamingAssets)
        {
            // extract the file to the application's persistent data path
            
            string persistentDataFilePath = Path.Combine(Application.persistentDataPath, pathInStreamingAssets).Replace('\\', '/');
            string streamingAssetsFilePath = Path.Combine(Application.streamingAssetsPath, pathInStreamingAssets).Replace('\\', '/');
            Debug.LogFormat("Mvx2: File {0} will be copied to {1}", streamingAssetsFilePath, persistentDataFilePath);
            
            UnityWebRequest request = new UnityWebRequest(streamingAssetsFilePath);
            request.method = UnityWebRequest.kHttpVerbGET;
            DownloadHandlerFile downloadHandler = new DownloadHandlerFile(persistentDataFilePath);
            downloadHandler.removeFileOnAbort = true;
            request.downloadHandler = downloadHandler;

            request.SendWebRequest();
            while (!request.isDone && !request.isNetworkError)
                continue;

            if (request.isNetworkError)
            {
                Debug.LogErrorFormat("Mvx2: Failed to process file from Unity's streaming assets folder. {0}", request.error);
                return string.Empty;
            }

            return persistentDataFilePath;
        }
#endif
    }
}

