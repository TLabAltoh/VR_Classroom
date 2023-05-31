using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TLabShelfSyncManager : TLabShelfManager
{
    private AssetBundle m_assetBundle;

    protected override IEnumerator FadeIn(TLabShelfObjInfo shelfObjInfo, Transform target)
    {
        TLabSyncGrabbable grabbable = TLabSyncClient.Instalce.Grabbables[shelfObjInfo.obj.gameObject.name] as TLabSyncGrabbable;
        if (grabbable != null)
        {
            grabbable.ForceRelease();

            // System locks objects, not players.

            grabbable.GrabbLock(true);
            grabbable.GrabbLockSelf(TLabSyncClient.Instalce.SeatIndex);
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

                grabbable.ForceRelease();
                grabbable.GrabbLock(true);
            }

            yield return null;
        }

        if (grabbable != null)
        {
            grabbable.SyncTransform();
            // Since other players may participate during the update, always continue to release the parent relationship and lock the object.
            grabbable.GrabbLock(false);
            grabbable.GrabbLockSelf(-1);
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
            grabbable.GrabbLockSelf(TLabSyncClient.Instalce.SeatIndex);
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

                grabbable.ForceRelease();
                grabbable.GrabbLock(true);
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
            grabbable.GrabbLockSelf(-1);
        }

        shelfObjInfo.currentTask = null;

        yield break;
    }

    public override void PutAway(int index)
    {
        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[index];

        if (shelfObjInfo.currentTask != null)
            StopCoroutine(shelfObjInfo.currentTask);

        shelfObjInfo.currentTask = FadeOut(shelfObjInfo, shelfObjInfo.start.transform);
        StartCoroutine(shelfObjInfo.currentTask);
        shelfObjInfo.isShelf = true;
    }

    public override void TakeOut(int index, Transform target)
    {
        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[index];

        if (shelfObjInfo.currentTask != null)
            StopCoroutine(shelfObjInfo.currentTask);

        shelfObjInfo.currentTask = FadeIn(shelfObjInfo, target);
        StartCoroutine(shelfObjInfo.currentTask);
        shelfObjInfo.isShelf = false;
    }

    public override void TakeOut(int index)
    {
        TakeOut(index, m_anchors[0]);
    }

    /// <summary>
    /// Collectively perform tasks to share and retrieve objects to clients
    /// </summary>
    /// <param name="index"></param>
    public override void LoopTask(int index)
    {
        base.LoopTask(index);
    }

    /// <summary>
    /// Starts a task to download an object from the outside.
    /// Replace the object with the object of the specified shelf number.
    /// </summary>
    /// <param name="modURL"></param>
    /// <param name="shelfIndex"></param>
    /// <returns></returns>
    public IEnumerator DownloadAssetBundle(string modURL, int shelfIndex)
    {
        Debug.Log("Start Load Asset");

        if (m_assetBundle != null)
            m_assetBundle.Unload(false);

        var request = UnityWebRequestAssetBundle.GetAssetBundle(modURL);
        yield return request.SendWebRequest();

        // Handle error
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.DataProcessingError)
        {
            Debug.LogError(request.error);
            yield break;
        }

        var handler = request.downloadHandler as DownloadHandlerAssetBundle;
        m_assetBundle = handler.assetBundle;

        Debug.Log("Finish Load Asset");

        AssetBundleRequest assetLoadRequest = m_assetBundle.LoadAssetAsync<GameObject>("ROOM");
        yield return assetLoadRequest;

        GameObject prefab = assetLoadRequest.asset as GameObject;
        Instantiate(prefab);

        // 棚のオブジェクトのメッシュ・子オブジェクトをAsset Bundleでダウンロードしたコンポーネントに差し替える
    }

    protected override void Start()
    {
        base.Start();
    }
}
