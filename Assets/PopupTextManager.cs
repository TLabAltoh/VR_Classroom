using UnityEngine;

public class PopupTextManager : MonoBehaviour
{
    [SerializeField] private TextController m_controller;

    private void OnDestroy()
    {
        if (m_controller == null) return;
        Destroy(m_controller.gameObject);
    }
}
