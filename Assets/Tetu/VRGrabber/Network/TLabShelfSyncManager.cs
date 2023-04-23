using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabShelfSyncManager : TLabShelfManager
{
    [SerializeField] protected Transform[] m_gests;

    private TLabSyncGrabbable GetTargetGrabbable(string targetName)
    {
        GameObject target = GameObject.Find(targetName);

        if (target != null)
        {
            return target.GetComponent<TLabSyncGrabbable>();
        }
        else
        {
            return null;
        }
    }

    protected override IEnumerator FadeIn(TLabShelfObjInfo shelfObjInfo, Transform target)
    {
        TLabSyncGrabbable grabbable = GetTargetGrabbable(shelfObjInfo.obj.gameObject.name);
        if (grabbable != null)
        {
            grabbable.ForceRelease();

            // System locks objects, not players.

            grabbable.GrabbLock(true);
            grabbable.GrabbLockSelf(true);
        }

        base.FadeIn(shelfObjInfo, target);

        if (grabbable != null)
        {
            grabbable.GrabbLock(false);
            grabbable.GrabbLockSelf(false);
        }

        yield break;
    }

    protected override IEnumerator FadeOut(TLabShelfObjInfo shelfObjInfo, Transform targetEnd, Transform targetStart)
    {
        TLabSyncGrabbable grabbable = GetTargetGrabbable(shelfObjInfo.obj.gameObject.name);
        if (grabbable != null)
        {
            grabbable.ForceRelease();
            grabbable.GrabbLock(true);
            grabbable.GrabbLockSelf(true);
        }

        base.FadeOut(shelfObjInfo, targetEnd, targetStart);

        if (grabbable != null)
        {
            grabbable.GrabbLock(false);
            grabbable.GrabbLockSelf(false);
        }

        yield break;
    }

    public override void TakeOut(int index, Transform target)
    {
        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[index];

        if (shelfObjInfo.currentTask != null)
        {
            StopCoroutine(shelfObjInfo.currentTask);
        }

        if (shelfObjInfo.isShelf == false)
        {
            shelfObjInfo.currentTask = FadeOut(shelfObjInfo, shelfObjInfo.start.transform, target);
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

    public void Share()
    {

    }

    public override void TakeOut(int index)
    {
        TakeOut(index, m_host);
    }

    protected override void Start()
    {
        base.Start();
    }
}
