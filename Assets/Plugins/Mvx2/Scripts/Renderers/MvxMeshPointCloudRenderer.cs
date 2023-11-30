using System;
using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Processors/Mesh Pointcloud Renderer")]
    public class MvxMeshPointCloudRenderer : MvxMeshRenderer
    {
        #region process frame

        public static bool SupportsFrameRendering(Mvx2API.Frame frame)
        {
            return frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.VERTEX_POSITIONS_DATA_LAYER, false);
        }

        protected override bool CanProcessFrame(Mvx2API.Frame frame)
        {
            return SupportsFrameRendering(frame);
        }

        protected override bool IgnoreNormals()
        {
            return true;
        }

        protected override bool IgnoreUVs()
        {
            return true;
        }

        private Int32[] m_pointCloudIndices = null;

        protected override UInt32 GetFrameMeshIndicesCount(Mvx2API.MeshData meshData)
        {
            return meshData.GetNumVertices();
        }

        unsafe protected override void CopyMeshIndicesToCollection(Mvx2API.MeshData meshData, Int32[] meshPartIndices)
        {
            UInt32 indicesCount = meshData.GetNumVertices();
            EnsureIndicesCollectionMinimalCapacity(indicesCount);

            Buffer.BlockCopy(m_pointCloudIndices, 0, meshPartIndices, 0, (int)indicesCount * 4);
        }

        protected override Mvx2API.MeshIndicesMode GetFrameIndicesMode(Mvx2API.MeshData meshData)
        {
            return Mvx2API.MeshIndicesMode.MIM_PointList;
        }

        private void EnsureIndicesCollectionMinimalCapacity(UInt32 minimalCapacity)
        {
            if (m_pointCloudIndices == null)
            {
                AllocPointCloudIndices(minimalCapacity);
            }
            else if (m_pointCloudIndices.Length < minimalCapacity)
            {
                ReleasePointCloudIndices();
                AllocPointCloudIndices(minimalCapacity);
            }
        }

        private void AllocPointCloudIndices(UInt32 size)
        {
            ReleasePointCloudIndices();

            m_pointCloudIndices = new Int32[size];

            for (int index = 0; index < size; index++)
                m_pointCloudIndices[index] = index;
        }

        private void ReleasePointCloudIndices()
        {
            if (m_pointCloudIndices != null)
                m_pointCloudIndices = null;
        }

        #endregion

        #region MonoBehaviour

        public override void Reset()
        {
            base.Reset();
#if UNITY_EDITOR
            Material defaultMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Plugins/Mvx2/Materials/PointCloud.mat");
            if (defaultMaterial != null)
                materialTemplates = new Material[] { defaultMaterial };
#endif
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            ReleasePointCloudIndices();
        }

        #endregion
    }
}