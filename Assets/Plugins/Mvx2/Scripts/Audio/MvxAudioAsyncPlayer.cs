using System;
using UnityEngine;
using UnityEngine.Events;

namespace MVXUnity
{
    /// <summary> Player of standard Windows PCM format audio. </summary>
    /// <remarks> 1-byte per sample -> unsigned; 2-bytes per sample -> signed, 4-bytes per sample -> signed</remarks>
    [AddComponentMenu("Mvx2/Data Processors/Audio Async Player")]
    public class MvxAudioAsyncPlayer : MvxAsyncDataProcessor
    {
        #region data 

        private const int AUDIO_CHUNKS_BUFFER_SIZE = 10;
        private MvxAudioPlayer m_audioPlayer = new MvxAudioPlayer(AUDIO_CHUNKS_BUFFER_SIZE);

        private int m_outputSampleRate;

        private AudioSource m_audioSource;

        private UnityEvent currentMvxPlaybackChangedEvent;

        [SerializeField] private bool m_mute = false;
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

        [SerializeField, Range(0, 1)] private float m_volume = 1f;
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

        #endregion

        #region MonoBehaviour


        private void OnValidate()
        {
            volume = m_volume;
            mute = m_mute;
        }

        public void Awake()
        {
            m_audioPlayer.onAudioChunkDiscarded.AddListener(OnAudioChunkDiscarded);

            m_audioSource = gameObject.AddComponent<AudioSource>();
            m_audioSource.mute = mute;
            m_audioSource.volume = volume;
            // AudioSettings.outputSampleRate can not be called from audio thread
            m_outputSampleRate = AudioSettings.outputSampleRate;
        }

        public override void OnDestroy()
        {
            m_audioPlayer.Reset();
            m_audioPlayer.onAudioChunkDiscarded.RemoveListener(OnAudioChunkDiscarded);

            ClearPlaybackSpeedEvent();
        }

        public override void Update()
        {
            m_outputSampleRate = AudioSettings.outputSampleRate;

            if (mvxStream == null)
                ClearPlaybackSpeedEvent();

            base.Update();
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            lock(m_audioPlayer)
                m_audioPlayer.DequeueAudioData(data, channels, m_outputSampleRate);
        }

        #endregion

        #region process frame

        protected override bool CanProcessFrame(Mvx2API.Frame frame)
        {
            return frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.AUDIO_DATA_LAYER, false);
        }

        protected override void ResetProcessedData()
        {
            lock(m_audioPlayer)
                m_audioPlayer.Reset();

            ClearPlaybackSpeedEvent();

            if (mvxStream is MvxSimpleDataStream simple)
            {
                currentMvxPlaybackChangedEvent = simple.OnPlaybackSpeedSettingsChanged;
                simple.OnPlaybackSpeedSettingsChanged.AddListener(UpdatePlaybackSpeed);

                UpdatePlaybackSpeed();
            }
        }

        private void ClearPlaybackSpeedEvent()
        {
            if (currentMvxPlaybackChangedEvent != null)
            {
                currentMvxPlaybackChangedEvent.RemoveListener(UpdatePlaybackSpeed);
                currentMvxPlaybackChangedEvent = null;
            }
        }

        private void UpdatePlaybackSpeed()
        {
            if (mvxStream is MvxSimpleDataStream simple && !simple.playAtMaxFPS)
                m_audioSource.pitch = simple.playbackSpeed;
            else
                m_audioSource.pitch = 1;
        }


        protected override void ProcessNextFrame(MVCommon.SharedRef<Mvx2API.Frame> frame)
        {
            ProcessAudioData(frame.sharedObj);
        }

        unsafe private void ProcessAudioData(Mvx2API.Frame frame)
        {
            UInt32 framePCMDataSize = Mvx2API.FrameAudioExtractor.GetPCMDataSize(frame);
            if (framePCMDataSize == 0)
                return;

            UInt32 frameChannelsCount;
            UInt32 frameBitsPerSample;
            UInt32 frameSampleRate;
            Mvx2API.FrameAudioExtractor.GetAudioSamplingInfo(frame, out frameChannelsCount, out frameBitsPerSample, out frameSampleRate);
            if (frameBitsPerSample != 8 && frameBitsPerSample != 16 && frameBitsPerSample != 32)
            {
                Debug.LogErrorFormat("Unsupported 'bits per sample' value {0}", frameBitsPerSample);
                return;
            }
            UInt32 frameBytesPerSample = frameBitsPerSample / 8;

            byte[] frameAudioBytes = new byte[framePCMDataSize];
            fixed (byte* frameAudioBytesPtr = frameAudioBytes)
                Mvx2API.FrameAudioExtractor.CopyPCMDataRaw(frame, (IntPtr)frameAudioBytesPtr);

            MvxAudioChunk newAudioChunk = MvxAudioChunksPool.instance.AllocateAudioChunk(frameAudioBytes, frameBytesPerSample, frameChannelsCount, frameSampleRate);
            lock (m_audioPlayer)
                m_audioPlayer.EnqueueAudioChunk(newAudioChunk);
        }

        private void OnAudioChunkDiscarded(MvxAudioChunk discardedAudioChunk)
        {
            MvxAudioChunksPool.instance.ReturnAudioChunk(discardedAudioChunk);
        }

        #endregion
    }
}