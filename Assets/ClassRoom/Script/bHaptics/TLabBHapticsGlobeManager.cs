using System.Collections.Generic;
using UnityEngine;
using Bhaptics.SDK2;

public class TLabBHapticsGlobeManager : MonoBehaviour
{
    [SerializeField] private TLabVRTrackingHand m_rightHand;
    [SerializeField] private TLabVRTrackingHand m_leftHand;
    [SerializeField] private LayerMask m_layer;

    private Dictionary<int, bool> m_prevHit = new Dictionary<int, bool>();

    private const int RIGHT_HAND    = 0;
    private const int LEFT_HAND     = 1;
    private const int HAND_END      = (int)OVRSkeleton.BoneId.Hand_End;
    private const int HAND_START    = (int)OVRSkeleton.BoneId.Hand_Start;

    private int GetTouchEvent(OVRSkeleton.BoneId id, int handID)
    {
        TLabVRTrackingHand hand;
        if (handID == RIGHT_HAND)   hand = m_rightHand;
        else                        hand = m_leftHand;

        OVRBone bone    = hand.GetFingerBone(id);
        int prevIndex   = HAND_END * handID + (int)id;

        if (bone == null)
        {
            m_prevHit[prevIndex] = false;
            return 0;
        }

        if (Physics.CheckSphere(bone.Transform.position, 0.025f, m_layer))
        {
            bool prevprevHit = m_prevHit[prevIndex];
            m_prevHit[prevIndex] = true;
            if (prevprevHit == false)   return 30;
            else                        return 0;
        }
        else
        {
            m_prevHit[prevIndex] = false;
            return 0;
        }
    }

    void Start()
    {
        // leftHand
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_RingTip]     = false;
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_ThumbTip]    = false;
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_IndexTip]    = false;
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_PinkyTip]    = false;
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_MiddleTip]   = false;
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_WristRoot]   = false;

        // rightHand
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_RingTip + HAND_END]      = false;
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_ThumbTip + HAND_END]     = false;
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_IndexTip + HAND_END]     = false;
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_PinkyTip + HAND_END]     = false;
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_MiddleTip + HAND_END]    = false;
        m_prevHit[(int)OVRSkeleton.BoneId.Hand_WristRoot + HAND_END]    = false;
    }

    void Update()
    {
        BhapticsLibrary.PlayMotors(
            position: (int)Bhaptics.SDK2.PositionType.GloveL,
            motors: new int[6] {
                    GetTouchEvent(OVRSkeleton.BoneId.Hand_WristRoot, LEFT_HAND),
                    GetTouchEvent(OVRSkeleton.BoneId.Hand_ThumbTip, LEFT_HAND),
                    GetTouchEvent(OVRSkeleton.BoneId.Hand_IndexTip, LEFT_HAND),
                    GetTouchEvent(OVRSkeleton.BoneId.Hand_MiddleTip, LEFT_HAND),
                    GetTouchEvent(OVRSkeleton.BoneId.Hand_RingTip, LEFT_HAND),
                    GetTouchEvent(OVRSkeleton.BoneId.Hand_PinkyTip, LEFT_HAND)},
            durationMillis: 50
        );
    }
}
