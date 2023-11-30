using UnityEngine;

namespace MVXUnity
{
    /// <summary>
    /// A wrapper for H264 decompressor.
    /// </summary>
    /// <remarks>
    /// Picks one of H264 data layers in the stream and decompresses it.
    /// </remarks>
    [CreateAssetMenu(fileName = "H264DataDecompressor", menuName = "Mvx2/Data Decompressors/H264 Data Decompressor")]
    public class MvxH264DataDecompressor : MvxDataDecompressor
    {
        #region data

        [Tooltip("Indicates whether original compressed data shall be dropped from the frame after decompression")]
        public bool dropCompressedInput = true;
        [Tooltip("A reasonable timeout for the decompressor to wait at most until it is successfully granted resources"
            + " by an underlying system in order to start the decoding process. (specified in microseconds")]
        public uint startDecodingTimeout = 3000u;
        [Tooltip("A size of frames buffer per stream. Determines how many frames can be decompressed at the same time"
            + " in case of asynchronous implementations. In case the buffer is full, the missing not-yet-decompressed"
            + " data of the oldest buffered frame are replaced with empty NV12 textures and released to the pipeline"
            + " to make a place for the new frame.")]
        public uint decompressedAtomsBufferSizePerStream = 20;

        #endregion

        public override void AppendDecompressor(Mvx2API.ManualGraphBuilder graphBuilder)
        {
            Mvx2API.SingleFilterGraphNode h264Decompressor = new Mvx2API.SingleFilterGraphNode(MVCommon.Guid.FromHexString("57A38625-A0DD-46C3-B030-51B044301E45"), true);
            h264Decompressor.SetFilterParameterValue("Drop compressed data", dropCompressedInput ? "true" : "false");
            h264Decompressor.SetFilterParameterValue("Decompressed atoms buffer size per stream", decompressedAtomsBufferSizePerStream.ToString());
            h264Decompressor.SetFilterParameterValue("Start decoding timeout", startDecodingTimeout.ToString());

            graphBuilder = graphBuilder + h264Decompressor;
        }
    }
}
