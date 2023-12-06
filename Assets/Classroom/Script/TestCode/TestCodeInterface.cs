using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.VRClassroom
{
#if UNITY_EDITOR
    public class TestCodeInterface : MonoBehaviour
    {
        [SerializeField] private Collider m_collider;

        public void ArrayInstantiateTest()
        {
            var array = new Queue<GameObject>[5];

            for (int i = 0; i < array.Length; i++)
            {
                Debug.Log($"array {i} is null ? :" + (array[i] == null));
            }
        }

        public void MeshColliderClosestPoint()
        {
            var point = m_collider.ClosestPoint(Vector3.zero);

            Debug.Log("closest point: " + point);
        }
    }
#endif
}
