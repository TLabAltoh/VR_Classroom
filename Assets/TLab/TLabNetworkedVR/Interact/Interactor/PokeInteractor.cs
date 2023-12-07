using UnityEngine;

namespace TLab.XR.Interact
{
    class PokeInteractor : Interactor
    {
        [Header("Poke Pointer")]
        [SerializeField] private Transform m_pokePointer;
        [SerializeField] private float m_selectThreshold = 0.01f;

        [Header("Target Gesture")]
        [SerializeField] private string m_gesture;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();

            m_pointer = m_pokePointer;

            // pressed, onPress, onReleaseはポインターとパネルの距離で決まるので，
            // ここでは定義できない ...

            m_pressed = false;

            m_onPress = false;

            m_onRelease = false;

            m_angulerVelocity = m_hand.angulerVelocity;

            if (m_hand.currentGesture == m_gesture)
            {
                m_hovereds.ForEach((i) =>
                {
                    i.WhileHovered(this);
                });

                var minDist = float.MaxValue;
                var candidate = null as Pointable;

                Pointable.registry.ForEach((h) =>
                {
                    if (h.Spherecast(m_pointer.position, out m_raycastHit, m_maxDistance))
                    {
                        var tmp = m_raycastHit.distance;
                        if (minDist > tmp)
                        {
                            candidate = h;
                            minDist = tmp;
                        }
                    }
                    else
                    {
                        if (m_hovereds.Contains(h))
                        {
                            h.UnHovered(this);
                            m_hovereds.Remove(h);
                        }

                        if (m_selecteds.Contains(h))
                        {
                            candidate.UnSelected(this);
                            m_selecteds.Clear();
                        }
                    }
                });

                if (candidate != null as Pointable)
                {
                    var target = candidate.srufaceCollider.gameObject;

                    m_raycastResult = target;

                    var selectedContain = m_selecteds.Contains(candidate);

                    var hoveredContain = m_hovereds.Contains(candidate);

                    // Hover

                    if (!hoveredContain)
                    {
                        m_hovereds.Add(candidate);
                        candidate.Hovered(this);
                    }

                    // Select

                    var distance = m_raycastHit.distance;
                    var pressed = distance < m_selectThreshold;

                    if (pressed)
                    {
                        if (selectedContain)
                        {
                            candidate.WhileSelected(this);
                        }
                        else
                        {
                            m_selecteds.Add(candidate);
                            candidate.Selected(this);
                        }
                    }
                }
                else
                {
                    m_raycastResult = null;
                }
            }
            else
            {
                m_selecteds.ForEach((i) =>
                {
                    i.UnSelected(this);
                });

                m_selecteds.Clear();

                m_hovereds.ForEach((i) =>
                {
                    i.UnHovered(this);
                });

                m_hovereds.Clear();
            }
        }
    }
}
