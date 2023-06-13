using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabShelfManager : MonoBehaviour
{
    [Tooltip("棚に登録するオブジェクト")]
    [SerializeField] public TLabShelfObjInfo[] m_shelfObjInfos;

    [Tooltip("転送先アンカー")]
    [SerializeField] protected Transform[] m_anchors;

    protected int m_currentObjIndex = 0;

    protected virtual IEnumerator FadeIn(int objIndex, int anchorIndex)
    {
        // https://docs.unity3d.com/ja/2018.4/Manual/Coroutines.html

        // 配列の範囲外だったらスキップ
        if (objIndex >= m_shelfObjInfos.Length) yield break;

        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[objIndex];

        // 配列に値が存在しなかったらスキップ
        if (shelfObjInfo == null) yield break;

        GameObject instanced;
        shelfObjInfo.instanced.TryGetValue(anchorIndex, out instanced);

        if (instanced != null)
        {
            // 既にインスタンス化されている場合はそれを削除する
            shelfObjInfo.instanced.Remove(anchorIndex);
            Destroy(instanced);
            yield return null;
        }

        // オブジェクトをシーンにインスタンス化

        Transform anchor = m_anchors[anchorIndex].transform;
        instanced = Instantiate(shelfObjInfo.obj, anchor.position, anchor.rotation);
        instanced.name = instanced.name + "_" + anchorIndex.ToString();

        // 分割可能オブジェクトの場合，子オブジェクトも名前を変更
        TLabVRGrabbable grabbable = instanced.GetComponent<TLabVRGrabbable>();
        if(grabbable != null)
        {
            if (grabbable.EnableDivide == true)
            {
                Transform[] transforms = instanced.gameObject.GetComponentsInChildren<Transform>();
                foreach(Transform childTransform in transforms)
                {
                    if (childTransform == this.transform)
                        continue;

                    childTransform.gameObject.name = childTransform.gameObject.name + "_" + anchorIndex.ToString();
                }
            }
        }

        // インスタンス化したオブジェクトの参照を保持する
        shelfObjInfo.instanced[anchorIndex] = instanced;

        yield break;
    }

    protected virtual IEnumerator FadeOut(int objIndex, int anchorIndex)
    {
        // 配列の範囲外だったらスキップ
        if (objIndex >= m_shelfObjInfos.Length) yield break;

        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[objIndex];

        // 配列に値が存在しなかったらスキップ
        if (shelfObjInfo == null) yield break;

        GameObject instanced;
        shelfObjInfo.instanced.TryGetValue(anchorIndex, out instanced);

        // インスタンスが存在しなかったらスキップ
        if (instanced == null) yield break;

        // インスタンスの削除
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

[System.Serializable]
public enum TLabShelfAction
{
    takeOut,
    putAway
}
