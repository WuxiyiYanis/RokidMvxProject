using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MVXUnity
{
    public abstract class MvxDataReaderStream : MvxDataStream
    {
        #region data

        private uint m_framesCount = 0; 
        public override uint framesCount => m_framesCount;
        public override bool isSingleFrameSource => m_framesCount == 1;

        private bool m_isOpen = false;
        public override bool isOpen => m_isOpen;
        public override bool isInitializing => m_isInitializing;

        public abstract MvxDataStreamSourceRuntime sourceRuntime { get; }

        private bool m_isInitializing = false;
        private bool m_isDisposing = false;

        #endregion

        #region stream

        /// <summary> 
        /// Note: Make sure you are not initializing or disposing mvx streams on the same source at the same time
        /// </summary>
        public override IEnumerator InitializeStream()
        {
            if (isOpen || m_isInitializing || m_isDisposing)
            {
                Debug.Log("Initialize stream interrupted, is already open/initializing/disposing");
                yield break;
            }

            m_isDisposing = false;
            m_isInitializing = true;

            m_framesCount = 0;
            lastReceivedFrameNr = 0;

            if (dataStreamDefinition == null)
            {
                Debug.LogWarning("Mvx2: Missing data stream definition");
                m_isInitializing = false;
                yield break;
            }

            bool? result = null;
            using (IEnumerator<bool?> openReader = OpenReader())
            {
                while (openReader.MoveNext())
                {
                    result = openReader.Current;
                    if (useAsyncInit) yield return null;
                }
            }

            if (!result.HasValue || !result.Value)
            {
                IEnumerator disposeReader = DisposeReader();
                while (disposeReader.MoveNext())
                {
                    yield return null;
                }
                m_isInitializing = false;
                yield break;
            }

            Mvx2API.SourceInfo mvxSourceInfo = ExtractSourceInfo();
            if (mvxSourceInfo == null)
            {
                IEnumerator disposeReader = DisposeReader();
                while (disposeReader.MoveNext())
                {
                    yield return null;
                }
                m_isInitializing = false;
                Debug.LogError("Could not extract source info");
                yield break;
            }

            uint streamFramesCount = mvxSourceInfo.GetNumFrames();
            
            bool sourceStreamSupported = SupportsSourceStream(mvxSourceInfo);
            mvxSourceInfo.Dispose();
            if (!sourceStreamSupported)
            {
                IEnumerator disposeReader = DisposeReader();
                while (disposeReader.MoveNext())
                {
                    yield return null;
                }
                m_isInitializing = false;
                Debug.LogError("Does not support source stream");
                yield break;
            }

            m_framesCount = streamFramesCount;
            m_isOpen = true;
            onStreamOpen.Invoke();
            m_isInitializing = false;
        }

        /// <summary> 
        /// Note: Make sure you are not initializing or disposing mvx streams on the same source at the same time
        /// </summary>
        public override IEnumerator DisposeStream()
        {
            while (m_isDisposing || m_isInitializing) 
                yield return null;

            if (!isOpen)
                yield break;

            m_isDisposing = true;

            if (useAsyncInit)
            {
                yield return DisposeReader();
            }
            else
			{
                DisposeReader().MoveNext();
			}

            m_isDisposing = false;
            m_isOpen = false;
        }

        #endregion

        #region source info

        private Mvx2API.SourceInfo ExtractSourceInfo()
        {
            Debug.Log("Mvx2: Extracting source info from source");

            Mvx2API.SourceInfo mvxSourceInfo = mvxRunner.GetSourceInfo();
            if (mvxSourceInfo == null)
                return null;

            return mvxSourceInfo;
        }

        protected virtual bool SupportsSourceStream(Mvx2API.SourceInfo mvxSourceInfo)
        {
            return true;
        }

        #endregion

        #region reader

        protected abstract IEnumerator<bool?> OpenReader();
        protected abstract IEnumerator DisposeReader();
        protected abstract Mvx2API.GraphRunner mvxRunner
        {
            get;
        }

        #endregion

        #region MonoBehaviour

        public override void Update()
        {
            base.Update();

            if (!isOpen || sourceRuntime == null)
                return;

            sourceRuntime.Update();
            if (sourceRuntime.StreamEnded())
            {
                StartCoroutine(DisposeStream());
            }
        }

        #endregion
    }
}