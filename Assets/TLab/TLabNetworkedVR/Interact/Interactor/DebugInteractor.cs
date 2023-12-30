using UnityEngine;

namespace TLab.XR.Interact
{
    public class DebugInteractor : Interactor
    {
        [SerializeField] private ExclusiveController[] m_controllers;

        public void Grab()
        {
            foreach (var controller in m_controllers)
                controller.OnGrabbed(this);
        }

        public void Release()
        {
            foreach (var controller in m_controllers)
                controller.OnRelease(this);
        }
    }
}
