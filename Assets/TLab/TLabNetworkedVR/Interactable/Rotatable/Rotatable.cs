using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR.Interact
{
    public class Rotatable : Interactable
    {
        private Grabbable m_grabbable;

        protected bool isGrabbled => m_grabbable.grabbed || m_grabbable.grabbedIndex != -1;

        private bool isSyncFromOutside => m_grabbable.syncFromOutside;
    }
}
