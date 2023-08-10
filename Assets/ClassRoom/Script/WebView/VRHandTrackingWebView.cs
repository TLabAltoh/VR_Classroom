using UnityEngine;
using TLab.Android.WebView;

public class VRHandTrackingWebView : MonoBehaviour
{
    [Header("Target WebView")]
    [SerializeField] private TLabWebView m_tlabWebView;
    [Header("Raycast Setting")]
    [SerializeField] private OVRHand m_hand;
    [SerializeField] private LayerMask m_webViewLayer;
    [SerializeField] private float m_rayMaxLength = 10.0f;

    private RaycastHit m_raycastHit;
    private int m_lastXPos;
    private int m_lastYPos;
    private bool m_onTheWeb = false;

    private const int TOUCH_DOWN = 0;
    private const int TOUCH_UP = 1;
    private const int TOUCH_MOVE = 2;

    private bool m_prevPitching = false;

    private int GetTouchEvent()
    {
        bool current = m_hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        int phase = current ? TOUCH_MOVE : (int)TouchPhase.Stationary;

        if (m_prevPitching && !current) phase = TOUCH_UP;

        if (!m_prevPitching && current) phase = TOUCH_DOWN;

        m_prevPitching = current;

        return phase;
    }

    private void DoesNotHit()
    {
        if (m_onTheWeb)
            m_tlabWebView.TouchEvent(m_lastXPos, m_lastYPos, TOUCH_UP);
    }

    private void OnEnable()
    {
        m_tlabWebView.StartWebView();
    }

    void Update()
    {
        Ray ray = new Ray(m_hand.PointerPose.position, m_hand.PointerPose.forward);

        if (Physics.Raycast(ray, out m_raycastHit, m_rayMaxLength, m_webViewLayer))
        {
            if(m_raycastHit.collider.name != this.name)
            {
                DoesNotHit();
            }
            else
            {
                m_onTheWeb = true;

                m_lastXPos = (int)((1.0f - m_raycastHit.textureCoord.x) * m_tlabWebView.webWidth);
                m_lastYPos = (int)(m_raycastHit.textureCoord.y * m_tlabWebView.webHeight);

                int eventNum = GetTouchEvent();
                m_tlabWebView.TouchEvent(m_lastXPos, m_lastYPos, eventNum);
            }
        }
        else
        {
            DoesNotHit();
        }

        m_tlabWebView.UpdateFrame();
    }
}
