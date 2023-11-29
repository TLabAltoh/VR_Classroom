using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR
{
    public class TLabXRHand : MonoBehaviour
    {
        [SerializeField] private Transform m_pointerPos;
        [SerializeField] private Transform m_grabbPoint;

        public Transform pointerPos => m_pointerPos;

        public Transform grabbPoint => m_grabbPoint;

        void Start()
        {

        }

        void Update()
        {

        }
    }
}
