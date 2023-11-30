using UnityEngine;

namespace MVXUnity
{
    /// <summary>
    /// A wrapper for SYK decompressor.
    /// </summary>
    /// <remarks>
    /// Picks one of SYK data layers in the stream and decompresses it.
    /// </remarks>
    [CreateAssetMenu(fileName = "SYKDataDecompressor", menuName = "Mvx2/Data Decompressors/SYK Data Decompressor")]
    public class MvxSYKDataDecompressor : MvxDataDecompressor
    {
        #region data

        const string DROP_DECODED_LAYERS = "Drop decoded layers";
        const string PRINT_STATS = "Print stats";

        [Tooltip("Indicates whether original compressed data shall be dropped from the frame after decompression")]
        public bool dropCompressedInput = true;

        [Tooltip("Enables statistics")]
        public bool printStats = false;

        #endregion

        public override void AppendDecompressor(Mvx2API.ManualGraphBuilder graphBuilder)
        {
            Mvx2API.SingleFilterGraphNode sykDecompressor = new Mvx2API.SingleFilterGraphNode(MVCommon.Guid.FromHexString("DAAEA229-CF6E-4EE1-8564-8A852E10556E"), true);
            sykDecompressor.SetFilterParameterValue(DROP_DECODED_LAYERS, dropCompressedInput ? "true" : "false");
            sykDecompressor.SetFilterParameterValue(PRINT_STATS, printStats ? "true" : "false");

            graphBuilder = graphBuilder + sykDecompressor;
        }
    }
}
