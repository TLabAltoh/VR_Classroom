using System;
using System.Collections;
using UnityEngine;
using TLab.XR.Network;
using TLab.XR.Interact;

namespace TLab.VRClassroom
{
    public class TextController : MonoBehaviour
    {
        public class TextControllerTransform
        {
            public TextControllerTransform(Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
            {
                this.localPosotion = localPosition;
                this.localScale = localScale;
                this.localRotation = localRotation;
            }

            public Vector3 localPosotion;
            public Vector3 localScale;
            public Quaternion localRotation;
        }

        [SerializeField] private Transform m_target;

        [SerializeField] private float m_forward;
        [SerializeField] private float m_vertical;
        [SerializeField] private float m_horizontal;

        [SerializeField] private bool m_enableSync = false;
        [SerializeField] private bool m_autoUpdate = false;

        private Grabbable m_grabbable;

        private TextControllerTransform m_initialTransform;

        const float DURATION = 0.25f;

        public void SetTarget(Transform taregt) => m_target = taregt;

        private void LerpScale(Transform target, TextControllerTransform start, TextControllerTransform end, float lerpValue)
        {
            target.localScale = Vector3.Lerp(start.localScale, end.localScale, lerpValue);
        }

        private IEnumerator FadeInTask()
        {
            // 現在のTransform
            var currentTransform = new TextControllerTransform(
                this.transform.localPosition,
                this.transform.localScale,
                this.transform.localRotation);

            float current = 0.0f;
            while (current < DURATION)
            {
                current += Time.deltaTime;
                LerpScale(this.transform, currentTransform, m_initialTransform, current / DURATION);
                yield return null;
            }
        }

        private IEnumerator FadeOutTask()
        {
            // 現在のTransform
            var currentTransform = new TextControllerTransform(
                this.transform.localPosition,
                this.transform.localScale,
                this.transform.localRotation);

            // Scaleが(0, 0, 0)のTransform(ターゲット)
            var targetTransform = new TextControllerTransform(
                this.transform.localPosition,
                Vector3.zero,
                this.transform.localRotation);

            float current = 0.0f;
            while (current < DURATION)
            {
                current += Time.deltaTime;
                LerpScale(this.transform, currentTransform, targetTransform, current / DURATION);
                yield return null;
            }
        }

        public void FadeIn() => StartCoroutine(FadeInTask());

        public void FadeOut() => StartCoroutine(FadeOutTask());

        public void FadeOutImmidiately()
        {
            m_initialTransform = new TextControllerTransform(
                this.transform.localPosition,
                this.transform.localScale,
                this.transform.localRotation);

            var targetTransform = new TextControllerTransform(
                this.transform.localPosition,
                Vector3.zero,
                this.transform.localRotation);

            LerpScale(this.transform, m_initialTransform, targetTransform, 1.0f);
        }

        void Start()
        {
            string name = this.gameObject.name;
            string num = name[name.Length - 1].ToString();

            int anchorIndex = SyncClient.NOT_REGISTED;
            Int32.TryParse(num, out anchorIndex);

            if (m_enableSync)
            {
                if (anchorIndex != SyncClient.Instance.seatIndex)
                {
                    m_target = null;
                }
                else
                {
                    m_autoUpdate = true;
                }
            }
            else if (anchorIndex != SyncClient.Instance.seatIndex)
            {
                Destroy(this.gameObject);
            }

            m_grabbable = this.GetComponent<Grabbable>();

            this.transform.parent = null;
        }

        void Update()
        {
            if (m_target == null)
            {
                return;
            }

            Transform mainCamera = Camera.main.transform;
            Vector3 diff = mainCamera.position - m_target.position;
            Vector3 offset = diff.normalized * m_forward + Vector3.up * m_vertical + Vector3.Cross(diff.normalized, Vector3.up) * m_horizontal;

            this.transform.position = m_target.position + offset;
            this.transform.LookAt(mainCamera, Vector3.up);

            if (m_enableSync && m_autoUpdate)
            {
                m_grabbable.SyncRTCTransform();
            }
        }
    }
}
