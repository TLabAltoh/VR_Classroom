using System.Collections;
using UnityEngine;
#if WIN || UNITY_EDITOR
using Bhaptics.SDK2;
#endif

public class TLabBHapticsGlobeManager : MonoBehaviour
{
    public class BHapticsGlobeJointInfo
    {
        private bool hit = false;
        private bool onHit = false;

        private OVRBone bone;
        private Vector3 position;
        private Vector3 prevPosition;

        private LayerMask layer;

        public float GetVelocity
        {
            get
            {
                return (position - prevPosition).magnitude / Time.deltaTime;
            }
        }

        public int GetHit
        {
            get
            {
                return (onHit == true) ? (int)(GetVelocity * 25) : 0;
            }
        }

        public Vector3 Position
        {
            get
            {
                return position;
            }
        }

        public Vector3 PrevPosition
        {
            get
            {
                return prevPosition;
            }
        }

        public BHapticsGlobeJointInfo(OVRBone bone, LayerMask targetLayer)
        {
            this.bone = bone;
            this.layer = targetLayer;
        }

        public void Update()
        {
            prevPosition = position;
            position = bone.Transform.position;

            if (hit == true)
            {
                // hit‚ªfalse‚É‚È‚é‚Ü‚ÅonHit‚ðtrue‚É‚Å‚«‚È‚¢
                hit = Physics.CheckSphere(position, 0.005f, layer);
                onHit = false;
            }
            else
            {
                onHit = Physics.CheckSphere(position, 0.005f, layer);
                hit = onHit;
            }
        }
    }

    [SerializeField] private TLabVRTrackingHand m_rightHand;
    [SerializeField] private TLabVRTrackingHand m_leftHand;
    [SerializeField] private LayerMask m_layer;

    private BHapticsGlobeJointInfo[] rightJointInfo;
    private BHapticsGlobeJointInfo[] leftJointInfo;

    private IEnumerator LateStart()
    {
        // right hand

        while (m_rightHand.SkeltonInitialized == false) yield return null;

        rightJointInfo = new BHapticsGlobeJointInfo[6];

        rightJointInfo[0] = new BHapticsGlobeJointInfo(
            m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_ThumbTip), m_layer);
        rightJointInfo[1] = new BHapticsGlobeJointInfo(
            m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_IndexTip), m_layer);
        rightJointInfo[2] = new BHapticsGlobeJointInfo(
            m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_MiddleTip), m_layer);
        rightJointInfo[3] = new BHapticsGlobeJointInfo(
            m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_RingTip), m_layer);
        rightJointInfo[4] = new BHapticsGlobeJointInfo(
            m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_PinkyTip), m_layer);
        rightJointInfo[5] = new BHapticsGlobeJointInfo(
            m_rightHand.GetFingerBone(OVRSkeleton.BoneId.Hand_WristRoot), m_layer);

        // left hand

        while (m_leftHand.SkeltonInitialized == false) yield return null;

        leftJointInfo = new BHapticsGlobeJointInfo[6];

        leftJointInfo[0] = new BHapticsGlobeJointInfo(
            m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_ThumbTip), m_layer);
        leftJointInfo[1] = new BHapticsGlobeJointInfo(
            m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_IndexTip), m_layer);
        leftJointInfo[2] = new BHapticsGlobeJointInfo(
            m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_MiddleTip), m_layer);
        leftJointInfo[3] = new BHapticsGlobeJointInfo(
            m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_RingTip), m_layer);
        leftJointInfo[4] = new BHapticsGlobeJointInfo(
            m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_PinkyTip), m_layer);
        leftJointInfo[5] = new BHapticsGlobeJointInfo(
            m_leftHand.GetFingerBone(OVRSkeleton.BoneId.Hand_WristRoot), m_layer);
    }

    void Start()
    {
        StartCoroutine("LateStart");
    }

    void Update()
    {
        if (rightJointInfo != null)
            foreach (BHapticsGlobeJointInfo jointInfo in rightJointInfo) jointInfo.Update();

        if (leftJointInfo != null)
            foreach (BHapticsGlobeJointInfo jointInfo in leftJointInfo) jointInfo.Update();

#if Win || UNITY_EDITOR
        if (rightJointInfo != null)
        {
            BhapticsLibrary.PlayMotors(
                position: (int)Bhaptics.SDK2.PositionType.GloveR,
                motors: new int[6] {
                                rightJointInfo[0].GetHit,
                                rightJointInfo[1].GetHit,
                                rightJointInfo[2].GetHit,
                                rightJointInfo[3].GetHit,
                                rightJointInfo[4].GetHit,
                                rightJointInfo[5].GetHit},
                durationMillis: 2
            );
        }

        if (leftJointInfo != null)
        {
            BhapticsLibrary.PlayMotors(
                position: (int)Bhaptics.SDK2.PositionType.GloveL,
                motors: new int[6] {
                                leftJointInfo[0].GetHit,
                                leftJointInfo[1].GetHit,
                                leftJointInfo[2].GetHit,
                                leftJointInfo[3].GetHit,
                                leftJointInfo[4].GetHit,
                                leftJointInfo[5].GetHit},
                durationMillis: 2
            );
        }
#endif
    }
}
