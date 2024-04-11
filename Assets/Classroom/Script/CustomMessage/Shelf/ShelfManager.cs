using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TLab.Network;

namespace TLab.VRClassroom
{
    public class ShelfManager : MonoBehaviour
    {
        [Tooltip("�I�ɓo�^����I�u�W�F�N�g")]
        [SerializeField] public ShelfObjInfo[] m_shelfObjInfos;

        [Tooltip("�]����A���J�[")]
        [SerializeField] protected Transform[] m_anchors;

        protected int m_currentObjIndex = 0;

        private const string THIS_NAME = "[tlabshelf] ";

        protected virtual IEnumerator FadeIn(int objIndex, int anchorIndex)
        {
            // https://docs.unity3d.com/ja/2018.4/Manual/Coroutines.html

            // �z��͈̔͊O��������X�L�b�v
            if (objIndex >= m_shelfObjInfos.Length)
            {
                yield break;
            }

            ShelfObjInfo shelfObjInfo = m_shelfObjInfos[objIndex];

            // �z��ɒl�����݂��Ȃ�������X�L�b�v
            if (shelfObjInfo == null)
            {
                yield break;
            }

            GameObject instanced;
            shelfObjInfo.instanced.TryGetValue(anchorIndex, out instanced);

            if (instanced != null)
            {
                // ���ɃC���X�^���X������Ă���ꍇ�͂�����폜����
                shelfObjInfo.instanced.Remove(anchorIndex);
                Destroy(instanced);
                yield return null;
            }

            // �I�u�W�F�N�g���V�[���ɃC���X�^���X��

            Transform anchor = m_anchors[anchorIndex].transform;
            instanced = Instantiate(shelfObjInfo.obj, anchor.position, anchor.rotation);
            instanced.name = instanced.name + "_" + anchorIndex.ToString();

            // �q�K�w�̃I�u�W�F�N�g�̖��O�����ׂĕύX
            Transform[] transforms = instanced.gameObject.GetComponentsInChildren<Transform>();
            foreach (Transform childTransform in transforms)
            {
                if (childTransform == instanced.transform)
                {
                    continue;
                }

                childTransform.gameObject.name = childTransform.gameObject.name + "_" + anchorIndex.ToString();
            }

            // SeatIdentifier�ɂ��̃I�u�W�F�N�g���ǂ̐ȂɃC���X�^���X�����ꂽ���̂Ȃ̂����L�^����
            var identifier = instanced.GetComponent<SeatIdentifier>();
            identifier.seatIndex = anchorIndex;

            // �C���X�^���X�������I�u�W�F�N�g�̎Q�Ƃ�ێ�����
            shelfObjInfo.instanced[anchorIndex] = instanced;

            yield break;
        }

        protected virtual IEnumerator FadeOut(int objIndex, int anchorIndex)
        {
            // �z��͈̔͊O��������X�L�b�v
            if (objIndex >= m_shelfObjInfos.Length)
            {
                yield break;
            }

            ShelfObjInfo shelfObjInfo = m_shelfObjInfos[objIndex];

            // �z��ɒl�����݂��Ȃ�������X�L�b�v
            if (shelfObjInfo == null)
            {
                yield break;
            }

            GameObject instanced;
            shelfObjInfo.instanced.TryGetValue(anchorIndex, out instanced);

            // �C���X�^���X�����݂��Ȃ�������X�L�b�v
            if (instanced == null)
            {
                yield break;
            }

            // �C���X�^���X�̍폜
            shelfObjInfo.instanced.Remove(anchorIndex);
            Destroy(instanced);

            yield break;
        }

        public virtual void OnDropDownChanged(int objIndex)
        {
            m_currentObjIndex = objIndex;
        }

        public virtual void PutAway()
        {
            StartCoroutine(FadeOut(m_currentObjIndex, 0));
        }

        public virtual void TakeOut()
        {
            StartCoroutine(FadeIn(m_currentObjIndex, 0));
        }

        public virtual void Share()
        {
            for (int i = 1; i < m_anchors.Length; i++)
            {
                StartCoroutine(FadeIn(m_currentObjIndex, i));
            }
        }

        public virtual void Collect()
        {
            for (int i = 1; i < m_anchors.Length; i++)
            {
                StartCoroutine(FadeOut(m_currentObjIndex, i));
            }
        }
    }

    [System.Serializable]
    public class ShelfObjInfo
    {
        public GameObject obj;
        [System.NonSerialized] public Dictionary<int, GameObject> instanced = new Dictionary<int, GameObject>();
    }
}
