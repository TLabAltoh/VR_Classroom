using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.VRClassroom
{
#if UNITY_EDITOR
    public class TestCodeInterface : MonoBehaviour
    {
        public void ArrayInstantiateTest()
        {
            var array = new Queue<GameObject>[5];

            for (int i = 0; i < array.Length; i++)
            {
                Debug.Log($"array {i} is null ? :" + (array[i] == null));
            }
        }
    }
#endif
}
