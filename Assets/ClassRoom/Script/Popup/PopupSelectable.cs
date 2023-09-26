using UnityEngine;
using TLab.XR.VRGrabber.VFX;

public class PopupSelectable : TLabOutlineSelectable
{
    [SerializeField] private PopupTextManager m_popupManager;
    [SerializeField] private int m_index;

    public PopupTextManager PopupManager { get => m_popupManager; set => m_popupManager = value; }

    public int Index
    {
        set
        {
            m_index = value;
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        if (m_selected && !m_prevSelected)
        {
            TextController instance = m_popupManager.GetTextController(m_index);
            if(instance != null) instance.FadeIn();
        }

        if (!m_selected && m_prevSelected)
        {
            TextController instance = m_popupManager.GetTextController(m_index);
            if (instance != null) instance.FadeOut();
        }

        base.Update();
    }
}
