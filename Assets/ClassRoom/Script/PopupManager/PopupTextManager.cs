using UnityEngine;

public class PopupTextManager : MonoBehaviour
{
    [SerializeField] private TextController m_controller;
    [SerializeField] private TextController m_controller1;

    private void OnDestroy()
    {
        if (m_controller == null) return;
        Destroy(m_controller.gameObject);
        Destroy(m_controller1.gameObject);
    }
}
