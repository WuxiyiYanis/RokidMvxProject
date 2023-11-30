using UnityEngine;

namespace MVXUnity
{
    // <summary> A data stream definition based on SourceRemoteMVX2FileAsyncRealtimeReader filter of MVX2FileStream plugin. </summary>
    [CreateAssetMenu(fileName = "RemoteFileAsyncRealtimeDataStreamDefinition", menuName = "Mvx2/Data Stream Definitions/Remote File Async Realtime Data Stream Definition")]
    public class MvxRemoteFileAsyncRealtimeDataStreamDefinition : MvxRemoteFileDataStreamDefinition
    {
        #region data

        /// <summary> A size of the frames-buffer populated from a downloading thread and depopulated from the pipeline thread. </summary>
        [SerializeField, HideInInspector] private System.UInt32 m_bufferSize = 1;
        public System.UInt32 bufferSize
        {
            get { return m_bufferSize; }
            set
            {
                if (m_bufferSize != value)
                {
                    m_bufferSize = value;
                    onDefinitionChanged.Invoke();
                }
            }
        }

        public enum Mode { Blocking, NonblockingBounded, NonblockingUnlimited }

        /// <summary> A frames-requesting mode manifested when downloading of frames is slower than playback in a pipeline thread. </summary>
        [SerializeField, HideInInspector] private Mode m_mode = Mode.Blocking;
        public Mode mode
        {
            get { return m_mode; }
            set
            {
                if (m_mode != value)
                {
                    m_mode = value;
                    onDefinitionChanged.Invoke();
                }
            }
        }
        #endregion

        public override MvxDataStreamSourceRuntime AppendSource(Mvx2API.ManualGraphBuilder graphBuilder)
        {
            Mvx2API.SingleFilterGraphNode remoteMVX2FileAsyncRealtimeReader = new Mvx2API.SingleFilterGraphNode(MVCommon.Guid.FromHexString("4295B4C6-E58E-47C4-9272-9A8F49EEBDD2"), true);
            base.SetBaseReaderParameters(remoteMVX2FileAsyncRealtimeReader);
            remoteMVX2FileAsyncRealtimeReader.SetFilterParameterValue("Buffer Size", bufferSize.ToString());
            remoteMVX2FileAsyncRealtimeReader.SetFilterParameterValue("Mode", 
                mode == Mode.Blocking ? "Blocking" :
                mode == Mode.NonblockingBounded ? "Nonblocking-Bounded" :
                "Nonblocking-Unlimited");

            graphBuilder.AppendGraphNode(remoteMVX2FileAsyncRealtimeReader);
            return new MvxRemoteFileDataStreamSourceRuntime(remoteMVX2FileAsyncRealtimeReader, events);
        }
    }
}
