using System.Collections;
using UnityEngine;

public class TLabShelfSyncManager : TLabShelfManager
{
    protected override IEnumerator FadeIn(TLabShelfObjInfo shelfObjInfo, Transform target)
    {
        TLabSyncGrabbable grabbable = TLabSyncClient.Instalce.Grabbables[shelfObjInfo.obj.gameObject.name] as TLabSyncGrabbable;
        if (grabbable != null)
        {
            grabbable.ForceRelease();

            // System locks objects, not players.

            grabbable.GrabbLock(true);
            grabbable.GrabbLockSelf(true);
        }

        GameObject shelfObj = shelfObjInfo.obj;

        shelfObj.transform.position = target.transform.position;

        float remain = 2.0f;

        while (remain > 0.0f)
        {
            remain -= Time.deltaTime;

            shelfObj.transform.rotation = Quaternion.AngleAxis(10.0f * Time.deltaTime, Vector3.up) * shelfObj.transform.rotation;

            if (grabbable != null)
            {
                grabbable.SyncTransform();
            }

            yield return null;
        }

        if (grabbable != null)
        {
            grabbable.SyncTransform();

            grabbable.GrabbLock(false);
            grabbable.GrabbLockSelf(false);
        }

        shelfObjInfo.currentTask = null;

        yield break;
    }

    protected override IEnumerator FadeOut(TLabShelfObjInfo shelfObjInfo, Transform target)
    {
        TLabSyncGrabbable grabbable = TLabSyncClient.Instalce.Grabbables[shelfObjInfo.obj.gameObject.name] as TLabSyncGrabbable;
        if (grabbable != null)
        {
            grabbable.ForceRelease();
            grabbable.GrabbLock(true);
            grabbable.GrabbLockSelf(true);
        }

        GameObject shelfObj = shelfObjInfo.obj;

        float remain = 2.0f;

        while (remain > 0.0f)
        {
            remain -= Time.deltaTime;

            shelfObj.transform.rotation = Quaternion.AngleAxis(10.0f * Time.deltaTime, Vector3.up) * shelfObj.transform.rotation;

            if(grabbable != null)
            {
                grabbable.SyncTransform();
            }

            yield return null;
        }

        shelfObj.transform.position = target.transform.position;
        shelfObj.transform.rotation = target.transform.rotation;
        shelfObj.transform.localScale = target.transform.localScale;

        if (grabbable != null)
        {
            grabbable.SyncTransform();

            grabbable.GrabbLock(false);
            grabbable.GrabbLockSelf(false);
        }

        shelfObjInfo.currentTask = null;

        yield break;
    }

    public override void PutAway(int index)
    {
        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[index];

        if (shelfObjInfo.currentTask != null)
        {
            StopCoroutine(shelfObjInfo.currentTask);
        }

        shelfObjInfo.currentTask = FadeOut(shelfObjInfo, shelfObjInfo.start.transform);
        StartCoroutine(shelfObjInfo.currentTask);
        shelfObjInfo.isShelf = true;
    }

    public override void TakeOut(int index, Transform target)
    {
        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[index];

        if (shelfObjInfo.currentTask != null)
        {
            StopCoroutine(shelfObjInfo.currentTask);
        }

        shelfObjInfo.currentTask = FadeIn(shelfObjInfo, target);
        StartCoroutine(shelfObjInfo.currentTask);
        shelfObjInfo.isShelf = false;
    }

    public override void TakeOut(int index)
    {
        TakeOut(index, m_anchors[0]);
    }

    public override void LoopTask(int index)
    {
        base.LoopTask(index);
    }

    protected override void Start()
    {
        base.Start();

        for (int i = 0; i < m_shelfObjInfos.Length; i++)
        {
            TLabSyncGrabbable grabbable = TLabSyncClient.Instalce.Grabbables[m_shelfObjInfos[i].obj.gameObject.name] as TLabSyncGrabbable;
            if (grabbable != null && grabbable.UseGravity == true)
            {
                Debug.LogError("tlabshelfsyncmanager: Objects with UseGravity enabled cannot be used");
                m_shelfObjInfos[i] = null;
                return;
            }
        }
    }
}
