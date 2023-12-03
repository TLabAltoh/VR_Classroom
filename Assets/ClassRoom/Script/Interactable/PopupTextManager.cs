using System.Collections;
using UnityEngine;

namespace TLab.VRClassroom
{
    public class PopupTextManager : MonoBehaviour
    {
        [System.Serializable]
        public class PointerPopupPair
        {
            public TextController controller;
            public GameObject target;
        }

        public PointerPopupPair[] pointerPairs => m_pointerPairs;

        [SerializeField] protected TextController[] m_controllers;

        [SerializeField] protected PointerPopupPair[] m_pointerPairs;

        public TextController GetTextController(int index)
        {
            if (index < m_pointerPairs.Length)
            {
                return m_pointerPairs[index].controller;
            }
            else
            {
                return null;
            }
        }

        protected IEnumerator LateStart()
        {
            yield return null;

            foreach (var popupPair in m_pointerPairs)
            {
                if (popupPair.controller == null)
                {
                    continue;
                }

                popupPair.controller.FadeOutImmidiately();
            }

            yield break;
        }

        protected void Start()
        {
            StartCoroutine(LateStart());
        }

        protected void OnDestroy()
        {
            if (m_controllers.Length > 0)
            {
                foreach (var controller in m_controllers)
                {
                    if (controller != null)
                    {
                        Destroy(controller.gameObject);
                    }
                }
            }

            if (m_pointerPairs.Length > 0)
            {
                foreach (var pointerPair in m_pointerPairs)
                {
                    if (pointerPair.controller != null)
                    {
                        Destroy(pointerPair.controller.gameObject);
                    }
                }
            }
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            foreach (var popupPair in m_pointerPairs)
            {
                if (popupPair.controller != null && popupPair.target != null)
                {
                    popupPair.controller.SetTarget(popupPair.target.transform);
                }
            }
        }
#endif
    }
}