using UnityEngine;
using TLab.XR.Input;
using TLab.XR.Interact;

namespace TLab.XR
{
    public class TLabXRHand : MonoBehaviour
    {
        [SerializeField] private InputDataSource m_inputDataSource;
        [SerializeField] private Transform m_pointerPos;
        [SerializeField] private Transform m_grabbPoint;
        [SerializeField] private float m_maxDistance = 10.0f;

        private GameObject m_raycastResult = null;
        private RaycastHit m_raycastHit;

        private Grabbable m_grabbable;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public Transform pointerPos => m_pointerPos;

        public Transform grabbPoint => m_grabbPoint;

        void Start()
        {

        }

        void Update()
        {
            var pose = m_inputDataSource.pointerPose;

            var ray = new Ray(pose.position, Vector3.forward);

            Grabbable candidate = null;
            float minDist = float.MaxValue;

            Grabbable.registory.ForEach((g) =>
            {
                if (g.Raycast(ray, out m_raycastHit, m_maxDistance))
                {
                    var tmp = m_raycastHit.distance;
                    if (minDist > tmp)
                    {
                        candidate = g;
                    }
                }
            });

            Grabbable.registory.ForEach((g) =>
            {
                bool grip = m_inputDataSource.pressed;
                bool onPress = m_inputDataSource.onPress;
                bool onRelease = m_inputDataSource.onRelease;

                if (g.Raycast(ray, out m_raycastHit, m_maxDistance))
                {
                    if (m_grabbable != null)
                    {
                        if (!grip)
                        {
                            m_grabbable.UnSelected(this);
                            m_grabbable = null;
                        }
                    }
                    else
                    {
                        var target = m_raycastHit.collider.gameObject;
                        m_raycastResult = target;

                        //
                        // Outline
                        //

                        //var selectable = target.GetComponent<OutlineSelectable>();
                        //if (selectable != null)
                        //{
                        //    selectable.selected = true;
                        //}

                        //
                        // Grip
                        //

                        //bool grip = OVRInput.GetDown(m_gripButton, m_controller);
                        //if (grip)
                        //{
                        //    var grabbable = target.GetComponent<TLabVRGrabbable>();

                        //    if (grabbable == null)
                        //    {
                        //        return;
                        //    }

                        //    if (grabbable.AddParent(this.gameObject))
                        //    {
                        //        m_grabbable = grabbable;
                        //    }
                        //}
                    }
                }
                else
                {
                    if (m_grabbable)
                    {
                        if (!grip)
                        {
                            m_grabbable.UnSelected(this);
                            m_grabbable = null;
                        }
                    }

                    m_raycastResult = null;
                }
            });
        }
    }
}
