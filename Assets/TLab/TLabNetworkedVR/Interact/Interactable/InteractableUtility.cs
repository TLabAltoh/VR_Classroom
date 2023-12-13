using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR
{
    /*
     * Fixed Count Queue
     * https://www.hanachiru-blog.com/entry/2020/05/05/120000
     * */

    public class FixedQueue<T> : IEnumerable<T>
    {
        private Queue<T> _queue;

        public int Count => _queue.Count;

        public int Capacity { get; private set; }

        public FixedQueue(int capacity)
        {
            Capacity = capacity;
            _queue = new Queue<T>(capacity);
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);

            if (Count > Capacity) Dequeue();
        }

        public T Dequeue() => _queue.Dequeue();

        public T Peek() => _queue.Peek();

        public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();
    }

    public class CashTransform
    {
        public Vector3 LocalPosiiton { get => m_localPosition; }

        public Vector3 LocalScale { get => m_localScale; }

        public Quaternion LocalRotation { get => m_localRotation; }

        public CashTransform(Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            m_localPosition = localPosition;
            m_localRotation = localRotation;
            m_localScale = localScale;
        }

        private Vector3 m_localPosition;
        private Vector3 m_localScale;
        private Quaternion m_localRotation;
    }
}
