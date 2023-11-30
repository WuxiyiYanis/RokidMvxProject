using UnityEngine;

namespace MVXUnity
{
    [ExecuteInEditMode]
    public class MvxDepthmapPointCloudPart : MonoBehaviour
    {
        #region data

        [SerializeField, HideInInspector] private MeshRenderer m_meshRenderer = null;
        [SerializeField, HideInInspector] private MeshFilter m_meshFilter = null;
        [SerializeField, HideInInspector] private Mesh m_mesh = null;
        [SerializeField, HideInInspector] private Material m_materialInstance = null;

        [SerializeField, HideInInspector] private int m_depthTextureWidth = 1;
        public int depthTextureWidth
        {
            get { return m_depthTextureWidth; }
        }

        [SerializeField, HideInInspector] private int m_depthTextureHeight = 1;
        public int depthTextureHeight
        {
            get { return m_depthTextureHeight; }
        }

        [SerializeField, HideInInspector] private int m_pointsCountDivider = 1;
        public int pointsCountDivider
        {
            get { return m_pointsCountDivider; }
        }

        private void DestroyMaterialInstance()
        {
            m_meshRenderer.material = null;

            if (m_materialInstance == null)
                return;

            if (Application.isPlaying)
                Destroy(m_materialInstance);
            else
                DestroyImmediate(m_materialInstance);
            m_materialInstance = null;
        }

        private void CreateMaterialInstance(Material materialTemplate)
        {
            if (materialTemplate == null)
                return;

            m_materialInstance = Instantiate<Material>(materialTemplate);
            m_meshRenderer.material = m_materialInstance;

            Vector2 pixelWidth = new Vector2(1f / m_depthTextureWidth, 1f / m_depthTextureHeight);
            m_meshRenderer.material.SetFloat("_PixelWidth", pixelWidth.x);
        }

        #endregion

        #region update

        public void SetMaterialTemplate(Material materialTemplate)
        {
            if (isActiveAndEnabled)
            {
                DestroyMaterialInstance();
                CreateMaterialInstance(materialTemplate);
            }
        }

        public void UpdateMeshSize(int depthTextureWidth, int depthTextureHeight, int pointsCountDivider = 1)
        {
            m_depthTextureWidth = depthTextureWidth;
            m_depthTextureHeight = depthTextureHeight;
            m_pointsCountDivider = pointsCountDivider;

            if (m_meshRenderer.material != null)
            {
                Vector2 pixelWidth = new Vector2(1f / m_depthTextureWidth, 1f / m_depthTextureHeight);
                m_meshRenderer.material.SetFloat("_PixelWidth", pixelWidth.x);
            }

            UpdateMesh();
        }

        public void ClearMesh()
        {
            m_mesh.Clear();

            m_depthTextureWidth = 1;
            m_depthTextureHeight = 1;
            m_pointsCountDivider = 1;
        }

        public void UpdateMaterialProperties(Texture2D depthmapTexture, Texture2D colorTexture, int colorTextureType,
            MVCommon.CameraParams depthCameraParams, MVCommon.CameraParams colorCameraParams)
        {
            if (m_meshRenderer.material == null)
                return;

            Vector4 depthParams = new Vector4(0.5f, 0.5f, 1000f, 1000f);
            Vector4 colorParams = new Vector4(0.5f, 0.5f, 1000f, 1000f);
            Vector4 depthAndColorResolutions = new Vector4(256, 256, 256, 256);
            Matrix4x4 colorProjectionMatrix = Matrix4x4.identity;
            Vector4 colorCameraDistortion4 = Vector4.zero;
            float colorCameraDistortion5 = 0f;

            if (depthCameraParams != null)
            {
            	depthAndColorResolutions.x = depthCameraParams.width;
                depthAndColorResolutions.y = depthCameraParams.height;
                depthParams = new Vector4(depthCameraParams.C.x, depthCameraParams.C.y, depthCameraParams.F.x, depthCameraParams.F.y);
            }

            if (colorCameraParams != null)
            {
                depthAndColorResolutions.z = colorCameraParams.width;
                depthAndColorResolutions.w = colorCameraParams.height;
                colorParams = new Vector4(colorCameraParams.C.x, colorCameraParams.C.y, colorCameraParams.F.x, colorCameraParams.F.y);
                MVCommon.Matrix4x4f colorCameraRotation = colorCameraParams.rotation;
                MVCommon.Vector3f colorCameraTranslation = colorCameraParams.translation;
                colorProjectionMatrix = new Matrix4x4(
                    new Vector4(colorCameraRotation[0, 0], colorCameraRotation[0, 1], colorCameraRotation[0, 2], colorCameraTranslation[0]),
                    new Vector4(colorCameraRotation[1, 0], colorCameraRotation[1, 1], colorCameraRotation[1, 2], colorCameraTranslation[1]),
                    new Vector4(colorCameraRotation[2, 0], colorCameraRotation[2, 1], colorCameraRotation[2, 2], colorCameraTranslation[2]),
                    new Vector4(0, 0, 0, 1));
                colorCameraDistortion4.x = colorCameraParams.GetDistortionCoefficient(0);
                colorCameraDistortion4.y = colorCameraParams.GetDistortionCoefficient(1);
                colorCameraDistortion4.z = colorCameraParams.GetDistortionCoefficient(2);
                colorCameraDistortion4.w = colorCameraParams.GetDistortionCoefficient(3);
                colorCameraDistortion5 = colorCameraParams.GetDistortionCoefficient(4);
            }
            
            m_meshRenderer.material.SetTexture("_DepthTex", depthmapTexture);
            m_meshRenderer.material.SetTexture("_MainTex", colorTexture);
            m_meshRenderer.material.SetInt("_TextureType", colorTextureType);
            
            m_meshRenderer.material.SetVector("_DepthParams", depthParams);
            m_meshRenderer.material.SetVector("_ColorParams", colorParams);
            m_meshRenderer.material.SetVector("_Resolutions", depthAndColorResolutions);
            m_meshRenderer.material.SetMatrix("_ColorProjection", colorProjectionMatrix);

            m_meshRenderer.material.SetVector("_ColorDistortion4", colorCameraDistortion4);
            m_meshRenderer.material.SetFloat("_ColorDistortion5", colorCameraDistortion5);
        }

