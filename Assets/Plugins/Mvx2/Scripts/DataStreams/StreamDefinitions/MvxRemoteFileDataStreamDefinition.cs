using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace MVXUnity
{
    public abstract class MvxRemoteFileDataStreamDefinition : MvxDataStreamDefinition
    {
        #region data

        /// <summary> URL of the remote file to read. </summary>
        [SerializeField, HideInInspector] private string m_fileURL;
        public string fileURL
        {
            get { return m_fileURL; }
            set
            {
                if (m_fileURL != value)
                {
                    m_fileURL = value;
                    onDefinitionChanged.Invoke();
                }
            }
        }

        /// <summary> Specifies whether a Look-up Table shall be employed if present in the file. </summary>
        [SerializeField, HideInInspector] private bool m_employLookupTable = true;
        public bool employLookupTable
        {
            get { return m_employLookupTable; }
            set
            {
                if (m_employLookupTable != value)
                {
                    m_employLookupTable = value;
                    onDefinitionChanged.Invoke();
                }
            }
        }

        public enum SSLVerificationMode { CertificateAndHost, Certificate, None }

        /// <summary> Requirements for the server certificate verification when a TLS/SSL connection is negotiated. </summary>
        [SerializeField, HideInInspector] private SSLVerificationMode m_sslVerificationMode = SSLVerificationMode.CertificateAndHost;
        public SSLVerificationMode sslVerificationMode
        {
            get { return m_sslVerificationMode; }
            set
            {
                if (m_sslVerificationMode != value)
                {
                    m_sslVerificationMode = value;
                    onDefinitionChanged.Invoke();
                }
            }
        }

        public enum CurlMode { Basic, Caching }

        /// <summary> Requirements for the server certificate verification when a TLS/SSL connection is negotiated. </summary>
        [SerializeField, HideInInspector] private CurlMode m_curlMode = CurlMode.Basic;
        public CurlMode curlMode
        {
            get { return m_curlMode; }
            set
            {
                if (m_curlMode != value)
                {
                    m_curlMode = value;
                    onDefinitionChanged.Invoke();
                }
            }
        }

        [SerializeField, HideInInspector] private string m_cacheFile;
        /// <summary>
        /// Path to a file to which the downloader will write the cache to
        /// </summary>
        public string cacheFile
        {
            get { return m_cacheFile; }
            set
            {
                if (m_cacheFile != value)
                {
                    m_cacheFile = value;
                    onDefinitionChanged.Invoke();
                }
            }
        }

        [SerializeField, HideInInspector] private bool m_slowDownloadBlocks = false;
        /// <summary>
        /// If a slow download is detected, pause playback until enough data is downloaded for smooth playback
        /// </summary>
        public bool slowDownloadBlocks
        {
            get { return m_slowDownloadBlocks; }
            set
            {
                if (m_slowDownloadBlocks != value)
                {
                    m_slowDownloadBlocks = value;
                    onDefinitionChanged.Invoke();
                }
            }
        }

        public enum BufferState
        {
            NONE = 0,
            INITIAL_PERCENTAGE = 1, //not used
            SMOOTH_PLAYBACK = 2,
            BUFFERING = 3
        }

        /// <summary>
        /// Filter events
        /// </summary>
        [SerializeField, HideInInspector] public StreamEvents events;

        [Serializable]
        public class StreamEvents
		{
            /// <summary>
            /// Downloading file to the cache was successful
            /// </summary>
            public UnityEvent onCacheFileSuccessful;
            /// <summary>
            /// Buffering state was changed
            /// </summary>
            public UnityEventBufferState onBufferingStateChanged;
            /// <summary>
            /// Buffering progress changed
            /// </summary>
            public UnityEventInt onBufferingProgressChanged;
            /// <summary>
            /// Download speed changed
            /// </summary>
            public UnityEventFloat onDownloadSpeedChanged;

            public void RemoveAllListeners()
			{
                onCacheFileSuccessful.RemoveAllListeners();
                onBufferingStateChanged.RemoveAllListeners();
                onBufferingProgressChanged.RemoveAllListeners();
                onDownloadSpeedChanged.RemoveAllListeners();
            }
        }

        [Serializable] public class UnityEventInt : UnityEvent<int> { }
        [Serializable] public class UnityEventFloat : UnityEvent<float> { }
        [Serializable] public class UnityEventBufferState : UnityEvent<BufferState> { }

        #endregion

        private string temporaryCachePath;
		private void Awake()
		{
            temporaryCachePath = Application.temporaryCachePath;
        }

		protected void SetBaseReaderParameters(Mvx2API.SingleFilterGraphNode readerGraphNode)
        {
            readerGraphNode.SetFilterParameterValue("MVX File URL", fileURL);
            readerGraphNode.SetFilterParameterValue("Employ Look-up Table", employLookupTable ? "true" : "false");
            readerGraphNode.SetFilterParameterValue("SSL Verification Mode", 
                sslVerificationMode == SSLVerificationMode.CertificateAndHost ? "Certificate&Host" :
                sslVerificationMode == SSLVerificationMode.Certificate ? "Certificate" :
                "None");
            readerGraphNode.SetFilterParameterValue("Curl Mode",
               curlMode == CurlMode.Basic ? "Basic" :
               curlMode == CurlMode.Caching ? "Caching" :
               "Basic");

            string cacheFilePath = cacheFile;
            if (Uri.IsWellFormedUriString(cacheFilePath, UriKind.Relative))
			{
                cacheFilePath = Path.Combine(temporaryCachePath, cacheFilePath);
            }
            if (!Directory.Exists(Path.GetDirectoryName(cacheFilePath)))
			{
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFilePath));
			}
            readerGraphNode.SetFilterParameterValue("MVX Cache File Path", cacheFilePath);

            readerGraphNode.SetFilterParameterValue("Slow Download Blocks Playback", slowDownloadBlocks.ToString());
        }
    }
}
