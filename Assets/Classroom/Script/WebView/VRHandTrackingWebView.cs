using UnityEngine;
using TLab.XR.Interact;
using TLab.Android.WebView;

namespace TLab.VRClassroom
{
    public class VRHandTrackingWebView : Pointable
    {
        [Header("Target WebView")]
        [SerializeField] private TLabWebView m_tlabWebView;
        [SerializeField] private RectTransform m_webViewRect;

        private int m_lastXPos;
        private int m_lastYPos;
        private bool m_onTheWeb = false;

        private const float ZERO = 0.0f;
        private const float ONE = 1.0f;

        private const int TOUCH_DOWN = 0;
        private const int TOUCH_UP = 1;
        private const int TOUCH_MOVE = 2;

        private const float m_rectZThreshold = 0.05f;

        void TouchRelease()
        {
            if (m_onTheWeb)
            {
                m_tlabWebView.TouchEvent(m_lastXPos, m_lastYPos, TOUCH_UP);
            }

            m_onTheWeb = false;
        }

        public override void Selected(Interactor interactor)
        {
            base.Selected(interactor);

            DispatchEvent(TOUCH_DOWN, interactor);
        }

        public override void WhileSelected(Interactor interactor)
        {
            base.WhileSelected(interactor);

            DispatchEvent(TOUCH_MOVE, interactor);
        }

        public override void UnSelected(Interactor interactor)
        {
            base.UnSelected(interactor);

            DispatchEvent(TOUCH_UP, interactor);
        }

        private void DispatchEvent(int eventNum, Interactor interactor)
        {
            Vector3 invertPositoin = m_webViewRect.transform.InverseTransformPoint(interactor.pointer.position);

            // https://docs.unity3d.com/jp/2018.4/ScriptReference/Transform.InverseTransformPoint.html
            invertPositoin.z *= m_webViewRect.transform.lossyScale.z;

            float uvX = invertPositoin.x / m_webViewRect.rect.width + m_webViewRect.pivot.x;
            float uvY = 1.0f - (invertPositoin.y / m_webViewRect.rect.height + m_webViewRect.pivot.y);

            var zCondition = Mathf.Abs(invertPositoin.z) < m_rectZThreshold;
            var rectCondition = uvX >= ZERO && uvX <= ONE && uvY >= ZERO && uvY <= ONE;

            if (zCondition && rectCondition)
            {
                m_onTheWeb = true;

                m_lastXPos = (int)(uvX * m_tlabWebView.WebWidth);
                m_lastYPos = (int)(uvY * m_tlabWebView.WebHeight);

                m_tlabWebView.TouchEvent(m_lastXPos, m_lastYPos, eventNum);
            }
            else
            {
                TouchRelease();
            }
        }
    }
}