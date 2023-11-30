using Mvx2API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace MVXUnity
{
	public class MvxByteArrayStreamDefinition : MvxDataStreamDefinition
	{
        private const string sourceFilterGUID = "9E6DF6ED-AFD8-419C-A323-2F6B13DD268A";

        private const string ARRAY_POINTER_PARAMNAME = "MVX Byte Array Pointer";
        private const string ARRAY_LENGTH_PARAMNAME = "MVX Byte Array Length";

        #region data

        [SerializeField, HideInInspector] private TextAsset m_mvxTextAsset;
        /// <summary>
        /// TextAsset for the mvx file
        /// </summary>
        public TextAsset mvxTextAsset
        {
            get { return m_mvxTextAsset; }
            set
            {
                if (m_mvxTextAsset == value)
                    return;
                m_mvxByteArray = null;
                m_mvxTextAsset = value;
                onDefinitionChanged.Invoke();
            }
        }

        [HideInInspector] private byte[] m_mvxByteArray;
        /// <summary>
        /// Byte array containing the mvx data
        /// </summary>
        public byte[] mvxByteArray
        {
            get { return m_mvxByteArray; }
            set
            {
                if (m_mvxByteArray == value)
                    return;
                m_mvxTextAsset = null;
                m_mvxByteArray = value;
                onDefinitionChanged.Invoke();
            }
        }

        #endregion

        private IntPtr m_unmanagedPointer;

        public override MvxDataStreamSourceRuntime AppendSource(ManualGraphBuilder graphBuilder)
        {
            SingleFilterGraphNode singleFilterGraphNode = new SingleFilterGraphNode(MVCommon.Guid.FromHexString(sourceFilterGUID));

            FreeUnmanagedPointer();

            if (m_mvxTextAsset == null && m_mvxByteArray == null)
            {
                Debug.LogWarning("No MVX data assigned");
                singleFilterGraphNode.SetFilterParameterValue(ARRAY_POINTER_PARAMNAME, "0");
                singleFilterGraphNode.SetFilterParameterValue(ARRAY_LENGTH_PARAMNAME, "0");
            }
            else
			{
                int length = 0;

                if (m_mvxTextAsset != null)
                {
#if UNITY_2021_2_OR_NEWER
                    NativeArray<byte> nativeArr = m_mvxTextAsset.GetData<byte>();
                    length = nativeArr.Length;
                    unsafe
                    {
                        m_unmanagedPointer = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(nativeArr);
                    }
#else
                    Debug.LogWarning("Creating copy of MVX byte array (upgrade to 2021.2+ to make use of NativeArray without allocation of new array)");
                    byte[] bytes = m_mvxTextAsset.bytes;
                    m_unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
                    Marshal.Copy(bytes, 0, m_unmanagedPointer, bytes.Length);
                    length = bytes.Length;
#endif
                }
                else if (m_mvxByteArray != null)
                {
                    Debug.LogWarning("Creating copy of MVX byte array");
                    m_unmanagedPointer = Marshal.AllocHGlobal(m_mvxByteArray.Length);
                    Marshal.Copy(m_mvxByteArray, 0, m_unmanagedPointer, m_mvxByteArray.Length);
                    length = m_mvxByteArray.Length;
                }
                else throw new InvalidOperationException("No MVX data assigned");

                Debug.Log($"MVX Text Asset: {m_unmanagedPointer.ToInt64()} {length}");

                singleFilterGraphNode.SetFilterParameterValue(ARRAY_POINTER_PARAMNAME, m_unmanagedPointer.ToInt64().ToString());
                singleFilterGraphNode.SetFilterParameterValue(ARRAY_LENGTH_PARAMNAME, length.ToString());
            }

            graphBuilder.AppendGraphNode(singleFilterGraphNode);
            return new MvxDataStreamSourceRuntime();
        }

		private void OnDestroy()
		{
            m_mvxTextAsset = null;
            m_mvxByteArray = null;

            onDefinitionChanged.Invoke();

            FreeUnmanagedPointer();
		}

		private void FreeUnmanagedPointer()
		{
#if UNITY_2021_2_OR_NEWER
            //TextAsset is the owner of the pointer
            //If the text asset is modified or destroyed, the array becomes invalid since it now points to invalid memory.
            m_unmanagedPointer = IntPtr.Zero;
#else
            if (m_unmanagedPointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_unmanagedPointer);
                m_unmanagedPointer = IntPtr.Zero;
            }
#endif
        }
    }
}