        private void UpdateMesh()
        {
            m_meshFilter.mesh = null;
            DestroyMesh();
            CreateMesh();
            m_meshFilter.mesh = m_mesh;

            // pointsCountDivider == 1 -> full mesh
            // otherwise pointsCountDivider must be divisible by 2 (for simplicity of mesh building)
            int pointsCountDivider = Mathf.Max(1, m_pointsCountDivider);
            if (pointsCountDivider > 1)
                pointsCountDivider -= (pointsCountDivider % 2);
            float pointsCountDividerFloat = (float)pointsCountDivider;

            int elementsCount = m_depthTextureWidth * m_depthTextureHeight / (pointsCountDivider * pointsCountDivider);

            // each element consists of single point/vertex
            Vector3[] vertices = new Vector3[elementsCount];
            Vector2[] depthTextureUVs = new Vector2[elementsCount];
            int[] indices = new int[elementsCount];

            Vector2 pixelWidth = new Vector2(1f / m_depthTextureWidth, 1f / m_depthTextureHeight);
            Vector2 pixelHalfWidth = pixelWidth * 0.5f;

            int vertexPositionIndex = 0;
            int vertexIndexIndex = 0;

            for (int row = 0; row < m_depthTextureHeight; row += pointsCountDivider)
            {
                Vector2 depthTextureUV = pixelHalfWidth + new Vector2(0, pixelWidth.y) * row;
                
                for (int column = 0; column < m_depthTextureWidth; column += pointsCountDivider)
                {
                    // 1 vertex
                    indices[vertexIndexIndex++] = vertexPositionIndex;

                    depthTextureUVs[vertexPositionIndex] = depthTextureUV;
                    vertices[vertexPositionIndex] = Vector3.zero;
                    vertexPositionIndex++;

                    depthTextureUV.x += pixelWidth.x * pointsCountDividerFloat;
                }
            }

            m_mesh.vertices = vertices;
            m_mesh.uv = depthTextureUVs;
            m_mesh.SetIndices(indices, MeshTopology.Points, 0);
            m_mesh.UploadMeshData(true);
            
            // set some big bounds, as the exact value is unknown
            m_mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        }

        private void CreateMesh()
        {
            if (!m_mesh)
            {
                m_mesh = new Mesh();
                m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m_mesh.MarkDynamic();
            }
        }

        private void DestroyMesh()
        {
            if (m_mesh)
            {
                if (Application.isPlaying)
                    Destroy(m_mesh);
                else
                    DestroyImmediate(m_mesh);
                m_mesh = null;
            }
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            if (m_meshFilter == null)
                m_meshFilter = gameObject.AddComponent<MeshFilter>();
            if (m_meshRenderer == null)
            {
                m_meshRenderer = gameObject.AddComponent<MeshRenderer>();
                m_meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                m_meshRenderer.receiveShadows = false;
            }

            CreateMesh();
            m_meshFilter.mesh = m_mesh;
        }

        private void OnDestroy()
        {
            DestroyMesh();

            if (Application.isPlaying)
            {
                Destroy(m_meshRenderer);
                Destroy(m_meshFilter);
            }
            m_meshRenderer = null;
            m_meshFilter = null;
        }

        #endregion
    }
}