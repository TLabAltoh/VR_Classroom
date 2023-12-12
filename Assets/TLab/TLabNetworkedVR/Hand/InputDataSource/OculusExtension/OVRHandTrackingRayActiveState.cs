using UnityEngine;
using Oculus.Interaction;

namespace TLab.XR.Input
{
    public class OVRHandTrackingRayActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField] private OVRHandTrackingInput m_input;

        public bool Active
        {
            get
            {
                return !m_input.rayHide;
            }
        }
    }
}
