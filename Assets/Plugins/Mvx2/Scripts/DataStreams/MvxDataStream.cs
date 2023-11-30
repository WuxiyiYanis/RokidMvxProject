using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MVXUnity
{
    public abstract class MvxDataStream : MonoBehaviour
    {
        #region data

        [Tooltip("Play the clip automatically on start or when the definition has changed")]
        public bool autoPlay = true;

        [Tooltip("Create and dispose the stream asynchronously")]
        public bool useAsyncInit = true;

        public abstract bool isPaused { get; }


        [SerializeField, HideInInspector] private MvxDataStreamDefinition m_dataStreamDefinition;
        public virtual MvxDataStreamDefinition dataStreamDefinition
        {
            get { return m_dataStreamDefinition; }
            set
            {
                if (m_dataStreamDefinition == value)
                    return;

                if (m_dataStreamDefinition != null)
                    m_dataStreamDefinition.onDefinitionChanged.RemoveListener(TryRestartStream);

                m_dataStreamDefinition = value;

                if (m_dataStreamDefinition != null)
                    m_dataStreamDefinition.onDefinitionChanged.AddListener(TryRestartStream);

                if (Application.isPlaying && isActiveAndEnabled && autoPlay)
                {
                    RestartStream();
                }
			}
        }

        protected internal MvxDataDecompressor[] dataDecompressors = null;
        protected void AddDataDecompressorsToGraph(Mvx2API.ManualGraphBuilder graphBuilder)
        {
            if (dataDecompressors == null)
                return;

            foreach (MvxDataDecompressor dataDecompressor in dataDecompressors)
                dataDecompressor.AppendDecompressor(graphBuilder);
        }

        [SerializeField] public MvxTarget[] additionalTargets = null;
        protected void AddAdditionalGraphTargetsToGraph(Mvx2API.ManualGraphBuilder graphBuilder)
        {
            if (additionalTargets == null)
                return;

            foreach (MvxTarget additionalTarget in additionalTargets)
                graphBuilder = graphBuilder + additionalTarget.GetGraphNode();
        }

        public abstract float sourceFPS { get; }
        public abstract uint framesCount { get; }
        public abstract bool isSingleFrameSource { get; }

        public abstract bool isOpen { get; }
        public abstract bool isInitializing { get; }

        public uint lastReceivedFrameNr { get; protected set; } = 0;

        private MVCommon.SharedRef<Mvx2API.Frame> m_lastReceivedFrame;
        public MVCommon.SharedRef<Mvx2API.Frame> lastReceivedFrame
        {
            get { return m_lastReceivedFrame; }
            protected set
            {
                if (m_lastReceivedFrame != null)
                    m_lastReceivedFrame.Dispose();

                m_lastReceivedFrame = value;

                if (m_lastReceivedFrame != null && m_lastReceivedFrame.sharedObj != null)
                {
                    uint frameNr = m_lastReceivedFrame.sharedObj.GetStreamAtomNr();
                    if (frameNr == framesCount - 1)
					{
                        onPlaybackEnd.Invoke();
                    }
                    else if (frameNr < lastReceivedFrameNr)
					{
                        onPlaybackRestarted.Invoke();
					}
                    lastReceivedFrameNr = frameNr;
                }
            }
        }

        #endregion

        #region frames handling

        [System.Serializable] public class StreamOpenEvent : UnityEvent { }
        [SerializeField, HideInInspector] public StreamOpenEvent onStreamOpen = new StreamOpenEvent();
        
        [System.Serializable] public class NextFrameReceivedEvent : UnityEvent<MVCommon.SharedRef<Mvx2API.Frame>> { }
        [SerializeField, HideInInspector] public NextFrameReceivedEvent onNextFrameReceived = new NextFrameReceivedEvent();

        [System.Serializable] public class PlaybackRestartedEvent : UnityEvent { }
        [HideInInspector] public PlaybackRestartedEvent onPlaybackRestarted = new PlaybackRestartedEvent();

        [System.Serializable] public class PlaybackEndEvent : UnityEvent { }
        [HideInInspector] public PlaybackEndEvent onPlaybackEnd = new PlaybackEndEvent();

        #endregion

        #region stream

        private void TryRestartStream()
        {
            if (Application.isPlaying && isActiveAndEnabled && autoPlay)
                RestartStream();
        }

        protected void RestartStream()
        {
            IEnumerator Restart()
			{
                yield return DisposeStream();
                yield return InitializeStream();
            }

            StartCoroutine(Restart());
        }

        public abstract IEnumerator InitializeStream();
        public abstract IEnumerator DisposeStream();

        public abstract void SeekFrame(uint frameID);

        public abstract void Pause();
        public abstract void Resume();

        public void Play()
		{
            RestartStream();
		}

        public void Play(MvxDataStreamDefinition streamDefinition)
        {
            bool prevAutoPlay = autoPlay;
            autoPlay = false;
            dataStreamDefinition = streamDefinition;
            autoPlay = prevAutoPlay;
            RestartStream();
        }

        #endregion

        #region MonoBehaviour

        public virtual void Awake()
        {
            MvxPluginsLoader.LoadPlugins();

            dataDecompressors = Resources.LoadAll<MvxDataDecompressor>("MvxDataDecompressors");
        }

        public virtual void Start()
        {
            if (m_dataStreamDefinition != null)
                m_dataStreamDefinition.onDefinitionChanged.AddListener(TryRestartStream);

            if (autoPlay)
                StartCoroutine(InitializeStream());
        }

        public virtual void OnDestroy()
        {
            GameObject go = new GameObject();
            go.name = "[Worker]";
            go.hideFlags = HideFlags.HideAndDontSave;
            var worker = go.AddComponent<Worker>();

            IEnumerator Work()
			{
                yield return DisposeStream();
                Destroy(go);
            }
            worker.StartCoroutine(Work());
        }

        class Worker : MonoBehaviour { }

		private void OnApplicationQuit()
		{
            Stack<IEnumerator> enumerators = new Stack<IEnumerator>();
            enumerators.Push(DisposeStream());
            while (enumerators.Count > 0)
            {
                while (enumerators.Peek().MoveNext())
                {
                    if (enumerators.Peek().Current is IEnumerator e)
                    {
                        enumerators.Push(e);
                    }
                }

                enumerators.Pop();
            }
        }

		public virtual void Update()
        {
            if (!isOpen)
                return;

            if (isSingleFrameSource && lastReceivedFrame != null)
            {
                StartCoroutine(DisposeStream());
            }
        }

        #endregion
    }
}