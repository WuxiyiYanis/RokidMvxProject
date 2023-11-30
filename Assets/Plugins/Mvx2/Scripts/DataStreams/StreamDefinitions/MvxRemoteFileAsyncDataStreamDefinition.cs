using UnityEngine;

namespace MVXUnity
{
    // <summary> A data stream definition based on SourceRemoteMVX2FileMutateAsyncReaderBackend and MutateRemoteMVX2FileAsyncReader filters of MVX2FileStream plugin. </summary>
    [CreateAssetMenu(fileName = "RemoteFileAsyncDataStreamDefinition", menuName = "Mvx2/Data Stream Definitions/Remote File Async Data Stream Definition")]
    public class MvxRemoteFileAsyncDataStreamDefinition : MvxRemoteFileDataStreamDefinition
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

        public enum FrameCacheType { None, Memory }

        /// <summary> A type of frame cache to be used for caching already downloaded frames. </summary>
        [SerializeField, HideInInspector] private FrameCacheType m_frameCacheType = FrameCacheType.None;
        public FrameCacheType frameCacheType
        {
            get { return m_frameCacheType; }
            set
            {
                if (m_frameCacheType != value)
                {
                    m_frameCacheType = value;
                    onDefinitionChanged.Invoke();
                }
            }
        }

        #endregion

        public override MvxDataStreamSourceRuntime AppendSource(Mvx2API.ManualGraphBuilder graphBuilder)
        {
            Mvx2API.SingleFilterGraphNode remoteMVX2FileMutateAsyncBackend = new Mvx2API.SingleFilterGraphNode(MVCommon.Guid.FromHexString("196E7756-F4F6-4BA5-93C2-77BBC0CEA020"), true);
            base.SetBaseReaderParameters(remoteMVX2FileMutateAsyncBackend);
            remoteMVX2FileMutateAsyncBackend.SetFilterParameterValue("Buffer Size", bufferSize.ToString());
            remoteMVX2FileMutateAsyncBackend.SetFilterParameterValue("Mode",
                mode == Mode.Blocking ? "Blocking" :
                mode == Mode.NonblockingBounded ? "Nonblocking-Bounded" :
                "Nonblocking-Unlimited");
            remoteMVX2FileMutateAsyncBackend.SetFilterParameterValue("Frame Cache Type", frameCacheType == FrameCacheType.None ? "None" : "Memory");

            Mvx2API.SingleFilterGraphNode remoteMVX2FileAsyncReader = new Mvx2API.SingleFilterGraphNode(MVCommon.Guid.FromHexString("AFACB6A7-BD00-416E-B1AA-A10615C28CF7"), true);

            graphBuilder.AppendGraphNode(remoteMVX2FileMutateAsyncBackend);
            graphBuilder.AppendGraphNode(remoteMVX2FileAsyncReader);
            return new MvxRemoteFileDataStreamSourceRuntime(remoteMVX2FileMutateAsyncBackend, events);
        }
    }
}
