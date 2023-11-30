using System;
using System.Globalization;
using UnityEngine;

namespace MVXUnity
{
    public static class MvxUtils
    {
        public static void EnsureCollectionMinimalCapacity<T>(ref T[] collection, UInt32 minimalCapacity)
        {
            if (collection == null || collection.Length < minimalCapacity)
                collection = new T[minimalCapacity];
        }

        private static float[] m_boundingBoxData = new float[6];
        
        unsafe public static Bounds GetFrameBoundingBox(Mvx2API.Frame frame)
        {
            fixed (float* boundingBoxDataPtr = m_boundingBoxData)
                Mvx2API.FrameMeshExtractor.GetMeshData(frame).CopyBoundingBoxRaw((IntPtr)boundingBoxDataPtr);

            Vector3 boundingBoxVector1 = new Vector3(m_boundingBoxData[0], m_boundingBoxData[1], m_boundingBoxData[2]);
            Vector3 boundingBoxVector2 = new Vector3(m_boundingBoxData[3], m_boundingBoxData[4], m_boundingBoxData[5]);
            Vector3 boundingBoxCenter = (boundingBoxVector1 + boundingBoxVector2) / 2.0f;
            Vector3 boundingBoxSize = boundingBoxVector2 - boundingBoxVector1;
            return new Bounds(boundingBoxCenter, boundingBoxSize);
        }

        public static bool TryGetParameterValue<T>(this Mvx2API.SingleFilterGraphNode m_readerGraphNode, string paramName, ref T currentValue)
        {
            MVCommon.String strValue;
            if (m_readerGraphNode.TryGetFilterParameterValue(paramName, out strValue))
            {
                Type type = typeof(T);
                T prevValue = currentValue;
                if (type == typeof(bool))
                {
                    currentValue = (T)(object)(strValue.NetString == "True");
                }
                else if (type == typeof(int) || type.IsEnum)
                {
                    currentValue = (T)(object)int.Parse(strValue.NetString);
                }
                else if (type == typeof(float))
                {
                    currentValue = (T)(object)float.Parse(strValue.NetString.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture);
                }
                else if (type == typeof(string))
                {
                    currentValue = (T)(object)strValue.NetString;
                }
                else throw new InvalidOperationException("Parameter of type " + typeof(T) + " is not supported");

                return !prevValue.Equals(currentValue);
            }

            return false;
        }
    }
}
