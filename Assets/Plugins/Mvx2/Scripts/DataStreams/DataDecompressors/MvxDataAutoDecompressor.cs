using UnityEngine;

namespace MVXUnity
{
    [CreateAssetMenu(fileName = "DataAutoDecompressor", menuName = "Mvx2/Data Decompressors/Data Auto Decompressor")]
    public class MvxDataAutoDecompressor : MvxDataDecompressor
    {
        #region data

        [Tooltip("Indicates whether original compressed data shall be dropped from the frame after decompression")]
        public bool dropCompressedInput = true;

        #endregion

        public override void AppendDecompressor(Mvx2API.ManualGraphBuilder graphBuilder)
        {
            graphBuilder = graphBuilder + new Mvx2API.AutoDecompressorGraphNode(dropCompressedInput);
        }
    }
}
