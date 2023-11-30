using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace MVXUnity
{
    /// <summary>
    /// Switches between audio samplerate-based and simple framerate-based streamers according to properties of the streamed source and preferences set.
    /// </summary>
    /// <remarks>
    /// For sources containing audio data, the stream supporting audio playback is preferred unless the audio playback is forbidden via property.
    /// </remarks>
    [AddComponentMenu("Mvx2/Data Streams/Data Stream Determiner")]
    public class MvxDataStreamDeterminer : MvxDataStream
    {
        #region data

        public override uint framesCount => currentStream == null ? 0 : currentStream.framesCount;

        public override bool isSingleFrameSource => currentStream == null ? false : currentStream.isSingleFrameSource;

        public override bool isOpen => (m_audioStream != null && m_audioStream.isOpen) || (m_simpleStream != null && m_simpleStream.isOpen);

		public override bool isInitializing => (m_audioStream != null && m_audioStream.isInitializing) || (m_simpleStream != null && m_simpleStream.isInitializing);

        public override bool isPaused => (m_audioStream != null && m_audioStream.isPaused) || (m_simpleStream != null && m_simpleStream.isPaused);

        private bool m_isInitializing = false;

        [SerializeField, HideInInspector] private Mvx2API.RunnerPlaybackMode m_playbackMode;
        public Mvx2API.RunnerPlaybackMode playbackMode
        {
            get { return m_playbackMode; }
            set
            {
                if (m_playbackMode == value)
                    return;
                m_playbackMode = value;

                if (m_audioStream != null)
                    m_audioStream.playbackMode = GetSupportedAudioPlaybackMode(playbackMode);
                if (m_simpleStream != null)
                    m_simpleStream.playbackMode = m_playbackMode;
            }
        }

        private static MvxAudioPlayerStream.AudioPlaybackMode GetSupportedAudioPlaybackMode(Mvx2API.RunnerPlaybackMode playbackMode)
        {
            if (playbackMode == Mvx2API.RunnerPlaybackMode.RPM_FORWARD_LOOP || playbackMode == Mvx2API.RunnerPlaybackMode.RPM_BACKWARD_LOOP)
                return MvxAudioPlayerStream.AudioPlaybackMode.PLAYBACKMODE_FORWARD_LOOP;
            if (playbackMode == Mvx2API.RunnerPlaybackMode.RPM_FORWARD_ONCE || playbackMode == Mvx2API.RunnerPlaybackMode.RPM_BACKWARD_ONCE)
                return MvxAudioPlayerStream.AudioPlaybackMode.PLAYBACKMODE_FORWARD_ONCE;
            if (playbackMode == Mvx2API.RunnerPlaybackMode.RPM_REALTIME)
                return MvxAudioPlayerStream.AudioPlaybackMode.PLAYBACKMODE_REALTIME;

            return MvxAudioPlayerStream.AudioPlaybackMode.PLAYBACKMODE_FORWARD_LOOP;
        }

        [SerializeField, HideInInspector] private bool m_playAtMaxFPS = true;
        public bool playAtMaxFPS
        {
            get { return m_playAtMaxFPS; }
            set
            {
                if (m_playAtMaxFPS == value)
                    return;
                m_playAtMaxFPS = value;

                if (m_simpleStream != null)
                    m_simpleStream.playAtMaxFPS = m_playAtMaxFPS;
            }
        }

        public override float sourceFPS
        {
            get
            {
                if (m_audioStream != null && m_audioStream.isOpen)
                    return m_audioStream.sourceFPS;

                if (m_simpleStream != null && m_simpleStream.isOpen)
                    return m_simpleStream.sourceFPS;

                return 0;
            }
        }

        [SerializeField, HideInInspector] private float m_minimalBufferedAudioDuration = 1f;
        public float minimalBufferedAudioDuration
        {
            get { return m_minimalBufferedAudioDuration; }
            set
            {
                if (m_minimalBufferedAudioDuration == value)
                    return;
                m_minimalBufferedAudioDuration = value;

                if (m_audioStream != null)
                    m_audioStream.minimalBufferedAudioDuration = m_minimalBufferedAudioDuration;
            }
        }


        [SerializeField, HideInInspector] private bool m_mute = false;
        public bool mute
        {
            get { return m_mute; }
            set
            {
                m_mute = value;
                if (m_audioStream)
                {
                    m_audioStream.mute = m_mute;
                }
            }
        }

        [SerializeField, HideInInspector] private float m_volume = 1f;
        public float volume
        {
            get { return m_volume; }
            set
            {
                m_volume = Mathf.Clamp01(value);
                if (m_audioStream)
                {
                    m_audioStream.volume = m_volume;
                }
            }
        }

        [SerializeField, HideInInspector] private float m_playbackSpeed = 1;
        public float playbackSpeed
        {
            get { return m_playbackSpeed; }
            set
            {
                m_playbackSpeed = Mathf.Clamp(value, 0, 3);
                if (m_audioStream)
                {
                    m_audioStream.playbackSpeed = m_playbackSpeed;
                }
                if (m_simpleStream)
                {
                    m_simpleStream.playbackSpeed = m_playbackSpeed;
                }
            }
        }

        #endregion

        #region nested streams data

        [SerializeField, HideInInspector] private bool m_audioStreamEnabled = true;
        public bool audioStreamEnabled
        {
            get { return m_audioStreamEnabled; }
            set
            {
                if (m_audioStreamEnabled == value)
                    return;
                m_audioStreamEnabled = value;

                if (Application.isPlaying && isActiveAndEnabled && isOpen && autoPlay)
                    RestartStream();
            }
        }

        private MvxSimpleDataStream m_simpleStream = null;
        private MvxAudioPlayerStream m_audioStream = null;

        public MvxDataStream currentStream
        {
            get
            {
                if (m_audioStream != null && m_audioStream.isOpen)
                    return m_audioStream;

                if (m_simpleStream != null && m_simpleStream.isOpen)
                    return m_simpleStream;

                return null;
            }
        }

        #endregion

        #region events propagation

        private void OnNestedStreamOpenedStream()
        {
            onStreamOpen.Invoke();
        }

        private void OnNestedStreamReceivedNextFrame(MVCommon.SharedRef<Mvx2API.Frame> nextFrame)
        {
            lastReceivedFrame = nextFrame;
            onNextFrameReceived.Invoke(nextFrame);
        }

        #endregion

        #region stream

        public System.Action OnInitializeStart;
        public System.Action<bool> OnInitializeEnd;

		public override IEnumerator InitializeStream()
        {
            if (isOpen || m_isInitializing)
                yield break;

            lastReceivedFrame = null;

            OnInitializeStart?.Invoke();

            if (m_audioStreamEnabled)
            {
                m_audioStream.enabled = true;
                m_simpleStream.enabled = false;

                Debug.Log("Mvx2: Audio stream enabled, will attempt to run the source with audio");
                m_audioStream.mute = mute;
                m_audioStream.volume = volume;
                m_audioStream.autoPlay = autoPlay;
                m_audioStream.playbackSpeed = playbackSpeed;
                m_audioStream.useAsyncInit = useAsyncInit;
                m_audioStream.playbackMode = GetSupportedAudioPlaybackMode(playbackMode);
                m_audioStream.minimalBufferedAudioDuration = minimalBufferedAudioDuration;
                m_audioStream.additionalTargets = additionalTargets;
                m_audioStream.dataDecompressors = dataDecompressors;
                m_audioStream.dataStreamDefinition = dataStreamDefinition;
                yield return StartCoroutine(m_audioStream.InitializeStream());

                if (m_audioStream.isOpen)
                {
                    m_isInitializing = false;
                    OnInitializeEnd?.Invoke(true);
                    yield break;
                }
                else
                {
                    m_audioStream.dataStreamDefinition = null;
                    yield return m_audioStream.DisposeStream();
                    Debug.Log("Mvx: Failed to run audio stream, simple frame-rate based stream will be used");

                    yield return new WaitForSeconds(0.3f);
                }
            }
            else
            {
                Debug.Log("Mvx2: Audio stream disabled, simple frame-rate based stream will be used");
            }

            m_audioStream.enabled = false;
            m_simpleStream.enabled = true;

            m_simpleStream.autoPlay = autoPlay;
            m_simpleStream.playAtMaxFPS = playAtMaxFPS;
            m_simpleStream.playbackSpeed = playbackSpeed;
            m_simpleStream.useAsyncInit = useAsyncInit;
            m_simpleStream.playbackMode = playbackMode;
            m_simpleStream.playAtMaxFPS = playAtMaxFPS;
            m_simpleStream.additionalTargets = additionalTargets;
            m_simpleStream.dataDecompressors = dataDecompressors;
            m_simpleStream.dataStreamDefinition = dataStreamDefinition;
            yield return StartCoroutine(m_simpleStream.InitializeStream());

            
            OnInitializeEnd?.Invoke(m_simpleStream.isOpen);

            m_isInitializing = false;
        }

        public override IEnumerator DisposeStream()
        {
            if (m_audioStream != null)
                yield return m_audioStream.DisposeStream();
            if (m_simpleStream != null)
                yield return m_simpleStream.DisposeStream();
        }

        public override void SeekFrame(uint frameID)
        {
            if (m_audioStream != null)
                m_audioStream.SeekFrame(frameID);
            if (m_simpleStream != null)
                m_simpleStream.SeekFrame(frameID);
        }

        public override void Pause()
        {
            if (m_audioStream != null)
                m_audioStream.Pause();
            if (m_simpleStream != null)
                m_simpleStream.Pause();
        }

        public override void Resume()
        {
            if (m_audioStream != null)
                m_audioStream.Resume();
            if (m_simpleStream != null)
                m_simpleStream.Resume();
        }

        #endregion

        #region MonoBehaviour

        public override void Awake()
        {
            base.Awake();

            m_simpleStream = gameObject.AddComponent<MvxSimpleDataStream>();
            m_simpleStream.onStreamOpen.AddListener(OnNestedStreamOpenedStream);
            m_simpleStream.onNextFrameReceived.AddListener(OnNestedStreamReceivedNextFrame);
            m_audioStream = gameObject.AddComponent<MvxAudioPlayerStream>();
            m_audioStream.onStreamOpen.AddListener(OnNestedStreamOpenedStream);
            m_audioStream.onNextFrameReceived.AddListener(OnNestedStreamReceivedNextFrame);
        }

        public override void OnDestroy()
        {
            if (m_simpleStream != null)
            {
                m_simpleStream.onStreamOpen.RemoveListener(OnNestedStreamOpenedStream);
                m_simpleStream.onNextFrameReceived.RemoveListener(OnNestedStreamReceivedNextFrame);
                Destroy(m_simpleStream);
                m_simpleStream = null;
            }

            if (m_audioStream != null)
            {
                m_audioStream.onStreamOpen.RemoveListener(OnNestedStreamOpenedStream);
                m_audioStream.onNextFrameReceived.RemoveListener(OnNestedStreamReceivedNextFrame);
                Destroy(m_audioStream);
                m_audioStream = null;
            }

            base.OnDestroy();
        }

        public void OnEnable()
        {
            m_simpleStream.enabled = true;
            m_audioStream.enabled = true;
        }

        public void OnDisable()
        {
            m_simpleStream.enabled = false;
            m_audioStream.enabled = false;
        }

        #endregion
    }
}
