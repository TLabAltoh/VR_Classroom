using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabShelfManager : MonoBehaviour
{
    [Header("Shelf Obj Info")]
    [SerializeField] protected TLabShelfObjInfo[] m_shelfObjInfos;

    [Header("Player Anchor")]
    [SerializeField] protected Transform m_host;

    protected virtual IEnumerator FadeIn(TLabShelfObjInfo shelfObjInfo, Transform target)
    {
        // https://docs.unity3d.com/ja/2018.4/Manual/Coroutines.html

        GameObject shelfObj = shelfObjInfo.obj;

        shelfObj.transform.position = target.transform.position;

        float remain = 2.0f;

        while (remain > 0.0f)
        {
            remain -= Time.deltaTime;

            shelfObj.transform.rotation = Quaternion.AngleAxis(10.0f * Time.deltaTime, Vector3.up) * shelfObj.transform.rotation;

            yield return null;
        }

        shelfObjInfo.currentTask = null;

        yield break;
    }

    protected virtual IEnumerator FadeOut(TLabShelfObjInfo shelfObjInfo, Transform target)
    {
        GameObject shelfObj = shelfObjInfo.obj;

        float remain = 2.0f;

        while (remain > 0.0f)
        {
            remain -= Time.deltaTime;

            shelfObj.transform.rotation = Quaternion.AngleAxis(10.0f * Time.deltaTime, Vector3.up) * shelfObj.transform.rotation;

            yield return null;
        }

        shelfObj.transform.position = target.transform.position;
        shelfObj.transform.rotation = target.transform.rotation;
        shelfObj.transform.localScale = target.transform.localScale;

        shelfObjInfo.currentTask = null;

        yield break;
    }

    public virtual void TakeOut(int index, Transform target)
    {
        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[index];

        if (shelfObjInfo.currentTask != null)
        {
            StopCoroutine(shelfObjInfo.currentTask);
        }

        if (shelfObjInfo.isShelf == false)
        {
            shelfObjInfo.currentTask = FadeOut(shelfObjInfo, shelfObjInfo.start.transform);
            StartCoroutine(shelfObjInfo.currentTask);
            shelfObjInfo.isShelf = true;
        }
        else
        {
            shelfObjInfo.currentTask = FadeIn(shelfObjInfo, target);
            StartCoroutine(shelfObjInfo.currentTask);
            shelfObjInfo.isShelf = false;
        }
    }

    public virtual void TakeOut(int index)
    {
        TakeOut(index, m_host);
    }

    protected virtual void Start()
    {
        for(int i = 0; i < m_shelfObjInfos.Length; i++)
        {
            m_shelfObjInfos[i].start = new GameObject();
            m_shelfObjInfos[i].start.transform.position = m_shelfObjInfos[i].obj.transform.position;
            m_shelfObjInfos[i].start.transform.rotation = m_shelfObjInfos[i].obj.transform.rotation;
            m_shelfObjInfos[i].start.transform.localScale = m_shelfObjInfos[i].obj.transform.localScale;
            m_shelfObjInfos[i].isShelf = true;
        }
    }
}

[System.Serializable]
public class TLabShelfObjInfo
{
    [System.NonSerialized] public GameObject start;
    [System.NonSerialized] public bool isShelf = true;
    public GameObject obj;
    public float speed;
    public IEnumerator currentTask = null;
}
