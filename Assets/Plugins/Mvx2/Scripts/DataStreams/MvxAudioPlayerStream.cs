using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Streams/Audio Player Stream")]
    public class MvxAudioPlayerStream : MvxDataReaderStream
    {
        #region data

        public override bool isPaused => m_paused;

        public enum AudioPlaybackMode
        {
            PLAYBACKMODE_FORWARD_ONCE = Mvx2API.RunnerPlaybackMode.RPM_FORWARD_ONCE,
            PLAYBACKMODE_FORWARD_LOOP = Mvx2API.RunnerPlaybackMode.RPM_FORWARD_LOOP,
            PLAYBACKMODE_REALTIME = Mvx2API.RunnerPlaybackMode.RPM_REALTIME
        }

        [SerializeField, HideInInspector] private AudioPlaybackMode m_playbackMode = AudioPlaybackMode.PLAYBACKMODE_FORWARD_LOOP;
        public AudioPlaybackMode playbackMode
        {
            get { return m_playbackMode; }
            set
            {
                if (m_playbackMode == value)
                    return;
                m_playbackMode = value;

                if (Application.isPlaying && isActiveAndEnabled && isOpen)
                {
                    lock(m_mvxRunner)
                        m_mvxRunner.RestartWithPlaybackMode(mvxPlaybackMode);
                }
            }
        }

        private Mvx2API.RunnerPlaybackMode mvxPlaybackMode
        {
            get { return (Mvx2API.RunnerPlaybackMode)m_playbackMode; }
        }

        [SerializeField] public float minimalBufferedAudioDuration = 1f;

        private MvxAudioPlayer m_audioPlayer = new MvxAudioPlayer();

        private int m_outputSampleRate;

        [SerializeField, HideInInspector] private bool m_mute = false;
        public bool mute
        {
            get { return m_mute; }
            set
            {
                m_mute = value;
                if (m_audioSource)
                {
                    m_audioSource.mute = m_mute;
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
                if (m_audioSource)
                {
                    m_audioSource.volume = m_volume;
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
                if (m_audioSource)
                {
                    m_audioSource.pitch = m_playbackSpeed;
                }
            }
        }

        private float m_sourceFPS = 0;
        public override float sourceFPS => m_sourceFPS;

        #endregion

        #region MonoBehaviour

        private AudioSource m_audioSource = null;

        public override void Awake()
        {
            base.Awake();

            m_audioSource = gameObject.AddComponent<AudioSource>();
            m_audioSource.mute = mute;
            m_audioSource.volume = volume;
            m_audioSource.pitch = playbackSpeed;
            // AudioSettings.outputSampleRate can not be called from audio thread
            m_outputSampleRate = AudioSettings.outputSampleRate;

            lock(m_audioPlayer)
            {
                m_audioPlayer.onAudioChunkPlaybackStarted.AddListener(OnAudioChunkPlaybackStarted);
                m_audioPlayer.onAudioChunkDiscarded.AddListener(OnAudioChunkDiscarded);
            }
        }

        public override void OnDestroy()
        {
            lock(m_audioPlayer)
            {
                m_audioPlayer.Reset();
                m_audioPlayer.onAudioChunkPlaybackStarted.RemoveListener(OnAudioChunkPlaybackStarted);
                m_audioPlayer.onAudioChunkDiscarded.RemoveListener(OnAudioChunkDiscarded);
            }

            if (m_audioSource)
            {
                Destroy(m_audioSource);
                m_audioSource = null;
            }

            base.OnDestroy();
        }

        public void OnAudioFilterRead(float[] data, int channels)
        {
            if (isOpen)
            {
                lock(m_audioPlayer)
                    m_audioPlayer.DequeueAudioData(data, channels, m_outputSampleRate);
            }
        }

        public override void Update()
        {
            base.Update();

            if (!Application.isPlaying || !isActiveAndEnabled || !isOpen)
                return;

            m_outputSampleRate = AudioSettings.outputSampleRate;
        }

        public void OnEnable()
        {
            if (Application.isPlaying && isActiveAndEnabled && isOpen && !m_paused && m_audioSource)
                m_audioSource.UnPause();
        }

        public void OnDisable()
        {
            if (Application.isPlaying && isOpen && m_audioSource)
                m_audioSource.Pause();
        }

        #endregion

        #region reader

        protected override bool SupportsSourceStream(Mvx2API.SourceInfo mvxSourceInfo)
        {
            bool fileStreamSupported = mvxSourceInfo.ContainsDataLayer(Mvx2API.BasicDataLayersGuids.AUDIO_DATA_LAYER);
            if (!fileStreamSupported)
                Debug.Log("Mvx2: MvxAudioPlayerStream does not support sources without audio");
            return fileStreamSupported;
        }

        [System.NonSerialized] private Mvx2API.ManualSequentialGraphRunner m_mvxRunner = null;
        [System.NonSerialized] private MvxDataStreamSourceRuntime m_sourceRuntime = null;
        public override MvxDataStreamSourceRuntime sourceRuntime => m_sourceRuntime;
        [System.NonSerialized] private Mvx2API.FrameAccessGraphNode m_frameAccess = null;

        protected override Mvx2API.GraphRunner mvxRunner
        {
            get { return m_mvxRunner; }
        }

        protected override IEnumerator<bool?> OpenReader()
        {
            lastReceivedFrame = null;
            m_paused = false;
            if (m_audioSource)
                m_audioSource.enabled = true;
            lock(m_audioPlayer)
                m_audioPlayer.Reset();

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

                    m_mvxRunner = new Mvx2API.ManualSequentialGraphRunner(graphBuilder.CompileGraphAndReset());

                    if (!m_mvxRunner.RestartWithPlaybackMode(mvxPlaybackMode))
                    {
                        Debug.LogError("Mvx2: Failed to play source");
                        m_mvxRunner = null;
                        m_sourceRuntime = null;
                        return false;
                    }

                    m_sourceFPS = m_mvxRunner.GetSourceInfo()?.GetFPS() ?? 0;

                    Debug.Log("Mvx2: The stream is open and playing");

                    m_stopReadingFrames = false;
                    m_framesReadingThread = new Thread(new ThreadStart(ReadFrames));
                    m_framesReadingThread.Start();

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
                if (m_mvxRunner == null)
                    return;

                if (m_framesReadingThread != null)
                {
                    m_stopReadingFrames = true;
                    m_framesReadingThread.Join();
                    m_framesReadingThread = null;
                }

                if (m_frameAccess != null)
                {
                    m_frameAccess.Dispose();
                    m_frameAccess = null;
                }

                if (m_sourceRuntime != null)
                    m_sourceRuntime = null;

                m_mvxRunner.Dispose();
                m_mvxRunner = null;

                lock (m_audioPlayer)
                    m_audioPlayer.Reset();
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

        private bool m_stopReadingFrames = false;
        /// <summary> A thread for reading MVX frames when needed. </summary>
        /// <remarks> The thread is necessary to relieve the execution time of Update(). </remarks>
        private Thread m_framesReadingThread;

        private void ReadFrames()
        {
            while (true)
            {
                if (m_stopReadingFrames)
                    return;

                float queuedAudioDuration;
                lock (m_audioPlayer)
                    queuedAudioDuration = m_audioPlayer.GetQueuedAudioDuration(m_outputSampleRate);

                while (queuedAudioDuration < minimalBufferedAudioDuration)
                {
                    if (m_stopReadingFrames)
                        return;

                    MVCommon.SharedRef<Mvx2API.Frame> newFrame = null;
                    lock (m_mvxRunner)
                    {
                        if (!m_mvxRunner.ProcessNextFrame())
                            break;

                        newFrame = new MVCommon.SharedRef<Mvx2API.Frame>(m_frameAccess.GetRecentProcessedFrame());
                    }
                    if (newFrame.sharedObj == null)
                        break;

                    MvxAudioChunk newAudioChunk = ExtractAudioFromFrame(newFrame);
                    if (newAudioChunk == null)
                    {
                        newFrame.Dispose();
                        break;
                    }

                    lock (m_audioChunkFramesQueue)
                    {
                        m_audioChunkFramesQueue.Enqueue(new KeyValuePair<MvxAudioChunk, MVCommon.SharedRef<Mvx2API.Frame>>(newAudioChunk, newFrame));
                        m_allocatedAudioChunks.Add(newAudioChunk);
                    }

                    lock (m_audioPlayer)
                    {
                        m_audioPlayer.EnqueueAudioChunk(newAudioChunk);
                        queuedAudioDuration = m_audioPlayer.GetQueuedAudioDuration(m_outputSampleRate);
                    }
                }

                Thread.Sleep(10);
            }
        }

        public override void SeekFrame(uint frameID)
        {
            if (!isOpen)
                return;

            lock (m_audioChunkFramesQueue)
            {
                while (m_audioChunkFramesQueue.Count > 0)
                {
                    KeyValuePair<MvxAudioChunk, MVCommon.SharedRef<Mvx2API.Frame>> dequeuedAudioChunkFrame = m_audioChunkFramesQueue.Dequeue();
                    dequeuedAudioChunkFrame.Value.Dispose();
                }
                m_allocatedAudioChunks.Clear();
            }
                
            lock (m_audioPlayer)
                m_audioPlayer.Reset();

            lock (m_mvxRunner)
                m_mvxRunner.SeekFrame(frameID);
        }

        private bool m_paused = false;

        public override void Pause()
        {
            if (!isOpen)
                return;

            m_paused = true;
            if (m_audioSource)
                m_audioSource.Pause();
        }

        public override void Resume()
        {
            if (!isOpen)
                return;

            m_paused = false;
            if (Application.isPlaying && isActiveAndEnabled && m_audioSource)
                m_audioSource.UnPause();
        }

        #endregion

        #region frames processing

        private Queue<KeyValuePair<MvxAudioChunk, MVCommon.SharedRef<Mvx2API.Frame>>> m_audioChunkFramesQueue = new Queue<KeyValuePair<MvxAudioChunk, MVCommon.SharedRef<Mvx2API.Frame>>>();
        /// <summary> An auxiliary collection of allocated audio chunks for fast 'contains' operation. </summary>
        private HashSet<MvxAudioChunk> m_allocatedAudioChunks = new HashSet<MvxAudioChunk>();

        unsafe private MvxAudioChunk ExtractAudioFromFrame(MVCommon.SharedRef<Mvx2API.Frame> frame)
        {
            if (frame == null)
                return null;

            UInt32 framePCMDataSize = Mvx2API.FrameAudioExtractor.GetPCMDataSize(frame.sharedObj);
            if (framePCMDataSize == 0)
                return null;

            UInt32 frameChannelsCount;
            UInt32 frameBitsPerSample;
            UInt32 frameSampleRate;
            Mvx2API.FrameAudioExtractor.GetAudioSamplingInfo(frame.sharedObj, out frameChannelsCount, out frameBitsPerSample, out frameSampleRate);
            if (frameBitsPerSample != 8 && frameBitsPerSample != 16 && frameBitsPerSample != 32)
            {
                Debug.LogErrorFormat("Unsupported 'bits per sample' value {0}", frameBitsPerSample);
                return null;
            }
            UInt32 frameBytesPerSample = frameBitsPerSample / 8;

            byte[] frameAudioBytes = new byte[framePCMDataSize];
            fixed (byte* frameAudioBytesPtr = frameAudioBytes)
                Mvx2API.FrameAudioExtractor.CopyPCMDataRaw(frame.sharedObj, (IntPtr)frameAudioBytesPtr);
            
            return MvxAudioChunksPool.instance.AllocateAudioChunk(frameAudioBytes, frameBytesPerSample, frameChannelsCount, frameSampleRate);
        }

        private void OnAudioChunkPlaybackStarted(MvxAudioChunk audioChunk)
        {
            MVCommon.SharedRef<Mvx2API.Frame> newLastReceivedFrame = null;

            lock (m_audioChunkFramesQueue)
            {
                KeyValuePair<MvxAudioChunk, MVCommon.SharedRef<Mvx2API.Frame>> queuedAudioChunkFrame;
                while (m_audioChunkFramesQueue.Count > 0)
                {
                    // find the frame associated with the audio chunk being played right now, discard preceding frames
                    queuedAudioChunkFrame = m_audioChunkFramesQueue.Dequeue();
                    m_allocatedAudioChunks.Remove(queuedAudioChunkFrame.Key);

                    if (queuedAudioChunkFrame.Key == audioChunk)
                    {
                        newLastReceivedFrame = queuedAudioChunkFrame.Value;
                        break;
                    }
                    else
                    {
                        queuedAudioChunkFrame.Value.Dispose();
                    }
                }
            }

            if (newLastReceivedFrame != null)
            {
                lastReceivedFrame = newLastReceivedFrame;
                onNextFrameReceived.Invoke(lastReceivedFrame);
            }
        }

        private void OnAudioChunkDiscarded(MvxAudioChunk discardedAudioChunk)
        {
            lock (m_audioChunkFramesQueue)
            {
                // discard frame of the audio chunk and preceding frames, if the audio chunk was never played back
                if (!m_allocatedAudioChunks.Contains(discardedAudioChunk))
                {
                    MvxAudioChunksPool.instance.ReturnAudioChunk(discardedAudioChunk);
                    return;
                }
                    
                KeyValuePair<MvxAudioChunk, MVCommon.SharedRef<Mvx2API.Frame>> queuedAudioChunkFrame;
                while (m_audioChunkFramesQueue.Count > 0)
                {
                    queuedAudioChunkFrame = m_audioChunkFramesQueue.Dequeue();
                    m_allocatedAudioChunks.Remove(queuedAudioChunkFrame.Key);
                    MvxAudioChunksPool.instance.ReturnAudioChunk(queuedAudioChunkFrame.Key);
                    queuedAudioChunkFrame.Value.Dispose();

                    if (queuedAudioChunkFrame.Key == discardedAudioChunk)
                        break;
                }
            }
        }

        #endregion
    }
}