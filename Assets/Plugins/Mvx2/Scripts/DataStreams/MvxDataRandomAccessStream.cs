using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Streams/Data Random-access Stream")]
    public class MvxDataRandomAccessStream : MvxDataReaderStream
    {
        #region data

        public override bool isPaused => m_paused;

        [SerializeField, HideInInspector] protected uint m_frameId = 0;
        public virtual uint frameId
        {
            get
            {
                return m_frameId;
            }
            set
            {
                if (m_frameId == value)
                    return;

                m_frameId = value;
                if (Application.isPlaying && isActiveAndEnabled)
                    ReadFrame();
            }
        }

        #endregion

        #region stream

        public override IEnumerator InitializeStream()
        {
            yield return StartCoroutine(base.InitializeStream());
            if (isOpen)
                ReadFrame();
        }

        #endregion

        #region reader

        [System.NonSerialized] private Mvx2API.RandomAccessGraphRunner m_mvxRunner = null;
        [System.NonSerialized] private MvxDataStreamSourceRuntime m_sourceRuntime = null;
        public override MvxDataStreamSourceRuntime sourceRuntime => m_sourceRuntime;
        [System.NonSerialized] private Mvx2API.FrameAccessGraphNode m_frameAccess = null;
        protected override Mvx2API.GraphRunner mvxRunner
        {
            get { return m_mvxRunner; }
        }

        private float m_sourceFPS = 0;
        public override float sourceFPS => m_sourceFPS;

        protected override IEnumerator<bool?> OpenReader()
        {
            lastReceivedFrame = null;
            m_paused = false;

            Mvx2API.ManualGraphBuilder graphBuilder = new Mvx2API.ManualGraphBuilder();
            m_sourceRuntime = dataStreamDefinition.AppendSource(graphBuilder);

            bool work()
            {
                try
                {
                    m_frameAccess = new Mvx2API.FrameAccessGraphNode();

                    AddDataDecompressorsToGraph(graphBuilder);
                    graphBuilder = graphBuilder + m_frameAccess;
                    AddAdditionalGraphTargetsToGraph(graphBuilder);

                    m_mvxRunner = new Mvx2API.RandomAccessGraphRunner(graphBuilder.CompileGraphAndReset());

                    m_sourceFPS = m_mvxRunner.GetSourceInfo()?.GetFPS() ?? 0;

                    Debug.Log("Mvx2: The stream is open and playing");
                    return true;
                }
                catch (System.Exception exception)
                {
                    Debug.LogErrorFormat("Failed to create the graph: {0}", exception.Message);
                    m_mvxRunner = null;
                    m_sourceRuntime = null;
                    return false;
                }
            }

            if (useAsyncInit)
            {
                var task = Task<bool>.Run(work);

                while (!task.IsCompleted)
                {
                    yield return null;
                }

                yield return task.GetAwaiter().GetResult();
            }
            else
			{
                yield return work();
			}
        }

        protected override IEnumerator DisposeReader()
        {
            void work()
            {
                if (m_frameAccess != null)
                {
                    m_frameAccess.Dispose();
                    m_frameAccess = null;
                }

                if (m_sourceRuntime != null)
                    m_sourceRuntime = null;

                if (m_mvxRunner == null)
                    return;

                m_mvxRunner.Dispose();
                m_mvxRunner = null;
            }

            if (useAsyncInit)
            {
                var task = Task.Run(work);

                while (!task.IsCompleted)
                {
                    yield return null;
                }
            }
            else
			{
                work();
			}
        }

        public override void SeekFrame(uint frameID)
        {
            if (!isOpen)
                return;

            this.frameId = frameID;
        }

        private bool m_paused = true;
        public override void Pause()
        {
            m_paused = true;
        }

        public override void Resume()
        {
            m_paused = false;
        }

        #endregion

        #region frames reading

        protected void ReadFrame()
        {
            if (m_mvxRunner == null || !m_mvxRunner.ProcessFrame(frameId))
                return;

            lastReceivedFrame = new MVCommon.SharedRef<Mvx2API.Frame>(m_frameAccess.GetRecentProcessedFrame());
            if (lastReceivedFrame.sharedObj != null)
                onNextFrameReceived.Invoke(lastReceivedFrame);
        }

        #endregion
    }
}
