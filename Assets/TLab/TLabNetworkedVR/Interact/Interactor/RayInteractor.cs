namespace TLab.XR.Interact
{
    public class RayInteractor : Interactor
    {
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

            m_pointer = m_hand.pointer;

            m_pressed = m_hand.pressed;

            m_onPress = m_hand.onPress;

            m_onRelease = m_hand.onRelease;

            m_angulerVelocity = m_hand.angulerVelocity;

            if (m_pressed)
            {
                m_selecteds.ForEach((i) =>
                {
                    i.WhileSelected(this);
                });
            }
            else if (m_onRelease)
            {
                m_selecteds.ForEach((i) =>
                {
                    i.UnSelected(this);
                });

                m_selecteds.Clear();
            }

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
                else if (m_hovereds.Contains(h))
                {
                    h.UnHovered(this);
                    m_hovereds.Remove(h);
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

                if (m_onPress && !selectedContain)
                {
                    m_selecteds.Add(candidate);
                    candidate.Selected(this);
                }
            }
            else
            {
                m_raycastResult = null;
            }
        }
    }
}
