using UnityEngine;

namespace MVXUnity
{
    [CreateAssetMenu(fileName = "CustomDataDecompressor", menuName = "Mvx2/Data Decompressors/Custom Data Decompressor")]
    public class MvxCustomDataDecompressor : MvxDataDecompressor
    {
        #region data

        [Tooltip("E.g. 9405A7FA-0533-4CE6-8F87-8A5407410904")]
        [SerializeField] public string decompressorFilterGUID;
        [SerializeField] public MvxParam[] decompressorFilterParams = null;

        #endregion

        public override void AppendDecompressor(Mvx2API.ManualGraphBuilder graphBuilder)
        {
            Mvx2API.SingleFilterGraphNode singleFilterGraphNode = new Mvx2API.SingleFilterGraphNode(MVCommon.Guid.FromHexString(decompressorFilterGUID));
            foreach (var sourceFilterParam in decompressorFilterParams)
                singleFilterGraphNode.SetFilterParameterValue(sourceFilterParam.key, sourceFilterParam.val);
            graphBuilder = graphBuilder + singleFilterGraphNode;
        }
    }
}
