using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TLabShelfManager : MonoBehaviour
{
    [Tooltip("�I�ɓo�^����I�u�W�F�N�g")]
    [SerializeField] public TLabShelfObjInfo[] m_shelfObjInfos;

    [Tooltip("�]����A���J�[")]
    [SerializeField] protected Transform[] m_anchors;

    protected int m_currentObjIndex = 0;

    private const string thisName = "[tlabshelf] ";

    protected virtual IEnumerator FadeIn(int objIndex, int anchorIndex)
    {
        // https://docs.unity3d.com/ja/2018.4/Manual/Coroutines.html

        // �z��͈̔͊O��������X�L�b�v
        if (objIndex >= m_shelfObjInfos.Length) yield break;

        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[objIndex];

        // �z��ɒl�����݂��Ȃ�������X�L�b�v
        if (shelfObjInfo == null) yield break;

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

        // �����\�I�u�W�F�N�g�̏ꍇ�C�q�I�u�W�F�N�g�����O��ύX
        Transform[] transforms = instanced.gameObject.GetComponentsInChildren<Transform>();
        foreach (Transform childTransform in transforms)
        {
            if (childTransform == instanced.transform) continue;

            childTransform.gameObject.name = childTransform.gameObject.name + "_" + anchorIndex.ToString();
        }

        // �C���X�^���X�������I�u�W�F�N�g�̎Q�Ƃ�ێ�����
        shelfObjInfo.instanced[anchorIndex] = instanced;

        yield break;
    }

    protected virtual IEnumerator FadeOut(int objIndex, int anchorIndex)
    {
        // �z��͈̔͊O��������X�L�b�v
        if (objIndex >= m_shelfObjInfos.Length) yield break;

        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[objIndex];

        // �z��ɒl�����݂��Ȃ�������X�L�b�v
        if (shelfObjInfo == null) yield break;

        GameObject instanced;
        shelfObjInfo.instanced.TryGetValue(anchorIndex, out instanced);

        // �C���X�^���X�����݂��Ȃ�������X�L�b�v
        if (instanced == null) yield break;

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
            StartCoroutine(FadeIn(m_currentObjIndex, i));
    }

    public virtual void Collect()
    {
        for (int i = 1; i < m_anchors.Length; i++)
            StartCoroutine(FadeOut(m_currentObjIndex, i));
    }
}

[System.Serializable]
public class TLabShelfObjInfo
{
    public GameObject obj;
    [System.NonSerialized] public Dictionary<int, GameObject> instanced = new Dictionary<int, GameObject>();
}

#if UNITY_EDITOR
[CustomEditor(typeof(TLabShelfManager))]
[CanEditMultipleObjects]
public class TLabShelfManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        TLabShelfManager manager = target as TLabShelfManager;

        if (GUILayout.Button("Initialize Shelf Obj"))
        {
            //
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
