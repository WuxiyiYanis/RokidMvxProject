using UnityEngine;
using UnityEngine.UI;

namespace MVXUnity.UI
{
    public class MvxSimpleDataStreamFollowFPSSetter : MonoBehaviour
    {
        [SerializeField] public MVXUnity.MvxSimpleDataStream dataStream;
        [SerializeField] public Toggle followFPSToggle;

        void Reset()
        {
            followFPSToggle = GetComponent<Toggle>();
        }

        void Start()
        {
            followFPSToggle.isOn = dataStream.playAtMaxFPS;
        }

        public void OnToggleValueChanged(bool newValue)
        {
            dataStream.playAtMaxFPS = newValue;
        }
    }
}
