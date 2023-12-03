using UnityEngine;

namespace TLab.XR
{
    public class AnchorVisualizer : MonoBehaviour
    {
        [Header("Gizmo Settings")]

        [SerializeField] private Color m_gizmoXColor = Color.red;

        [SerializeField] private Color m_gizmoYColor = Color.green;

        [SerializeField] private Color m_gizmoZColor = Color.blue;

        [SerializeField] private Vector3 m_gizmoSize = new Vector3(0.1f, 0.1f, 0.5f);

        private const float HALF = 0.5f;

        void OnDrawGizmos()
        {
            Gizmos.color = m_gizmoXColor;

            var cache = Gizmos.matrix;

            var offset = new Vector3(0f, 0f, HALF);
            offset.z *= m_gizmoSize.z;

            // https://hacchi-man.hatenablog.com/entry/2021/05/23/220000

            Gizmos.color = m_gizmoXColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation * Quaternion.Euler(0f, Mathf.PI * Mathf.Rad2Deg * HALF, 0f) * Quaternion.identity, transform.lossyScale);
            Gizmos.DrawCube(offset, m_gizmoSize);

            Gizmos.color = m_gizmoYColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation * Quaternion.Euler(-Mathf.PI * Mathf.Rad2Deg * HALF, 0f, 0f) * Quaternion.identity, transform.lossyScale);
            Gizmos.DrawCube(offset, m_gizmoSize);

            Gizmos.color = m_gizmoZColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation * Quaternion.Euler(0f, 0f, 0f) * Quaternion.identity, transform.lossyScale);
            Gizmos.DrawCube(offset, m_gizmoSize);

            Gizmos.matrix = cache;
        }
    }
}
