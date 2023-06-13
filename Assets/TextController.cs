using System;
using UnityEngine;

public class TextController : MonoBehaviour
{
    [SerializeField] private Transform m_chair;
    [SerializeField] private float m_forward;
    [SerializeField] private float m_vertical;
    [SerializeField] private float m_horizontal;

    void Start()
    {
        string name = this.gameObject.name;
        int anchorIndex = Int32.Parse(name[name.Length - 1].ToString());
        if (anchorIndex != TLabSyncClient.Instalce.SeatIndex)
            Destroy(this.gameObject);

        this.transform.parent = null;
    }

    void Update()
    {
        if (m_chair == null) return;

        Transform mainCamera = Camera.main.transform;
        Vector3 diff = mainCamera.position - m_chair.position;
        Vector3 offset = diff.normalized * m_forward + Vector3.up * m_vertical + Vector3.right * m_horizontal;

        this.transform.position = m_chair.position + offset;
        this.transform.LookAt(mainCamera, Vector3.up);
    }
}
