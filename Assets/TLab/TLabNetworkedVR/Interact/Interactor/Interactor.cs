using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR.Interact
{
    public class Interactor : MonoBehaviour
    {
        [Header("TLab XR Hand")]
        [SerializeField] protected TLabXRHand m_hand;

        [SerializeField] protected Transform m_pointer;

        protected static List<int> m_identifiers = new List<int>();

        protected static int GenerateIdentifier()
        {
            while (true)
            {
                int identifier = Random.Range(0, int.MaxValue);

                if (m_identifiers.Contains(identifier))
                {
                    continue;
                }

                m_identifiers.Add(identifier);

                return identifier;
            }
        }

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected Interactable m_interactable;

        protected int m_identifier;

        protected RaycastHit m_raycastHit;

        protected GameObject m_raycastResult = null;

        protected bool m_pressed = false;

        protected bool m_onPress = false;

        protected bool m_onRelease = false;

        protected float m_pressStrength = 0.0f;

        protected Vector3 m_angulerVelocity;

        public int identifier => m_identifier;

        public Transform pointer => m_pointer;

        // Raycast Result

        public RaycastHit raycastHit => m_raycastHit;

        public GameObject raycastResult => m_raycastResult;

        // Interactor input

        public float pressStrength => m_pressStrength;

        public bool pressed => m_pressed;

        public bool onPress => m_onPress;

        public bool onRelease => m_onRelease;

        public Vector3 angulerVelocity => m_angulerVelocity;

        protected virtual void UpdateRaycast()
        {

        }

        protected virtual void UpdateInput()
        {

        }

        protected virtual void Process()
        {

        }

        protected virtual void Awake()
        {
            m_identifier = GenerateIdentifier();
        }

        protected virtual void Start()
        {
            UpdateInput();
        }

        protected virtual void Update()
        {
            // TODO: �����ōs�����������ʉ��������D�ǂ̃T�u�N���X�ł������悤��
            // ���������s���Ă���D�����CSpherecast()�̑Ώۂ̌^���قȂ邽�߁C
            // �^�w��̃p�����[�^����̓A�N�Z�X�ł��Ȃ� ...

            UpdateInput();

            Process();
        }
    }
}
