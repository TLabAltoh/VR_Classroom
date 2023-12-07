using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR.Interact
{
    public class Interactor : MonoBehaviour
    {
        [Header("TLab XR Hand")]
        [SerializeField] protected TLabXRHand m_hand;

        [Header("Raycst Settings")]
        [SerializeField] protected float m_maxDistance = 0.05f;

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

        protected List<Interactable> m_selecteds = new List<Interactable>();

        protected List<Interactable> m_hovereds = new List<Interactable>();

        protected bool m_pressed = false;

        protected bool m_onPress = false;

        protected bool m_onRelease = false;

        protected float m_pressStrength = 0.0f;

        protected Transform m_pointer;

        protected RaycastHit m_raycastHit;

        protected GameObject m_raycastResult = null;

        protected Vector3 m_angulerVelocity;

        protected int m_identifier;

        public bool pressed => m_pressed;

        public bool onPress => m_onPress;

        public bool onRelease => m_onRelease;

        public float pressStrength => m_pressStrength;

        public Transform pointer => m_pointer;

        public RaycastHit raycastHit => m_raycastHit;

        public GameObject raycastResult => m_raycastResult;

        public Vector3 angulerVelocity => m_angulerVelocity;

        public int identifier => m_identifier;

        protected virtual void Awake()
        {
            m_identifier = GenerateIdentifier();
        }

        protected virtual void Start()
        {

        }

        protected virtual void Update()
        {
            // TODO: ここで行う処理を共通化したい．どのサブクラスでも同じような
            // 処理を実行している．ただ，Spherecast()の対象の型が異なるため，
            // 型指定のパラメータからはアクセスできない ...
        }
    }
}
