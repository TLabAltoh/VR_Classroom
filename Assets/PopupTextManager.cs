using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupTextManager : MonoBehaviour
{
    [SerializeField] private TextController m_controller;

    private void OnDestroy()
    {
        Destroy(m_controller.gameObject);
    }
}
