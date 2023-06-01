using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabShelfManager : MonoBehaviour
{
    [Header("Shelf Obj Info")]
    [SerializeField] public TLabShelfObjInfo[] m_shelfObjInfos;

    [Header("Transport Anchor")]
    [SerializeField] protected Transform[] m_anchors;

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

    public virtual void PutAway(int objIndex)
    {
        StartCoroutine(FadeOut(objIndex, 0));
    }

    public virtual void TakeOut(int objIndex)
    {
        StartCoroutine(FadeIn(objIndex, 0));
    }

    public virtual void Share(int objIndex)
    {
        for (int i = 1; i < m_anchors.Length; i++)
            StartCoroutine(FadeIn(objIndex, i));
    }

    public virtual void Collect(int objIndex)
    {
        for (int i = 1; i < m_anchors.Length; i++)
            StartCoroutine(FadeOut(objIndex, i));
    }
}

[System.Serializable]
public class TLabShelfObjInfo
{
    public GameObject obj;
    public float speed;
    [System.NonSerialized] public Dictionary<int, GameObject> instanced = new Dictionary<int, GameObject>();
}

[System.Serializable]
public enum TLabShelfAction
{
    takeOut,
    putAway
}
