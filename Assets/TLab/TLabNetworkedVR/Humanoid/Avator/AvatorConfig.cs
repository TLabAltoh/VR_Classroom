using UnityEngine;

namespace TLab.XR.Humanoid
{
    [CreateAssetMenu(menuName = "TLab/AvatorConfig")]
    public class AvatorConfig : ScriptableObject
    {
        public enum BodyParts
        {
            HEAD,
            L_HAND,
            R_HAND
        };

        [System.Serializable]
        public class AvatorParts
        {
            public BodyParts parts;

            public GameObject prefab;
        }

        [SerializeField] private AvatorParts[] m_body;

        public AvatorParts[] body => m_body;
    }
}
