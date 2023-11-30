using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Streams/Simple Data Stream")]
    public class MvxSimpleDataStream : MvxDataReaderStream
    {
        #region data

        public override bool isPaused => m_paused;

        [SerializeField, HideInInspector] private Mvx2API.RunnerPlaybackMode m_playbackMode;
        public Mvx2API.RunnerPlaybackMode playbackMode
        {
            get { return m_playbackMode; }
            set
            {
                if (m_playbackMode == value)
                    return;
                m_playbackMode = value;

                if (Application.isPlaying && isActiveAndEnabled && isOpen)
                {
                    m_mvxRunner.Stop();
                    m_mvxRunner.Play(m_playbackMode);
                }
            }
        }

        [SerializeField, HideInInspector] private bool m_playAtMaxFPS = false;
        public bool playAtMaxFPS
        {
            get { return m_playAtMaxFPS; }
            set
            {
                if (m_playAtMaxFPS == value)
                    return;
                m_playAtMaxFPS = value;

                if (Application.isPlaying && isActiveAndEnabled && isOpen)
                {
                    m_fpsBlocker.SetFPS(m_playAtMaxFPS ? Mvx2API.BlockFPSGraphNode.FPS_MAX : m_sourceFPS * m_playbackSpeed);
                    OnPlaybackSpeedSettingsChanged.Invoke();
                }
            }
        }

        [SerializeField, HideInInspector] private float m_playbackSpeed = 1;
        public float playbackSpeed
        {
            get { return m_playbackSpeed; }
            set
            {
                m_playbackSpeed = Mathf.Clamp(value, 0.001f, 3);
                if (Application.isPlaying && isActiveAndEnabled && isOpen && !m_playAtMaxFPS)
                {
                    m_fpsBlocker.SetFPS(m_sourceFPS * m_playbackSpeed);
                    OnPlaybackSpeedSettingsChanged.Invoke();
                }
            }
        }

        [HideInInspector] public UnityEvent OnPlaybackSpeedSettingsChanged = new UnityEvent();

        private float m_sourceFPS = 0;
        public override float sourceFPS => m_sourceFPS;

        #endregion

        #region reader

        [System.NonSerialized] private Mvx2API.AutoSequentialGraphRunner m_mvxRunner = null;
        [System.NonSerialized] private MvxDataStreamSourceRuntime m_sourceRuntime = null;
        public override MvxDataStreamSourceRuntime sourceRuntime => m_sourceRuntime;
        [System.NonSerialized] private Mvx2API.BlockFPSGraphNode m_fpsBlocker = null;
        [System.NonSerialized] private Mvx2API.AsyncFrameAccessGraphNode m_frameAccess = null;

        protected override Mvx2API.GraphRunner mvxRunner
        {
            get { return m_mvxRunner; }
        }

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
                    m_frameAccess = new Mvx2API.AsyncFrameAccessGraphNode(new Mvx2API.DelegatedFrameListener(HandleNextFrame));
                    m_fpsBlocker = new Mvx2API.BlockFPSGraphNode(3, m_playAtMaxFPS ? Mvx2API.BlockFPSGraphNode.FPS_MAX : Mvx2API.BlockFPSGraphNode.FPS_FROM_SOURCE, Mvx2API.BlockGraphNode.FullBehaviour.FB_BLOCK_FRAMES);

                    
                    AddDataDecompressorsToGraph(graphBuilder);
                    graphBuilder = graphBuilder
                        + m_fpsBlocker
                        + m_frameAccess;
                    AddAdditionalGraphTargetsToGraph(graphBuilder);

                    m_mvxRunner = new Mvx2API.AutoSequentialGraphRunner(graphBuilder.CompileGraphAndReset());

                    if (!m_mvxRunner.Play(m_playbackMode))
                    {
                        Debug.LogError("Mvx2: Failed to play source");
                        m_mvxRunner = null;
                        m_sourceRuntime = null;
                        return false;
                    }

                    m_sourceFPS = m_mvxRunner.GetSourceInfo()?.GetFPS() ?? 0;
                    Debug.Assert(m_sourceFPS != 0, "Failed to get source FPS");

                    if (!m_playAtMaxFPS)
                        m_fpsBlocker.SetFPS(m_sourceFPS * m_playbackSpeed);

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
            if (m_mvxRunner == null)
                yield break;

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

                m_mvxRunner.Stop();
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

            m_mvxRunner.SeekFrame(frameID);
        }

        private bool m_paused = false;

        public override void Pause()
        {
            if (!isOpen)
                return;

            m_paused = true;
            m_mvxRunner.Pause();
        }

        public override void Resume()
        {
            if (!isOpen)
                return;

            m_paused = false;
            if (Application.isPlaying && isActiveAndEnabled)
                m_mvxRunner.Resume();
        }

        #endregion

        #region frames handling

        protected void HandleNextFrame(Mvx2API.Frame nextFrame)
        {
            lastReceivedFrame = new MVCommon.SharedRef<Mvx2API.Frame>(nextFrame);
            onNextFrameReceived.Invoke(lastReceivedFrame);
        }

        #endregion

        #region MonoBehaviour

        public void OnEnable()
        {
            if (Application.isPlaying && isActiveAndEnabled && isOpen && !m_paused)
                m_mvxRunner.Resume();
        }

        public void OnDisable()
        {
            if (Application.isPlaying && isOpen)
                m_mvxRunner.Pause();
        }

        #endregion
    }
}