﻿using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Stream Thumbnail")]
    [ExecuteInEditMode]
    public class MvxDataStreamThumbnail : MvxDataRandomAccessStream
    {
        #region data

        public override MvxDataStreamDefinition dataStreamDefinition
        {
            get { return base.dataStreamDefinition; }
            set
            {
                if (dataStreamDefinition == value)
                    return;

                base.dataStreamDefinition = value;

                if (!Application.isPlaying && isActiveAndEnabled && autoPlay)
                    RestartStream();
            }
        }

        public override uint frameId
        {
            get
            {
                return 0;
                //causes crashes when initialized with frame other than 0
                //return !isOpen ? 0u : (uint)Mathf.Clamp((int)m_frameId, 0, (int)framesCount);
            }
            set
            {
                m_frameId = 0;
                return;

                //if (frameId == value)
                //    return;

                //if (!isOpen)
                //{
                //    m_frameId = 0u;
                //    return;
                //}

                //m_frameId = value;
                //if (!Application.isPlaying && isActiveAndEnabled)
                //    ReadFrame();
            }
        }

        #endregion

        #region MonoBehaviour

        public override void Awake()
        {
            if (Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }
                
            base.Awake();
        }

        public override void Update()
        {
            base.Update();

            if (isOpen)
            {
                if (mvxRunner == null)
                    RestartStream();    // on assemblies reload, the reader may become lost even though the isOpen still remains true

                ReadFrame();
            }
        }

        #endregion
    }
}
