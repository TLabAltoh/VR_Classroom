using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TLab.XR.VRGrabber
{
    [System.Serializable]
    public class SyncClientCustomCallback
    {
        public void OnMessage(string message)
        {
            if (onMessage != null)
            {
                onMessage.Invoke(message);
            }
        }

        public void OnGuestParticipated(int seatIndex)
        {
            if (onGuestParticipated != null)
            {
                onGuestParticipated.Invoke(seatIndex);
            }
        }

        public void OnGuestDisconnected(int seatIndex)
        {
            if (onGuestDisconnected != null)
            {
                onGuestDisconnected.Invoke(seatIndex);
            }
        }

        [SerializeField] private UnityEvent<string> onMessage;
        [SerializeField] private UnityEvent<int> onGuestParticipated;
        [SerializeField] private UnityEvent<int> onGuestDisconnected;
    }

    [System.Serializable]
    public class CustomCallback
    {
        public void OnMessage(string message)
        {
            if (onMessage != null)
            {
                onMessage.Invoke(message);
            }
        }

        public void OnGuestParticipated(int seatIndex)
        {
            if (onGuestParticipated != null)
            {
                onGuestParticipated.Invoke(seatIndex);
            }
        }

        public void OnGuestDisconnected(int seatIndex)
        {
            if (onGuestDisconnected != null)
            {
                onGuestDisconnected.Invoke(seatIndex);
            }
        }

        [SerializeField] private UnityEvent<string> onMessage;
        [SerializeField] private UnityEvent<int> onGuestParticipated;
        [SerializeField] private UnityEvent<int> onGuestDisconnected;
    }
}
