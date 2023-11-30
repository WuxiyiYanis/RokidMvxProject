using UnityEngine;

namespace MVXUnity
{
    // <summary> A data stream definition based on SourceRemoteMVX2FileSyncReader filter of MVX2FileStream plugin. </summary>
    [CreateAssetMenu(fileName = "RemoteFileSyncDataStreamDefinition", menuName = "Mvx2/Data Stream Definitions/Remote File Sync Data Stream Definition")]
    public class MvxRemoteFileSyncDataStreamDefinition : MvxRemoteFileDataStreamDefinition
    {
        #region data

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
            Mvx2API.SingleFilterGraphNode remoteMVX2FileSyncReader = new Mvx2API.SingleFilterGraphNode(MVCommon.Guid.FromHexString("EE9E6123-43B5-4F9F-BE48-9BC4AFF947AF"), true);
            base.SetBaseReaderParameters(remoteMVX2FileSyncReader);
            remoteMVX2FileSyncReader.SetFilterParameterValue("Frame Cache Type", frameCacheType == FrameCacheType.None ? "None" : "Memory");

            graphBuilder.AppendGraphNode(remoteMVX2FileSyncReader);
            return new MvxRemoteFileDataStreamSourceRuntime(remoteMVX2FileSyncReader, events);
        }
    }
}
