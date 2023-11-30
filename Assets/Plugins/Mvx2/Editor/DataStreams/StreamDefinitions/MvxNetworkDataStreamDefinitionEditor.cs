using UnityEngine;
using UnityEditor;

namespace MVXUnity
{
    [CustomEditor(typeof(MvxNetworkDataStreamDefinition), editorForChildClasses: true), CanEditMultipleObjects]
    public class MvxNetworkDataStreamDefinitionEditor : Editor
    {
        private readonly GUIContent m_commandsSocketGuiContent = new GUIContent("Commands socket", "E.g. 'tcp://192.168.1.1:5555'");
        private readonly GUIContent m_dataSocketGuiContent = new GUIContent("Data socket", "E.g. 'tcp://192.168.1.1:5556'");
        private readonly GUIContent m_receiveBufferCapacityGuiContent = new GUIContent("Receive buffer capacity");
        private readonly GUIContent m_responseReceiveTimeoutGuiContent = new GUIContent("Response receive timeout");

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(target, "MvxNetworkDataStreamDefinition properties changed");

            DrawDefaultInspector();
            DrawCommandsSocketProperty();
            DrawDataSocketProperty();
            DrawReceiveBufferCapacityProperty();
            DrawResponseReceiveTimeoutProperty();
        }

        private void DrawCommandsSocketProperty()
        {
            this.DrawPropertyTextField<MvxNetworkDataStreamDefinition>(m_commandsSocketGuiContent, x => x.commandsSocket, (x, value) => x.commandsSocket = value);
        }

        private void DrawDataSocketProperty()
        {
            this.DrawPropertyTextField<MvxNetworkDataStreamDefinition>(m_dataSocketGuiContent, x => x.dataSocket, (x, value) => x.dataSocket = value);
        }

        private void DrawReceiveBufferCapacityProperty()
        {
            this.DrawPropertyIntField<MvxNetworkDataStreamDefinition>(m_receiveBufferCapacityGuiContent, x => (int)x.receiveBufferCapacity, (x, value) => x.receiveBufferCapacity = (uint)value);
        }

        private void DrawResponseReceiveTimeoutProperty()
        {
            this.DrawPropertyLongField<MvxNetworkDataStreamDefinition>(m_responseReceiveTimeoutGuiContent, x => x.responseReceiveTimeout, (x, value) => x.responseReceiveTimeout = value);
        }
    }
}