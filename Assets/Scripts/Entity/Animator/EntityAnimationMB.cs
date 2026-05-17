using System.Collections.Generic;
using UnityEngine;

namespace BC.Animation
{
    public interface IAnimatorParameterController
    {
        bool HasParameter(string parameterName);
        void SetBool(string parameterName, bool value);
        void SetFloat(string parameterName, float value);
        void SetInteger(string parameterName, int value);
        void SetTrigger(string parameterName);
        void ResetTrigger(string parameterName);
    }

    public enum EntityAnimatorParameterWriteMode
    {
        SetBool = 0,
        SetFloat = 1,
        SetInteger = 2,
        SetTrigger = 3,
        ResetTrigger = 4,
    }

    [DisallowMultipleComponent]
    public sealed class EntityAnimationMB : MonoBehaviour, IAnimatorParameterController
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;

        private bool initialized;
        private readonly Dictionary<string, int> parameterHashCache = new();
        private readonly Dictionary<string, AnimatorControllerParameterType> parameterTypeCache = new();
        private readonly HashSet<string> missingParameterWarnings = new();
        private readonly HashSet<string> missingLayerWarnings = new();
        private readonly HashSet<string> parameterTypeWarnings = new();

        public Animator Animator => animator;
        public bool IsReady => initialized && animator != null;

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            Initialize();
        }
        private void Initialize()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogError($"{nameof(EntityAnimationMB)}: Animator is missing.", this);
                enabled = false;
                return;
            }

            initialized = true;
        }

        public bool HasParameter(string parameterName)
        {
            return TryGetParameterHash(parameterName, out _);
        }

        public void SetFloat(string parameterName, float value)
        {
            if (TryGetParameterHash(parameterName, AnimatorControllerParameterType.Float, out int parameterHash))
                animator.SetFloat(parameterHash, value);
        }

        public void SetFloat(string parameterName, float value, float dampTime, float deltaTime)
        {
            if (TryGetParameterHash(parameterName, AnimatorControllerParameterType.Float, out int parameterHash))
                animator.SetFloat(parameterHash, value, dampTime, deltaTime);
        }

        public void SetBool(string parameterName, bool value)
        {
            if (TryGetParameterHash(parameterName, AnimatorControllerParameterType.Bool, out int parameterHash))
                animator.SetBool(parameterHash, value);
        }

        public void SetInteger(string parameterName, int value)
        {
            if (TryGetParameterHash(parameterName, AnimatorControllerParameterType.Int, out int parameterHash))
                animator.SetInteger(parameterHash, value);
        }

        public void SetTrigger(string parameterName)
        {
            if (TryGetParameterHash(parameterName, AnimatorControllerParameterType.Trigger, out int parameterHash))
                animator.SetTrigger(parameterHash);
        }

        public void ResetTrigger(string parameterName)
        {
            if (TryGetParameterHash(parameterName, AnimatorControllerParameterType.Trigger, out int parameterHash))
                animator.ResetTrigger(parameterHash);
        }

        public void SetLayerWeight(string layerName, float weight)
        {
            TrySetLayerWeight(layerName, weight);
        }

        public bool TrySetLayerWeight(string layerName, float weight)
        {
            if (!TryGetLayerIndex(layerName, out int layerIndex))
                return false;

            animator.SetLayerWeight(layerIndex, Mathf.Clamp01(weight));
            return true;
        }

        public bool TryGetBool(string parameterName, out bool value)
        {
            if (TryGetParameterHash(parameterName, AnimatorControllerParameterType.Bool, out int parameterHash))
            {
                value = animator.GetBool(parameterHash);
                return true;
            }

            value = false;
            return false;
        }

        public bool TryApplyParameter(
            EntityAnimatorParameterWriteMode writeMode,
            string parameterName,
            bool boolValue,
            float floatValue,
            int intValue)
        {
            switch (writeMode)
            {
                case EntityAnimatorParameterWriteMode.SetBool:
                    return TryApplyBool(parameterName, boolValue);

                case EntityAnimatorParameterWriteMode.SetFloat:
                    return TryApplyFloat(parameterName, floatValue);

                case EntityAnimatorParameterWriteMode.SetInteger:
                    return TryApplyInteger(parameterName, intValue);

                case EntityAnimatorParameterWriteMode.SetTrigger:
                    return TryApplyTrigger(parameterName);

                case EntityAnimatorParameterWriteMode.ResetTrigger:
                    return TryResetTrigger(parameterName);
            }

            return false;
        }

        private bool TryApplyBool(string parameterName, bool value)
        {
            if (!TryGetParameterHash(parameterName, AnimatorControllerParameterType.Bool, out int parameterHash))
                return false;

            animator.SetBool(parameterHash, value);
            return true;
        }

        private bool TryApplyFloat(string parameterName, float value)
        {
            if (!TryGetParameterHash(parameterName, AnimatorControllerParameterType.Float, out int parameterHash))
                return false;

            animator.SetFloat(parameterHash, value);
            return true;
        }

        private bool TryApplyInteger(string parameterName, int value)
        {
            if (!TryGetParameterHash(parameterName, AnimatorControllerParameterType.Int, out int parameterHash))
                return false;

            animator.SetInteger(parameterHash, value);
            return true;
        }

        private bool TryApplyTrigger(string parameterName)
        {
            if (!TryGetParameterHash(parameterName, AnimatorControllerParameterType.Trigger, out int parameterHash))
                return false;

            animator.SetTrigger(parameterHash);
            return true;
        }

        private bool TryResetTrigger(string parameterName)
        {
            if (!TryGetParameterHash(parameterName, AnimatorControllerParameterType.Trigger, out int parameterHash))
                return false;

            animator.ResetTrigger(parameterHash);
            return true;
        }

        private bool TryGetParameterHash(
            string parameterName,
            AnimatorControllerParameterType expectedType,
            out int parameterHash)
        {
            if (!TryGetParameterHash(parameterName, out parameterHash))
                return false;

            if (parameterTypeCache.TryGetValue(parameterName, out AnimatorControllerParameterType actualType) &&
                actualType == expectedType)
            {
                return true;
            }

            string warningKey = $"{parameterName}:{expectedType}";
            if (parameterTypeWarnings.Add(warningKey))
            {
                Debug.LogWarning(
                    $"{nameof(EntityAnimationMB)}: Animator parameter '{parameterName}' is '{actualType}', not '{expectedType}'.",
                    this);
            }

            return false;
        }

        private bool TryGetParameterHash(string parameterName, out int parameterHash)
        {
            parameterHash = 0;

            if (!IsReady)
                Initialize();

            if (!IsReady)
                return false;

            if (string.IsNullOrWhiteSpace(parameterName))
                return false;

            if (parameterHashCache.TryGetValue(parameterName, out parameterHash))
                return true;

            parameterHash = Animator.StringToHash(parameterName);

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash != parameterHash)
                    continue;

                parameterHashCache.Add(parameterName, parameterHash);
                parameterTypeCache.Add(parameterName, parameters[i].type);
                return true;
            }

            if (missingParameterWarnings.Add(parameterName))
            {
                Debug.LogWarning(
                    $"{nameof(EntityAnimationMB)}: Animator parameter '{parameterName}' was not found.",
                    this);
            }

            return false;
        }

        private bool TryGetLayerIndex(string layerName, out int layerIndex)
        {
            layerIndex = -1;

            if (!IsReady)
                Initialize();

            if (!IsReady)
                return false;

            if (string.IsNullOrWhiteSpace(layerName))
                return false;

            layerIndex = animator.GetLayerIndex(layerName);
            if (layerIndex >= 0)
                return true;

            if (missingLayerWarnings.Add(layerName))
            {
                Debug.LogWarning(
                    $"{nameof(EntityAnimationMB)}: Animator layer '{layerName}' was not found.",
                    this);
            }

            return false;
        }
    }
}