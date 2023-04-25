using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabUIUtil : MonoBehaviour
{
    [SerializeField] private TLabUIAnimState[] m_states;

    public void SetCursor(int index)
    {
        TLabUIAnimState target = m_states[index];

        target.cursorOn = !target.cursorOn;
        target.animator.SetBool("CursorOn", target.cursorOn);
    }
}

[System.Serializable]
public class TLabUIAnimState
{
    [System.NonSerialized] public bool cursorOn = false;
    public Animator animator;
}
