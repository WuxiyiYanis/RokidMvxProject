using System;
using System.Globalization;
using UnityEngine;

namespace MVXUnity
{
    public class MvxRemoteFileDataStreamSourceRuntime : MvxDataStreamSourceRuntime
    {
        private Mvx2API.SingleFilterGraphNode m_readerGraphNode = null;
        private MvxRemoteFileDataStreamDefinition.StreamEvents m_events;

        private bool m_lookupTablePresent = false;
        public bool lookupTablePresent => m_lookupTablePresent;

        private bool m_realtimeStreamEnded = false;
        public bool realtimeStreamEnded => m_realtimeStreamEnded;

        private bool m_cacheFileSuccessful = false;
		public bool cacheFileSuccessful => m_cacheFileSuccessful;

        private float m_downloadSpeed = 0f;
        public float downloadSpeed => m_downloadSpeed;


        private MvxRemoteFileDataStreamDefinition.BufferState m_bufferState = MvxRemoteFileDataStreamDefinition.BufferState.NONE;
        public MvxRemoteFileDataStreamDefinition.BufferState bufferState => m_bufferState;

        private int m_bufferProgress = 0;
        public int bufferProgress => m_bufferProgress;

        public MvxRemoteFileDataStreamSourceRuntime(Mvx2API.SingleFilterGraphNode readerGraphNode, MvxRemoteFileDataStreamDefinition.StreamEvents events)
        {
            m_readerGraphNode = readerGraphNode;
            m_events = events;
        }

        public override bool StreamEnded()
        {
            return realtimeStreamEnded;
        }

        const string LOOKUP_TABLE_PRESENT_PARAMNAME = "Look-up Table Present";
        const string REALTIME_STREAM_ENDED_PARAMNAME = "Realtime Stream Ended";
        const string CACHE_SUCCESSFUL_PARAMNAME = "MVX Cache File Successful";
        const string BUFFERING_STATE_PARAMNAME = "Buffering State";
        const string BUFFER_PROGRESS_PARAMNAME = "Buffering Progress Percentage";
        const string DOWNLOAD_SPEED_PARAMNAME = "Downloading Speed";

        public override void Update()
        {
            m_readerGraphNode.TryGetParameterValue(LOOKUP_TABLE_PRESENT_PARAMNAME, ref m_lookupTablePresent);

            m_readerGraphNode.TryGetParameterValue(REALTIME_STREAM_ENDED_PARAMNAME, ref m_realtimeStreamEnded);

            if(m_readerGraphNode.TryGetParameterValue(CACHE_SUCCESSFUL_PARAMNAME, ref m_cacheFileSuccessful) && m_cacheFileSuccessful)
			{
                m_events.onCacheFileSuccessful.Invoke();
            }

            if(m_readerGraphNode.TryGetParameterValue(BUFFERING_STATE_PARAMNAME, ref m_bufferState))
			{
                m_events.onBufferingStateChanged.Invoke(bufferState);
            }

            if(m_readerGraphNode.TryGetParameterValue(BUFFER_PROGRESS_PARAMNAME, ref m_bufferProgress))
			{
                m_events.onBufferingProgressChanged.Invoke(bufferProgress);
            }

            if(m_readerGraphNode.TryGetParameterValue(DOWNLOAD_SPEED_PARAMNAME, ref m_downloadSpeed))
			{
                m_events.onDownloadSpeedChanged.Invoke(downloadSpeed);
            }
        }
    }
}
