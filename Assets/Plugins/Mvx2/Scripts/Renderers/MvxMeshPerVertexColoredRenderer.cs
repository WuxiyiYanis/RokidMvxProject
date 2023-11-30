using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Processors/Mesh per Vertex Colored Renderer")]
    public class MvxMeshPerVertexColoredRenderer : MvxMeshRenderer
    {
        #region process frame

        public static bool SupportsFrameRendering(Mvx2API.Frame frame)
        {
            return frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.VERTEX_POSITIONS_DATA_LAYER, false)
                && frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.VERTEX_INDICES_DATA_LAYER, false)
                && frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.VERTEX_COLORS_DATA_LAYER, false);
        }

        protected override bool CanProcessFrame(Mvx2API.Frame frame)
        {
            return SupportsFrameRendering(frame);
        }

        protected override bool IgnoreUVs()
        {
            return true;
        }

        #endregion

        #region MonoBehaviour

        public override void Reset()
        {
            base.Reset();
#if UNITY_EDITOR
            Material defaultMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Plugins/Mvx2/Materials/MeshPerVertexColored.mat");
            if (defaultMaterial != null)
                materialTemplates = new Material[] { defaultMaterial };
#endif
        }

        #endregion
    }
}