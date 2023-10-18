using System.Collections;
using UnityEngine;

namespace TLab.XR.VRGrabber
{
    [System.Serializable]
    public class AnimParameter
    {
        public AnimatorControllerParameterType type;
        public string name;
        public int lastValueHash;
    }

    public class SyncAnimator : MonoBehaviour
    {
        [SerializeField] private Animator m_animator;
        private Hashtable m_parameters = new Hashtable();

        private const string m_thisName = "[tlabsyncanim] ";

        public Animator animator
        {
            get
            {
                return m_animator;
            }
        }

        public void SyncAnim(AnimParameter parameter)
        {
            var obj = new TLabSyncJson
            {
                role = (int)WebRole.GUEST,
                action = (int)WebAction.SYNCANIM,
                animator = new WebAnimInfo
                {
                    id = this.transform.name,
                    parameter = parameter.name
                }
            };

            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Int:
                    obj.animator.type = (int)WebAnimValueType.TYPEINT;
                    obj.animator.intVal = m_animator.GetInteger(parameter.name);
                    break;
                case AnimatorControllerParameterType.Float:
                    obj.animator.type = (int)WebAnimValueType.TYPEFLOAT;
                    obj.animator.floatVal = m_animator.GetFloat(parameter.name);
                    break;
                case AnimatorControllerParameterType.Bool:
                    obj.animator.type = (int)WebAnimValueType.TYPEBOOL;
                    obj.animator.boolVal = m_animator.GetBool(parameter.name);
                    break;
                default: // AnimatorControllerParameterType.Trigger:
                    obj.animator.type = (int)WebAnimValueType.TYPETRIGGER;
                    obj.animator.triggerVal = parameter.name;
                    break;
            }
            SyncClient.Instance.SendWsMessage(JsonUtility.ToJson(obj));
        }

        public void SyncAnimFromOutside(WebAnimInfo webAnimator)
        {
            switch (webAnimator.type)
            {
                case (int)WebAnimValueType.TYPEFLOAT:
                    SetFloat(webAnimator.parameter, webAnimator.floatVal);
                    OnChangeParameter(webAnimator.parameter, webAnimator.floatVal.GetHashCode());
                    break;
                case (int)WebAnimValueType.TYPEINT:
                    SetInteger(webAnimator.parameter, webAnimator.intVal);
                    OnChangeParameter(webAnimator.parameter, webAnimator.intVal.GetHashCode());
                    break;
                case (int)WebAnimValueType.TYPEBOOL:
                    SetBool(webAnimator.parameter, webAnimator.boolVal);
                    OnChangeParameter(webAnimator.parameter, webAnimator.boolVal.GetHashCode());
                    break;
                default: // (int)WebAnimValueType.TYPETRIGGER:
                    SetTrigger(webAnimator.parameter);
                    // always false.GetHashCode()
                    break;
            }
        }

        public void ClearAnim()
        {
            var obj = new TLabSyncJson
            {
                role = (int)WebRole.GUEST,
                action = (int)WebAction.CLEARANIM,
                animator = new WebAnimInfo { id = this.transform.name }
            };
            SyncClient.Instance.SendWsMessage(JsonUtility.ToJson(obj));
        }

        public void ShutdownAnimator(bool deleteCache)
        {
            if (deleteCache)
            {
                ClearAnim();
            }
        }

        private void OnChangeParameter(string paramName, int hashCode)
        {
            var parameterInfo = m_parameters[paramName] as AnimParameter;

            if (parameterInfo == null)
            {
                return;
            }

            parameterInfo.lastValueHash = hashCode;
        }

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

        void Reset()
        {
            if (m_animator == null)
            {
                m_animator = GetComponent<Animator>();
            }
        }

        void Start()
        {
            int parameterLength = m_animator.parameters.Length;
            for (int i = 0; i < parameterLength; i++)
            {
                var parameterInfo = new AnimParameter();
                var parameter = m_animator.GetParameter(i);
                parameterInfo.type = parameter.type;
                parameterInfo.name = parameter.name;

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        parameterInfo.lastValueHash = m_animator.GetInteger(parameterInfo.name).GetHashCode();
                        break;
                    case AnimatorControllerParameterType.Float:
                        parameterInfo.lastValueHash = m_animator.GetFloat(parameterInfo.name).GetHashCode();
                        break;
                    case AnimatorControllerParameterType.Bool:
                        parameterInfo.lastValueHash = m_animator.GetBool(parameterInfo.name).GetHashCode();
                        break;
                    default:    //  AnimatorControllerParameterType.Trigger
                        parameterInfo.lastValueHash = false.GetHashCode();
                        break;
                }

                Debug.Log(m_thisName + parameter.name);

                m_parameters[parameterInfo.name] = parameterInfo;
            }

            SyncClient.Instance.AddSyncAnimator(this.gameObject.name, this);
        }

        void Update()
        {
            foreach (AnimParameter parameter in m_parameters.Values)
            {
                int prevValueHash = parameter.lastValueHash;
                int currentValueHash;

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        currentValueHash = m_animator.GetInteger(parameter.name).GetHashCode();
                        parameter.lastValueHash = currentValueHash;
                        break;
                    case AnimatorControllerParameterType.Float:
                        currentValueHash = m_animator.GetFloat(parameter.name).GetHashCode();
                        parameter.lastValueHash = currentValueHash;
                        break;
                    case AnimatorControllerParameterType.Bool:
                        currentValueHash = m_animator.GetBool(parameter.name).GetHashCode();
                        parameter.lastValueHash = currentValueHash;
                        break;
                    default:    //  AnimatorControllerParameterType.Trigger
                        currentValueHash = m_animator.GetBool(parameter.name).GetHashCode();
                        parameter.lastValueHash = false.GetHashCode();
                        break;
                }

                if (prevValueHash != currentValueHash)
                {
                    Debug.Log("Sync animation parameter");
                    SyncAnim(parameter);
                }
            }
        }
    }
}
