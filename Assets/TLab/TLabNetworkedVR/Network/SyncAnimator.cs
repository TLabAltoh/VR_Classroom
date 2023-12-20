using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TLab.XR.Network
{
    [System.Serializable]
    public class AnimParameter
    {
        public AnimatorControllerParameterType type;
        public string name;
        public int lastValueHash;
    }

    public class SyncAnimator : NetworkedObject
    {
        // TODO: HumanoidをAnimatorで編集できるのか確認する．
        // 編集できる場合は，SyncAnimatorを継承したWebRTCでパラメータを同期するコンポーネントを作成する．

        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static void Register(string id, SyncAnimator syncAnimator) => m_registry[id] = syncAnimator;

        public static new void UnRegister(string id) => m_registry.Remove(id);

        public static new void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var grabbable = entry.Value as SyncAnimator;
                gameobjects.Add(grabbable.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public static new SyncAnimator GetById(string id) => m_registry[id] as SyncAnimator;

        #endregion REGISTRY

        [SerializeField] private Animator m_animator;

        private Hashtable m_parameters = new Hashtable();

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public virtual void SyncAnim(AnimParameter parameter)
        {
            var obj = new TLabSyncJson
            {
                role = (int)WebRole.GUEST,
                action = (int)WebAction.SYNCANIM,
                animator = new WebAnimInfo
                {
                    id = m_id,
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

            m_syncFromOutside = false;
        }

        public virtual void SyncAnimFromOutside(WebAnimInfo webAnimator)
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

            m_syncFromOutside = true;
        }

        public virtual void ClearAnim()
        {
            var obj = new TLabSyncJson
            {
                role = (int)WebRole.GUEST,
                action = (int)WebAction.CLEARANIM,
                animator = new WebAnimInfo { id = m_id }
            };
            SyncClient.Instance.SendWsMessage(JsonUtility.ToJson(obj));
        }

        protected virtual void OnChangeParameter(string paramName, int hashCode)
        {
            var parameterInfo = m_parameters[paramName] as AnimParameter;

            if (parameterInfo == null)
            {
                Debug.LogError("Animation Parameter Not Found:" + paramName);
                return;
            }

            parameterInfo.lastValueHash = hashCode;
        }

        public virtual void SetInteger(string paramName, int value) => m_animator.SetInteger(paramName, value);

        public virtual void SetFloat(string paramName, float value) => m_animator.SetFloat(paramName, value);

        public virtual void SetBool(string paramName, bool value) => m_animator.SetBool(paramName, value);

        public virtual void SetTrigger(string paramName) => m_animator.SetTrigger(paramName);

        public override void Shutdown(bool deleteCache)
        {
            if (m_shutdown || !socketIsOpen) return;

            if (deleteCache) ClearAnim();

            UnRegister(m_id);

            base.Shutdown(deleteCache);
        }

        protected virtual void Reset()
        {
            if (m_animator == null)
                m_animator = GetComponent<Animator>();
        }

        protected override void Start()
        {
            base.Start();

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

                Debug.Log(THIS_NAME + parameter.name);

                m_parameters[parameterInfo.name] = parameterInfo;
            }

            Register(m_id, this);
        }

        protected override void Update()
        {
            base.Update();

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
                    SyncAnim(parameter);
            }
        }

        protected override void OnDestroy() => Shutdown(false);

        protected override void OnApplicationQuit() => Shutdown(false);
    }
}
