using UnityEngine;

namespace TLab.XR.VRGrabber.VFX
{
    public class OutlineSelector : MonoBehaviour
    {
        [SerializeField] private float m_raycastDistance = 10.0f;
        [SerializeField] private LayerMask m_outlineLayer;

        void Update()
        {
            Ray ray = new Ray(this.transform.position, this.transform.forward);
            RaycastHit raycastHit;
            if (Physics.Raycast(ray, out raycastHit, m_raycastDistance, m_outlineLayer))
            {
                GameObject target = raycastHit.collider.gameObject;

                var selectable = target.GetComponent<OutlineSelectable>();

                if (selectable == null)
                {
                    return;
                }

                selectable.Selected = true;
            }
        }
    }
}
