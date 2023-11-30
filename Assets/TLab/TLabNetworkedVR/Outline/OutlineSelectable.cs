using System;
using System.Collections.Generic;
using UnityEngine;
using TLab.XR.Network;

namespace TLab.XR.VFX
{
    public class OutlineSelectable : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.1f)] protected float m_outlineWidth = 0.025f;

        public virtual bool selected { set => m_selected = value; }

        public virtual Material outlineMat { get => m_material; set => m_material = value; }

        [SerializeField] protected Material m_material;

        protected bool m_selected = false;
        protected bool m_prevSelected = false;

        protected virtual void Start()
        {
            string name = this.gameObject.name;
            string num = name[name.Length - 1].ToString();

            int anchorIndex = SyncClient.NOT_REGISTED;
            Int32.TryParse(num, out anchorIndex);

            if (anchorIndex != SyncClient.Instance.seatIndex)
            {
                var copy = new Material(m_material);
                var meshRenderer = this.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    var materials = meshRenderer.sharedMaterials;
                    var materialList = new List<Material>();

                    foreach (var material in materials)
                    {
                        if (material != m_material)
                        {
                            materialList.Add(material);
                        }
                    }

                    materialList.Add(copy);

                    m_material = copy;
                    meshRenderer.sharedMaterials = materialList.ToArray();
                }
            }

            m_material.SetFloat("_OutlineWidth", 0.0f);
        }

        protected virtual void Update()
        {
            m_material.SetFloat("_OutlineWidth", m_selected == true ? m_outlineWidth : 0.0f);
            m_prevSelected = m_selected;
            m_selected = false;
        }
    }
}