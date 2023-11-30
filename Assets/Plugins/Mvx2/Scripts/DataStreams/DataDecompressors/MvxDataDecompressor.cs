using UnityEngine;

namespace MVXUnity
{
    public abstract class MvxDataDecompressor : ScriptableObject
    {
        public abstract void AppendDecompressor(Mvx2API.ManualGraphBuilder graphBuilder);
    }
}
