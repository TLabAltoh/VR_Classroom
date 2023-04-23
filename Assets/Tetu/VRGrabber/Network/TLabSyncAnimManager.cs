using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabSyncAnimManager : MonoBehaviour
{
    [SerializeField] private TLabAnimationInfo[] m_animInfos;

    public static TLabSyncAnimManager Instance;

    public void StartAnimIndex(int index)
    {
        m_animInfos[index].StartAnimation();
    }

    void Start()
    {
        Instance = this;
    }

    void Update()
    {
        
    }
}

[System.Serializable]
public class TLabAnimationInfo
{
    [SerializeField] private Animation animation;

    public void StartAnimation()
    {
        animation.Play();
    }
}
