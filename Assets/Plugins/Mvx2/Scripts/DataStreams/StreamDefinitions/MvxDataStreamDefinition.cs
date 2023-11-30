using UnityEngine;
using UnityEngine.Events;

namespace MVXUnity
{
    public abstract class MvxDataStreamDefinition : MonoBehaviour
    {
        #region events

        [System.Serializable] public class DefinitionChangedEvent : UnityEvent {}
        [SerializeField, HideInInspector] public DefinitionChangedEvent onDefinitionChanged = new DefinitionChangedEvent();

        #endregion

        public abstract MvxDataStreamSourceRuntime AppendSource(Mvx2API.ManualGraphBuilder graphBuilder);
    }
}
