using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR.VRGrabber
{
    [System.Serializable]
    public class TLabAnimParameterInfo
    {
        public AnimatorControllerParameterType type;
        public string name;
    }

    public class TLabSyncAnim : MonoBehaviour
    {
        [SerializeField] private Animator m_animator;
        [SerializeField] private TLabAnimParameterInfo[] m_parameters;

        private const string m_thisName = "[tlabsyncanim] ";

        public Animator animator
        {
            get
            {
                return animator;
            }
        }

        public void SyncAnim()
        {
            foreach (TLabAnimParameterInfo parameter in m_parameters)
            {
                TLabSyncJson obj = new TLabSyncJson
                {
                    role = (int)WebRole.GUEST,
                    action = (int)WebAction.SYNCTRANSFORM,
                    animator = new WebAnimInfo
                    {
                        id = this.transform.name,
                        parameter = parameter.name
                    }
                };

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        obj.animator.type = (int)WebAnimValueType.typeInt;
                        obj.animator.intVal = m_animator.GetInteger(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Float:
                        obj.animator.type = (int)WebAnimValueType.typeFloat;
                        obj.animator.floatVal = m_animator.GetFloat(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        obj.animator.type = (int)WebAnimValueType.typeBool;
                        obj.animator.boolVal = m_animator.GetBool(parameter.name);
                        break;
                }

                string json = JsonUtility.ToJson(obj);
                TLabSyncClient.Instalce.SendWsMessage(json);
            }
        }

        public void SyncAnimFromOutside(WebAnimInfo webAnimator)
        {
            switch (webAnimator.type)
            {
                case (int)WebAnimValueType.typeFloat:
                    SetFloat(webAnimator.parameter, webAnimator.floatVal);
                    break;
                case (int)WebAnimValueType.typeInt:
                    SetInteger(webAnimator.parameter, webAnimator.intVal);
                    break;
                case (int)WebAnimValueType.typeBool:
                    SetBool(webAnimator.parameter, webAnimator.boolVal);
                    break;
                case (int)WebAnimValueType.typeTrigger:
                    SetTrigger(webAnimator.parameter);
                    break;
            }
        }

        public void ClearAnim()
        {
            TLabSyncJson obj = new TLabSyncJson
            {
                role = (int)WebRole.GUEST,
                action = (int)WebAction.CLEARANIM,
                animator = new WebAnimInfo { id = this.transform.name }
            };

            string json = JsonUtility.ToJson(obj);
            TLabSyncClient.Instalce.SendWsMessage(json);
        }

        public void ShutdownAnimator(bool deleteCache)
        {
            if (deleteCache == true) ClearAnim();
        }

        #region SetParameter
        public void SetInteger(string paramName, int value)
        {
            m_animator.SetInteger(paramName, value);
        }

        public void SetFloat(string paramName, float value)
        {
            m_animator.SetFloat(paramName, value);
        }

        public void SetBool(string paramName, bool value)
        {
            m_animator.SetBool(paramName, value);
        }

        public void SetTrigger(string paramName)
        {
            m_animator.SetTrigger(paramName);
        }
        #endregion SetParameter

        void Start()
        {
            List<TLabAnimParameterInfo> parameterInfoList = new List<TLabAnimParameterInfo>();
            int parameterLength = m_animator.parameters.Length;
            for (int i = 0; i < parameterLength; i++)
            {
                TLabAnimParameterInfo parameterInfo = new TLabAnimParameterInfo();
                AnimatorControllerParameter parameter = m_animator.GetParameter(i);
                parameterInfo.type = parameter.type;
                parameterInfo.name = parameter.name;

                parameterInfoList.Add(parameterInfo);

                Debug.Log(m_thisName + parameter.name);
            }
            m_parameters = parameterInfoList.ToArray();

            TLabSyncClient.Instalce.AddSyncAnimator(this.gameObject.name, this);
            SyncAnim();
        }
    }
}
