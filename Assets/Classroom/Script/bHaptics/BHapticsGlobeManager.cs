using System.Collections;
using UnityEngine;
#if WIN || UNITY_EDITOR
using Bhaptics.SDK2;
#endif
using TLab.XR.VRGrabber;

namespace TLab.VRClassroom
{
    public class BHapticsGlobeManager : MonoBehaviour
    {
        public class BHapticsGlobeJointInfo
        {
            private bool m_hit = false;
            private bool m_onHit = false;

            private OVRBone m_bone;
            private Vector3 m_position;
            private Vector3 m_prevPosition;

            private LayerMask m_layer;

            public float GetVelocity
            {
                get
                {
                    return (m_position - m_prevPosition).magnitude / Time.deltaTime;
                }
            }

            public int GetHit
            {
                get
                {
                    return (m_onHit == true) ? (int)(GetVelocity * 25) : 0;
                }
            }

            public Vector3 Position
            {
                get
                {
                    return m_position;
                }
            }

            public Vector3 PrevPosition
            {
                get
                {
                    return m_prevPosition;
                }
            }

            public BHapticsGlobeJointInfo(OVRBone m_bone, LayerMask targetLayer)
            {
                this.m_bone = m_bone;
                this.m_layer = targetLayer;
            }

            public void Update()
            {
                m_prevPosition = m_position;
                m_position = m_bone.Transform.position;

                if (m_hit == true)
                {
                    // m_hit‚ªfalse‚É‚È‚é‚Ü‚Åm_onHit‚ðtrue‚É‚Å‚«‚È‚¢
                    m_hit = Physics.CheckSphere(m_position, 0.005f, m_layer);
                    m_onHit = false;
                }
                else
                {
                    m_onHit = Physics.CheckSphere(m_position, 0.005f, m_layer);
                    m_hit = m_onHit;
                }
            }
        }

        [SerializeField] private TLabVRTrackingHand m_rightHand;
        [SerializeField] private TLabVRTrackingHand m_leftHand;
        [SerializeField] private LayerMask m_layer;

        private BHapticsGlobeJointInfo[] m_rightJointInfos;
        private BHapticsGlobeJointInfo[] m_leftJointInfos;

        private IEnumerator LateStart()
        {
            // right hand

            while (!m_rightHand.SkeltonInitialized)
            {
                yield return null;
            }

            m_rightJointInfos = new BHapticsGlobeJointInfo[6];

            m_rightJointInfos[0] = new BHapticsGlobeJointInfo(m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_ThumbTip), m_layer);
            m_rightJointInfos[1] = new BHapticsGlobeJointInfo(m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_IndexTip), m_layer);
            m_rightJointInfos[2] = new BHapticsGlobeJointInfo(m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_MiddleTip), m_layer);
            m_rightJointInfos[3] = new BHapticsGlobeJointInfo(m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_RingTip), m_layer);
            m_rightJointInfos[4] = new BHapticsGlobeJointInfo(m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_PinkyTip), m_layer);
            m_rightJointInfos[5] = new BHapticsGlobeJointInfo(m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_WristRoot), m_layer);

            // left hand

            while (!m_leftHand.SkeltonInitialized)
            {
                yield return null;
            }

            m_leftJointInfos = new BHapticsGlobeJointInfo[6];

            m_leftJointInfos[0] = new BHapticsGlobeJointInfo(m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_ThumbTip), m_layer);
            m_leftJointInfos[1] = new BHapticsGlobeJointInfo(m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_IndexTip), m_layer);
            m_leftJointInfos[2] = new BHapticsGlobeJointInfo(m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_MiddleTip), m_layer);
            m_leftJointInfos[3] = new BHapticsGlobeJointInfo(m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_RingTip), m_layer);
            m_leftJointInfos[4] = new BHapticsGlobeJointInfo(m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_PinkyTip), m_layer);
            m_leftJointInfos[5] = new BHapticsGlobeJointInfo(m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_WristRoot), m_layer);
        }

        void Start()
        {
            StartCoroutine("LateStart");
        }

        void Update()
        {
            if (m_rightJointInfos != null)
            {
                foreach (BHapticsGlobeJointInfo jointInfo in m_rightJointInfos)
                {
                    jointInfo.Update();
                }
            }

            if (m_leftJointInfos != null)
            {
                foreach (BHapticsGlobeJointInfo jointInfo in m_leftJointInfos)
                {
                    jointInfo.Update();
                }
            }

#if Win || UNITY_EDITOR
            if (m_rightJointInfos != null)
            {
                BhapticsLibrary.PlayMotors(
                    position: (int)Bhaptics.SDK2.PositionType.GloveR,
                    motors: new int[6] {
                        m_rightJointInfos[0].GetHit,
                        m_rightJointInfos[1].GetHit,
                        m_rightJointInfos[2].GetHit,
                        m_rightJointInfos[3].GetHit,
                        m_rightJointInfos[4].GetHit,
                        m_rightJointInfos[5].GetHit},
                    durationMillis: 2
                );
            }

            if (m_leftJointInfos != null)
            {
                BhapticsLibrary.PlayMotors(
                    position: (int)Bhaptics.SDK2.PositionType.GloveL,
                    motors: new int[6] {
                        m_leftJointInfos[0].GetHit,
                        m_leftJointInfos[1].GetHit,
                        m_leftJointInfos[2].GetHit,
                        m_leftJointInfos[3].GetHit,
                        m_leftJointInfos[4].GetHit,
                        m_leftJointInfos[5].GetHit},
                    durationMillis: 2
                );
            }
#endif
        }
    }
}
