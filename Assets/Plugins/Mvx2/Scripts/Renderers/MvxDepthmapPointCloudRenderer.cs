using System;
using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Processors/Depthmap Pointcloud Renderer")]
    public class MvxDepthmapPointCloudRenderer : MvxAsyncDataProcessor
    {
        #region data

        [SerializeField, HideInInspector] private Material m_materialTemplate = null;
        public Material materialTemplate
        {
            get { return m_materialTemplate; }
            set
            {
                if (m_materialTemplate == value)
                    return;

                m_materialTemplate = value;

                if (isActiveAndEnabled)
                {
                    if (m_depthPCLPart)
                        m_depthPCLPart.SetMaterialTemplate(m_materialTemplate);
                }
            }
        }

        [SerializeField, HideInInspector] private MvxDepthmapPointCloudPart m_depthPCLPart = null;
        [SerializeField, HideInInspector] private GameObject m_transformFixGO = null;
        [SerializeField] private int m_pointsCountDivider = 1;

        #endregion

        #region process frame

        public static bool SupportsFrameRendering(Mvx2API.Frame frame)
        {
            return frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.DEPTHMAP_TEXTURE_DATA_LAYER, false)
                && frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.CAMERA_PARAMS_DATA_LAYER, false)
                && FrameContainsTexture(frame);
        }

        private static bool FrameContainsTexture(Mvx2API.Frame frame)
        {
            return frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.RGB_TEXTURE_DATA_LAYER, false)
                || frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.NVX_TEXTURE_DATA_LAYER, false)
                || frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.DXT1_TEXTURE_DATA_LAYER, false)
                || frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.ETC2_TEXTURE_DATA_LAYER, false)
                || frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.ASTC_TEXTURE_DATA_LAYER, false);
        }

        protected override bool CanProcessFrame(Mvx2API.Frame frame)
        {
            return SupportsFrameRendering(frame);
        }

        protected override void ResetProcessedData()
        {
            // disable mesh
            if (m_depthPCLPart)
            {
                m_depthPCLPart.ClearMesh();
                m_depthPCLPart.gameObject.SetActive(false);
            }
        }

        protected override void ProcessNextFrame(MVCommon.SharedRef<Mvx2API.Frame> frame)
        {
            UpdateDepthMeshPart(frame.sharedObj);
        }

        unsafe private void UpdateDepthMeshPart(Mvx2API.Frame frame)
        {
            if (!m_depthPCLPart)
                m_depthPCLPart = CreateNewDepthPCLPart();

            MVCommon.CameraParams depthCameraParams;
            MVCommon.CameraParams colorCameraParams;
            ExtractCameraParams(frame, out depthCameraParams, out colorCameraParams);

            ExtractColorTextureData(frame);

            int depthmapTextureWidth;
            int depthmapTextureHeight;
            if (!ExtractDepthmapTextureData(frame, out depthmapTextureWidth, out depthmapTextureHeight))
            {
                m_depthPCLPart.gameObject.SetActive(false);
                return;
            }

            UpdateDepthMeshPartIfNecessary(depthmapTextureWidth, depthmapTextureHeight);

            m_depthPCLPart.UpdateMaterialProperties(m_depthmapTextures[m_activeDepthmapTextureIndex], m_colorTextures[m_activeColorTextureIndex], m_colorTextureTypes[m_activeColorTextureIndex],
                depthCameraParams, colorCameraParams);
            m_depthPCLPart.gameObject.SetActive(true);
        }

        private MvxDepthmapPointCloudPart CreateNewDepthPCLPart()
        {
            GameObject partGameObject = new GameObject("PCLPart");
            partGameObject.hideFlags = partGameObject.hideFlags | HideFlags.DontSave;
            partGameObject.transform.SetParent(m_transformFixGO.transform);
            partGameObject.transform.localPosition = Vector3.zero;
            partGameObject.transform.localRotation = Quaternion.identity;
            partGameObject.transform.localScale = Vector3.one;

            MvxDepthmapPointCloudPart newDepthPCLPart = partGameObject.AddComponent<MvxDepthmapPointCloudPart>();
            newDepthPCLPart.SetMaterialTemplate(m_materialTemplate);
            return newDepthPCLPart;
        }

        private void UpdateDepthMeshPartIfNecessary(int depthMapWidth, int depthMapHeight)
        {
            if (m_depthPCLPart.depthTextureWidth != depthMapWidth
                || m_depthPCLPart.depthTextureHeight != depthMapHeight
                || m_depthPCLPart.pointsCountDivider != m_pointsCountDivider)
            {
                m_depthPCLPart.UpdateMeshSize(depthMapWidth, depthMapHeight, m_pointsCountDivider);
            }
        }

        #endregion

        #region camera params extraction

        private void ExtractCameraParams(Mvx2API.Frame frame, out MVCommon.CameraParams depthCameraParams, out MVCommon.CameraParams colorCameraParams)
        {
            depthCameraParams = null;
            colorCameraParams = null;

            if (!frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.CAMERA_PARAMS_DATA_LAYER, false))
                return;

            depthCameraParams = new MVCommon.CameraParams();
            Mvx2API.FrameMiscDataExtractor.GetIRCameraParams(frame, depthCameraParams);

            colorCameraParams = new MVCommon.CameraParams();
            Mvx2API.FrameMiscDataExtractor.GetColorCameraParams(frame, colorCameraParams);
        }

        #endregion

        #region color textures extraction

        private enum TextureTypeCodes
        {
            TTC_ASTC = 4,
            TTC_DXT1 = 3,
            TTC_ETC2 = 2,
            TTC_NVX = 0,
            TTC_RGB = 1
        };

        // an array of textures - they are switched between updates to improve performance -> textures double-buffering
        private Texture2D[] m_colorTextures = new Texture2D[2];
        private int[] m_colorTextureTypes = new int[2];
        private int m_activeColorTextureIndex = -1;

        private void ExtractColorTextureData(Mvx2API.Frame frame)
        {
            int textureType;
            TextureFormat textureFormat;
            Mvx2API.FrameTextureExtractor.TextureType mvxTextureType;

            if (frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.ASTC_TEXTURE_DATA_LAYER, false))
            {
                textureType = (int)TextureTypeCodes.TTC_ASTC;
                textureFormat = TextureFormat.ASTC_8x8;
                mvxTextureType = Mvx2API.FrameTextureExtractor.TextureType.TT_ASTC;
            }
            else if (frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.DXT1_TEXTURE_DATA_LAYER, false))
            {
                textureType = (int)TextureTypeCodes.TTC_DXT1;
                textureFormat = TextureFormat.DXT1;
                mvxTextureType = Mvx2API.FrameTextureExtractor.TextureType.TT_DXT1;
            }
            else if (frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.ETC2_TEXTURE_DATA_LAYER, false))
            {
                textureType = (int)TextureTypeCodes.TTC_ETC2;
                textureFormat = TextureFormat.ETC2_RGB;
                mvxTextureType = Mvx2API.FrameTextureExtractor.TextureType.TT_ETC2;
            }
            else if (frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.NVX_TEXTURE_DATA_LAYER, false))
            {
                textureType = (int)TextureTypeCodes.TTC_NVX;
                textureFormat = TextureFormat.Alpha8;
                mvxTextureType = Mvx2API.FrameTextureExtractor.TextureType.TT_NVX;
            }
            else if (frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.RGB_TEXTURE_DATA_LAYER, false))
            {
                textureType = (int)TextureTypeCodes.TTC_RGB;
                textureFormat = TextureFormat.RGB24;
                mvxTextureType = Mvx2API.FrameTextureExtractor.TextureType.TT_RGB;
            }
            else
            {
                return;
            }

            ushort textureWidth, textureHeight;
            Mvx2API.FrameTextureExtractor.GetTextureResolution(frame, mvxTextureType, out textureWidth, out textureHeight);
            UInt32 textureSizeInBytes = Mvx2API.FrameTextureExtractor.GetTextureDataSizeInBytes(frame, mvxTextureType);
            IntPtr textureData = Mvx2API.FrameTextureExtractor.GetTextureData(frame, mvxTextureType);

            m_activeColorTextureIndex = (m_activeColorTextureIndex + 1) % m_colorTextures.Length;
            Texture2D newActiveTexture = m_colorTextures[m_activeColorTextureIndex];
            EnsureTextureProperties(ref newActiveTexture, textureFormat, textureWidth, textureHeight);
            m_colorTextures[m_activeColorTextureIndex] = newActiveTexture;
            m_colorTextureTypes[m_activeColorTextureIndex] = textureType;

            newActiveTexture.LoadRawTextureData(textureData, (Int32)textureSizeInBytes);
            newActiveTexture.Apply(true, false);
        }

        private void EnsureTextureProperties(ref Texture2D texture, TextureFormat targetFormat, ushort targetWidth, ushort targetHeight)
        {
            if (texture == null
                || texture.format != targetFormat
                || texture.width != targetWidth || texture.height != targetHeight)
                texture = new Texture2D(targetWidth, targetHeight, targetFormat, false);
        }

        #endregion

        #region depthmap texture extraction

        // an array of textures - they are switched between updates to improve performance -> textures double-buffering
        private Texture2D[] m_depthmapTextures = new Texture2D[2];
        private int m_activeDepthmapTextureIndex = -1;

        private bool ExtractDepthmapTextureData(Mvx2API.Frame frame, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (!frame.StreamContainsDataLayer(Mvx2API.BasicDataLayersGuids.DEPTHMAP_TEXTURE_DATA_LAYER, false))
                return false;

            const Mvx2API.FrameTextureExtractor.TextureType mvxTextureType = Mvx2API.FrameTextureExtractor.TextureType.TT_DEPTH;

            ushort textureWidth, textureHeight;
            Mvx2API.FrameTextureExtractor.GetTextureResolution(frame, mvxTextureType, out textureWidth, out textureHeight);
            UInt32 textureSizeInBytes = Mvx2API.FrameTextureExtractor.GetTextureDataSizeInBytes(frame, mvxTextureType);
            IntPtr textureData = Mvx2API.FrameTextureExtractor.GetTextureData(frame, mvxTextureType);

            if (textureSizeInBytes == 0)
                return false;

            m_activeDepthmapTextureIndex = (m_activeDepthmapTextureIndex + 1) % m_depthmapTextures.Length;
            Texture2D newActiveTexture = m_depthmapTextures[m_activeDepthmapTextureIndex];
            EnsureTextureProperties(ref newActiveTexture, TextureFormat.RG16, textureWidth, textureHeight);
            m_depthmapTextures[m_activeDepthmapTextureIndex] = newActiveTexture;

            newActiveTexture.filterMode = FilterMode.Point;
            newActiveTexture.LoadRawTextureData(textureData, (Int32)textureSizeInBytes);
            newActiveTexture.Apply(true, false);

            width = textureWidth;
            height = textureHeight;
            return true;
        }

        #endregion

        #region MonoBehaviour

        public override void Reset()
        {
            base.Reset();
#if UNITY_EDITOR
            Material defaultMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DepthmapPointCloud.mat");
            if (defaultMaterial != null)
                m_materialTemplate = defaultMaterial;
#endif
        }

        public virtual void Awake()
        {
            CreateTransformFixGO();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            DestroyDepthMeshPart();
            DestroyTransformFixGO();
        }

        private void CreateTransformFixGO()
        {
            if (m_transformFixGO != null)
                return;

            m_transformFixGO = new GameObject("TransformFix");
            m_transformFixGO.transform.parent = gameObject.transform;
            m_transformFixGO.transform.localPosition = Vector3.zero;
            m_transformFixGO.transform.localRotation = Quaternion.identity;
            m_transformFixGO.transform.localScale = new Vector3(1, 1, -1);
        }

        private void DestroyTransformFixGO()
        {
            if (m_transformFixGO == null)
                return;

            if (Application.isPlaying)
                Destroy(m_transformFixGO);
            else
                DestroyImmediate(m_transformFixGO);

            m_transformFixGO = null;
        }

        private void DestroyDepthMeshPart()
        {
            if (!m_depthPCLPart)
                return;

            m_depthPCLPart.ClearMesh();
            m_depthPCLPart.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(m_depthPCLPart.gameObject);
            else
                DestroyImmediate(m_depthPCLPart.gameObject);
            m_depthPCLPart = null;
        }

        #endregion
    }
}