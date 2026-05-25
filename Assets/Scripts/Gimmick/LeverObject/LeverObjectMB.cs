using System;
using System.Collections;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Base;
using BC.Player;
using BC.Rendering;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.LeverObject
{
    public enum LeverDirection
    {
        Left = 0,
        Middle = 1,
        Right = 2,
    }

    [Serializable]
    public struct LeverDirectionVisual
    {
        [LabelText("Local Euler")]
        [Tooltip("この方向が有効なときに適用されるローカル回転（Euler角）です。")]
        [SerializeField]
        private Vector3 localEulerAngles;

        [LabelText("Color On")]
        [Tooltip("この方向が選択中のときに適用するベースカラーです。")]
        [SerializeField]
        private Color color;

        [LabelText("Override Off Color")]
        [Tooltip("オフ時に専用カラーを使う場合に有効化します。")]
        [SerializeField]
        private bool overrideOffColor;

        [ShowIf(nameof(UsesAutoOffColor))]
        [LabelText("Off Saturation")]
        [Tooltip("オフ時の彩度倍率です。1 で同じ色、0 で無彩色になります。")]
        [SerializeField, Range(0f, 1f)]
        private float offSaturation;

        [ShowIf(nameof(UsesCustomOffColor))]
        [LabelText("Color Off")]
        [Tooltip("この方向が非選択時に適用するベースカラーです。")]
        [SerializeField]
        private Color offColor;

        [LabelText("Emit When Selected")]
        [Tooltip("この方向が選択中の間、Emission を有効にします。")]
        [SerializeField]
        private bool emitWhenSelected;

        [ShowIf(nameof(UsesEmissionSettings))]
        [LabelText("Emission Strength")]
        [Tooltip("Emission 有効時に使用する発光強度です。")]
        [SerializeField, Min(0f)]
        private float emissionStrength;

        [ShowIf(nameof(UsesEmissionSettings))]
        [LabelText("SimpleBoost Intensity")]
        [Tooltip("EnvironmentStylizedLit の SimpleBoost に加算する発光強度です。")]
        [SerializeField, Min(0f)]
        private float simpleBoostIntensity;

        private bool UsesAutoOffColor => !overrideOffColor;
        private bool UsesCustomOffColor => overrideOffColor;
        private bool UsesEmissionSettings => emitWhenSelected;

        public Vector3 LocalEulerAngles => localEulerAngles;
        public Color OnColor => color;
        public bool EmitWhenSelected => emitWhenSelected;
        public float EmissionStrength => Mathf.Max(0f, emissionStrength);
        public float SimpleBoostIntensity => Mathf.Max(0f, simpleBoostIntensity);

        public Color GetBaseColor(bool isSelected)
        {
            if (isSelected)
                return color;

            if (overrideOffColor)
                return offColor;

            float resolvedSaturation = offSaturation > 0f ? offSaturation : 0.35f;
            return CreateDesaturatedColor(color, resolvedSaturation);
        }

        private static Color CreateDesaturatedColor(Color source, float saturationMultiplier)
        {
            Color.RGBToHSV(source, out float hue, out float saturation, out float value);
            Color result = Color.HSVToRGB(hue, saturation * Mathf.Clamp01(saturationMultiplier), value);
            result.a = source.a;
            return result;
        }
    }

    [Serializable]
    public sealed class LeverVariableBinding
    {
        [Tooltip("この状態変数バインディングを有効にします。")]
        [SerializeField] private bool enabled = true;
        [ShowIf(nameof(IsEnabled))]
        [Tooltip("この変数を書き込む先のストアスコープです。")]
        [SerializeField] private ValueStoreWriteStoreScope scope = ValueStoreWriteStoreScope.Entity;
        [ShowIf(nameof(IsEnabled))]
        [Tooltip("対応するレバー状態が有効になったときに書き込みます。")]
        [SerializeField] private bool writeOnTrue = true;
        [ShowIf(nameof(IsEnabled))]
        [Tooltip("対応するレバー状態が無効になったときに書き込みます。")]
        [SerializeField] private bool writeOnFalse = true;

        [ShowIf(nameof(UsesEntityValueKey))]
        [Tooltip("この状態バインディングで書き込む Bool の ValueKey です。")]
        [SerializeField, ValueKeyDropdown]
        private ValueKeyReference entityBoolKey;

        [ShowIf(nameof(UsesEntityTarget))]
        [LabelText("$EntityTargetLabel")]
        [Tooltip("スコープが Entity のときに使用する対象 Entity です。")]
        [SerializeField]
        private EntityTargetReference entityTarget = EntityTargetReference.Self();

        private bool IsEnabled => enabled;
        private bool UsesEntityValueKey => enabled && scope != ValueStoreWriteStoreScope.Local;
        private bool UsesEntityTarget => enabled && ValueStoreWriteScopeUtility.UsesEntityTarget(scope);
        private string EntityTargetLabel => $"EntityTarget [{entityTarget.ToSummaryString()}]";

        public bool Enabled => enabled;
        public ValueStoreWriteStoreScope Scope => scope;
        public bool WriteOnTrue => writeOnTrue;
        public bool WriteOnFalse => writeOnFalse;
        public ValueKeyReference EntityBoolKey => entityBoolKey;
        public EntityTargetReference EntityTarget => entityTarget;

        public void NormalizeForLeverUsage()
        {
            if (scope == ValueStoreWriteStoreScope.Local)
                scope = ValueStoreWriteStoreScope.Entity;
        }
    }

    [DisallowMultipleComponent]
    public sealed class LeverObjectMB : MonoBehaviour, IInteractionTarget, IInteractionPromptProvider, IInteractionPromptDetailTextProvider
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int SimpleBoostEnabledId = Shader.PropertyToID("_SimpleBoostEmissionEnabled");
        private static readonly int SimpleBoostColorId = Shader.PropertyToID("_SimpleBoostEmissionColor");
        private static readonly int SimpleBoostIntensityId = Shader.PropertyToID("_SimpleBoostEmissionIntensity");

        [Header("Interaction")]
        [Tooltip("インタラクト位置に使うワールド Transform です。未指定時は自身の Transform を使います。")]
        [SerializeField] private Transform interactionTransform;
        [Tooltip("プロンプト配置のアンカー Transform です。未指定時は InteractionTransform を使います。")]
        [SerializeField] private Transform promptAnchor;
        [Tooltip("インタラクションのハイライト表示に使う任意の VisualTarget です。")]
        [SerializeField] private InteractionVisualTargetMB visualTarget;
        [Tooltip("インタラクト可能な平面距離の最大値です。")]
        [SerializeField, Min(0.05f)] private float maxInteractionDistance = 2.5f;
        [Tooltip("インタラクト可能な向き角度の最大値（度）です。")]
        [SerializeField, Range(0f, 180f)] private float maxInteractionAngle = 65f;
        [Tooltip("インタラクト完了までに必要なホールド時間（秒）です。")]
        [SerializeField, Min(0f)] private float requiredHoldDuration;
        [Tooltip("プロンプトアンカーに加算するワールド空間オフセットです。")]
        [SerializeField] private Vector3 promptWorldOffset = new(0f, 1.4f, 0f);
        [Tooltip("プロンプト詳細表示に使う任意テキストです。")]
        [SerializeField, TextArea] private string promptDetailText = string.Empty;

        [Header("State")]
        [Tooltip("Middle 方向を有効化し、Left/Middle/Right で循環するようにします。")]
        [SerializeField] private bool hasMiddleState;
        [Tooltip("コンポーネント初期化時に適用する初期状態です。")]
        [SerializeField] private LeverDirection initialState = LeverDirection.Left;

        [Header("Animation")]
        [Tooltip("レバー状態変更時に回転させる Transform です。")]
        [SerializeField] private Transform leverTransform;
        [Tooltip("レバー状態遷移アニメーションの時間（秒）です。")]
        [SerializeField, Min(0.01f)] private float duration = 0.2f;
        [Tooltip("レバー回転補間に使うイージングカーブです。")]
        [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Direction Visual")]
        [Tooltip("レバーが Left 状態のときに使う見た目設定です。")]
        [SerializeField] private LeverDirectionVisual left = new LeverDirectionVisual();
        [ShowIf(nameof(hasMiddleState))]
        [Tooltip("レバーが Middle 状態のときに使う見た目設定です。")]
        [SerializeField] private LeverDirectionVisual middle = new LeverDirectionVisual();
        [Tooltip("レバーが Right 状態のときに使う見た目設定です。")]
        [SerializeField] private LeverDirectionVisual right = new LeverDirectionVisual();

        [Header("Mesh")]
        [Tooltip("左ナビゲーション表示に使う Renderer です。")]
        [SerializeField] private Renderer leftNavigationMesh;
        [Tooltip("右ナビゲーション表示に使う Renderer です。")]
        [SerializeField] private Renderer rightNavigationMesh;
        [Tooltip("レバーハンドル表示に使う Renderer です。")]
        [SerializeField] private Renderer handleMesh;
        [Tooltip("Renderer に EnvironmentStylizedLit の SimpleBoost プロパティも書き込みます。")]
        [SerializeField] private bool syncEnvironmentSimpleBoost = true;

        [Header("State Variables")]
        [Tooltip("現在のレバー状態を ValueStore へ反映する書き込みを有効化します。")]
        [SerializeField] private bool writeStateVariables = true;
        [ShowIf(nameof(writeStateVariables))]
        [Tooltip("Left 状態の有効/無効に応じて適用するバインディングです。")]
        [SerializeField] private LeverVariableBinding leftVariable = new LeverVariableBinding();
        [ShowIf("@writeStateVariables && hasMiddleState")]
        [Tooltip("Middle 状態の有効/無効に応じて適用するバインディングです。")]
        [SerializeField] private LeverVariableBinding middleVariable = new LeverVariableBinding();
        [ShowIf(nameof(writeStateVariables))]
        [Tooltip("Right 状態の有効/無効に応じて適用するバインディングです。")]
        [SerializeField] private LeverVariableBinding rightVariable = new LeverVariableBinding();

        [Header("InlineAction")]
        [Tooltip("状態が Left に変わった後に実行する InlineAction です。")]
        [SerializeField] private InlineAction onLeftInlineAction;
        [ShowIf(nameof(hasMiddleState))]
        [Tooltip("状態が Middle に変わった後に実行する InlineAction です。")]
        [SerializeField] private InlineAction onMiddleInlineAction;
        [Tooltip("状態が Right に変わった後に実行する InlineAction です。")]
        [SerializeField] private InlineAction onRightInlineAction;

        [Header("WiringAction")]
        [Tooltip("状態が Left に変わった後に実行する WiringAction 群です。")]
        [SerializeField] private WiringAction[] onLeftWiringActions = Array.Empty<WiringAction>();
        [ShowIf(nameof(hasMiddleState))]
        [Tooltip("状態が Middle に変わった後に実行する WiringAction 群です。")]
        [SerializeField] private WiringAction[] onMiddleWiringActions = Array.Empty<WiringAction>();
        [Tooltip("状態が Right に変わった後に実行する WiringAction 群です。")]
        [SerializeField] private WiringAction[] onRightWiringActions = Array.Empty<WiringAction>();

        [Header("Debug")]
        [Tooltip("実行中の現在レバー状態です。")]
        [SerializeField, ReadOnly] private LeverDirection currentState;
        [Tooltip("レバー遷移アニメーション実行中は true になります。")]
        [SerializeField, ReadOnly] private bool isTransitioning;

        private readonly List<EntityRef> entityTargetBuffer = new(8);
        private MaterialPropertyBlock propertyBlock;

        private SceneKernel sceneKernel;
        private EntityMB selfEntityMB;
        private Coroutine transitionCoroutine;
        private bool nextMiddleStepTowardRight;

        public Transform InteractionTransform => interactionTransform != null ? interactionTransform : transform;
        public float RequiredHoldDuration => requiredHoldDuration;
        public InteractionVisualTargetMB VisualTarget => visualTarget;
        public Transform PromptAnchor => promptAnchor != null ? promptAnchor : InteractionTransform;
        public Vector3 PromptWorldOffset => promptWorldOffset;
        public string PromptDetailText => promptDetailText ?? string.Empty;

        private void Reset()
        {
            interactionTransform = transform;
            promptAnchor = transform;
            leverTransform = transform;
            visualTarget = GetComponentInChildren<InteractionVisualTargetMB>(true);
            currentState = NormalizeDirection(initialState);
            SyncPoseAndVisualImmediate();
        }

        private void Awake()
        {
            EnsurePropertyBlock();
            ResolveReferences();
            currentState = NormalizeDirection(initialState);
            nextMiddleStepTowardRight = currentState != LeverDirection.Right;
            SyncPoseAndVisualImmediate();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (Application.isPlaying)
            {
                currentState = NormalizeDirection(initialState);
                nextMiddleStepTowardRight = currentState != LeverDirection.Right;
                SyncPoseAndVisualImmediate();
            }
        }

        private void OnDisable()
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            isTransitioning = false;
        }

        private void OnValidate()
        {
            EnsurePropertyBlock();

            if (interactionTransform == null)
                interactionTransform = transform;

            if (promptAnchor == null)
                promptAnchor = interactionTransform;

            if (leverTransform == null)
                leverTransform = transform;

            maxInteractionDistance = Mathf.Max(0.05f, maxInteractionDistance);
            requiredHoldDuration = Mathf.Max(0f, requiredHoldDuration);
            duration = Mathf.Max(0.01f, duration);
            currentState = NormalizeDirection(currentState);

            if (visualTarget == null)
                visualTarget = GetComponentInChildren<InteractionVisualTargetMB>(true);

            if (!Application.isPlaying)
            {
                NormalizeVariableScopes();
                SyncPoseAndVisualImmediate();
            }
        }

        private void NormalizeVariableScopes()
        {
            leftVariable?.NormalizeForLeverUsage();
            middleVariable?.NormalizeForLeverUsage();
            rightVariable?.NormalizeForLeverUsage();
        }

        public bool TryGetCandidateScore(InteractionQuery query, out float score)
        {
            score = float.MaxValue;

            if (!isActiveAndEnabled || isTransitioning)
                return false;

            Transform target = InteractionTransform;
            if (target == null)
                return false;

            return InteractionScoringUtility.TryGetPlanarFacingScore(
                query,
                target.position,
                maxInteractionDistance,
                maxInteractionAngle,
                out score);
        }

        public void OnInteractionStarted(InteractionEventData eventData)
        {
        }

        public void OnInteractionUpdated(InteractionEventData eventData)
        {
        }

        public void OnInteractionCanceled(InteractionEventData eventData)
        {
        }

        public void OnInteractionCompleted(InteractionEventData eventData)
        {
            if (isTransitioning)
                return;

            LeverDirection nextState = DetermineNextState();

            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            transitionCoroutine = StartCoroutine(AnimateToStateCoroutine(nextState, eventData));
        }

        private IEnumerator AnimateToStateCoroutine(LeverDirection targetState, InteractionEventData eventData)
        {
            isTransitioning = true;

            Transform targetTransform = leverTransform != null ? leverTransform : transform;
            Quaternion from = targetTransform.localRotation;
            Quaternion to = Quaternion.Euler(GetVisual(targetState).LocalEulerAngles);
            float elapsed = 0f;
            float total = Mathf.Max(0.01f, duration);

            while (elapsed < total)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / total);
                float eased = EvaluateEasing(t);
                targetTransform.localRotation = Quaternion.Slerp(from, to, eased);
                yield return null;
            }

            targetTransform.localRotation = to;
            CommitStateAfterAnimation(targetState, eventData);

            isTransitioning = false;
            transitionCoroutine = null;
        }

        private void CommitStateAfterAnimation(LeverDirection state, InteractionEventData eventData)
        {
            currentState = NormalizeDirection(state);

            if (currentState == LeverDirection.Left)
                nextMiddleStepTowardRight = true;
            else if (currentState == LeverDirection.Right)
                nextMiddleStepTowardRight = false;

            ApplyVisual(currentState);
            ApplyStateVariables(currentState, eventData);
            ExecuteStateActions(currentState, eventData);
        }

        private LeverDirection DetermineNextState()
        {
            if (!hasMiddleState)
                return currentState == LeverDirection.Left ? LeverDirection.Right : LeverDirection.Left;

            return currentState switch
            {
                LeverDirection.Left => LeverDirection.Middle,
                LeverDirection.Right => LeverDirection.Middle,
                LeverDirection.Middle => nextMiddleStepTowardRight ? LeverDirection.Right : LeverDirection.Left,
                _ => LeverDirection.Left,
            };
        }

        private float EvaluateEasing(float t)
        {
            if (easingCurve == null || easingCurve.length == 0)
                return t;

            return Mathf.Clamp01(easingCurve.Evaluate(t));
        }

        private void ResolveReferences()
        {
            if (sceneKernel == null)
            {
                SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();
                if (kernelMB != null)
                    sceneKernel = kernelMB.Kernel;
            }

            if (selfEntityMB == null)
                selfEntityMB = GetComponentInParent<EntityMB>();
        }

        private void SyncPoseAndVisualImmediate()
        {
            Transform targetTransform = leverTransform != null ? leverTransform : transform;
            targetTransform.localRotation = Quaternion.Euler(GetVisual(currentState).LocalEulerAngles);
            ApplyVisual(currentState);
        }

        private void ApplyVisual(LeverDirection activeState)
        {
            LeverDirectionVisual activeVisual = GetVisual(activeState);
            bool isLeftActive = activeState == LeverDirection.Left;
            bool isRightActive = activeState == LeverDirection.Right;

            ApplyRendererVisual(
                leftNavigationMesh,
                left.GetBaseColor(isLeftActive),
                isLeftActive && left.EmitWhenSelected,
                left.OnColor,
                left.EmissionStrength,
                left.SimpleBoostIntensity);

            ApplyRendererVisual(
                rightNavigationMesh,
                right.GetBaseColor(isRightActive),
                isRightActive && right.EmitWhenSelected,
                right.OnColor,
                right.EmissionStrength,
                right.SimpleBoostIntensity);

            ApplyRendererVisual(
                handleMesh,
                activeVisual.GetBaseColor(true),
                activeVisual.EmitWhenSelected,
                activeVisual.OnColor,
                activeVisual.EmissionStrength,
                activeVisual.SimpleBoostIntensity);
        }

        private void ApplyRendererVisual(
            Renderer targetRenderer,
            Color baseColor,
            bool emissionEnabled,
            Color emissionColor,
            float emissionStrength,
            float simpleBoostIntensity)
        {
            if (targetRenderer == null)
                return;

            EnsurePropertyBlock();

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, baseColor);
            propertyBlock.SetColor(ColorId, baseColor);

            float resolvedEmissionStrength = emissionEnabled ? Mathf.Max(0f, emissionStrength) : 0f;
            propertyBlock.SetColor(EmissionColorId, emissionColor);
            propertyBlock.SetFloat(EmissionStrengthId, resolvedEmissionStrength);

            if (syncEnvironmentSimpleBoost)
            {
                float boost = emissionEnabled ? Mathf.Max(0f, simpleBoostIntensity) : 0f;
                propertyBlock.SetFloat(SimpleBoostEnabledId, boost > 0.0001f ? 1f : 0f);
                propertyBlock.SetColor(SimpleBoostColorId, emissionColor);
                propertyBlock.SetFloat(SimpleBoostIntensityId, boost);
            }

            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
        }

        private void ApplyStateVariables(LeverDirection activeState, InteractionEventData eventData)
        {
            if (!writeStateVariables)
                return;

            ResolveReferences();

            if (sceneKernel == null || sceneKernel.EntityValueStore == null)
            {
                Debug.LogWarning($"{nameof(LeverObjectMB)}: SceneKernel entity value store is not available. State variables were skipped.", this);
                return;
            }

            WiringActionContext context = BuildWiringContext(eventData);
            TryApplyVariable(leftVariable, activeState == LeverDirection.Left, context);

            if (hasMiddleState)
                TryApplyVariable(middleVariable, activeState == LeverDirection.Middle, context);
            else
                TryApplyVariable(middleVariable, false, context);

            TryApplyVariable(rightVariable, activeState == LeverDirection.Right, context);
        }

        private void TryApplyVariable(LeverVariableBinding variable, bool value, in WiringActionContext context)
        {
            if (variable == null || !variable.Enabled)
                return;

            if (value)
            {
                if (!variable.WriteOnTrue)
                    return;
            }
            else
            {
                if (!variable.WriteOnFalse)
                    return;
            }

            TrySetScopedBool(variable.EntityBoolKey, variable.Scope, variable.EntityTarget, value, context);
        }

        private void TrySetScopedBool(
            ValueKeyReference keyReference,
            ValueStoreWriteStoreScope scope,
            EntityTargetReference entityTarget,
            bool value,
            in WiringActionContext context)
        {
            if (!keyReference.TryResolve(out ValueKeyDescriptor descriptor))
                return;

            if (descriptor.ValueType != typeof(bool))
            {
                Debug.LogWarning($"{nameof(LeverObjectMB)}: Key '{descriptor.Path}' is not bool.", this);
                return;
            }

            if (!ValueStoreWriteScopeUtility.IsKeyCompatible(scope, descriptor))
            {
                Debug.LogWarning($"{nameof(LeverObjectMB)}: Scope '{scope}' does not match key '{descriptor.Path}'.", this);
                return;
            }

            int resolvedCount = ValueStoreWriteScopeUtility.ResolveTargets(context, scope, entityTarget, entityTargetBuffer);
            if (resolvedCount == 0)
            {
                Debug.LogWarning($"{nameof(LeverObjectMB)}: Failed to resolve target entity for scope '{scope}'.", this);
                return;
            }

            ValueKey<bool> key = descriptor.GetKey<bool>();
            for (int i = 0; i < resolvedCount; i++)
                sceneKernel.EntityValueStore.Set(entityTargetBuffer[i], key, value);
        }

        private void ExecuteStateActions(LeverDirection state, InteractionEventData eventData)
        {
            WiringActionContext context = BuildWiringContext(eventData);

            switch (state)
            {
                case LeverDirection.Left:
                    WiringActionRunner.ExecuteAll(onLeftWiringActions, context);
                    ExecuteInlineAction(onLeftInlineAction, eventData);
                    break;

                case LeverDirection.Middle:
                    WiringActionRunner.ExecuteAll(onMiddleWiringActions, context);
                    ExecuteInlineAction(onMiddleInlineAction, eventData);
                    break;

                case LeverDirection.Right:
                    WiringActionRunner.ExecuteAll(onRightWiringActions, context);
                    ExecuteInlineAction(onRightInlineAction, eventData);
                    break;
            }
        }

        private void ExecuteInlineAction(InlineAction inlineAction, InteractionEventData eventData)
        {
            if (inlineAction == null)
                return;

            ResolveReferences();

            if (selfEntityMB == null || !selfEntityMB.HasEntity)
            {
                Debug.LogWarning($"{nameof(LeverObjectMB)}: InlineAction was skipped because self Entity is not available.", this);
                return;
            }

            InlineActionExecutionUtility.ExecuteAndForget(
                this,
                selfEntityMB.Entity,
                inlineAction,
                eventData.SourceEntity,
                $"{nameof(LeverObjectMB)}.{currentState}");
        }

        private WiringActionContext BuildWiringContext(InteractionEventData eventData)
        {
            ResolveReferences();

            EntityRef selfEntity = selfEntityMB != null && selfEntityMB.HasEntity ? selfEntityMB.Entity : default;
            EntityTagId selfTag = selfEntityMB != null ? selfEntityMB.Tag : default;
            GameObject triggerObject = eventData.SourceFacingTransform != null ? eventData.SourceFacingTransform.gameObject : null;

            return new WiringActionContext(
                sceneKernel,
                gameObject,
                transform,
                selfEntity,
                selfTag,
                triggerObject,
                eventData.SourceFacingTransform,
                eventData.SourceEntity,
                default);
        }

        private LeverDirection NormalizeDirection(LeverDirection direction)
        {
            if (!hasMiddleState && direction == LeverDirection.Middle)
                return LeverDirection.Left;

            return direction;
        }

        private LeverDirectionVisual GetVisual(LeverDirection direction)
        {
            return direction switch
            {
                LeverDirection.Left => left,
                LeverDirection.Middle => hasMiddleState ? middle : left,
                LeverDirection.Right => right,
                _ => left,
            };
        }
    }
}
