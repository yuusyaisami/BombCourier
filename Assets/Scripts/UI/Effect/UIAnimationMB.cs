using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    public class UIAnimationMB : MonoBehaviour
    {
        [SerializeField] private UIAnimationSettings settings = new UIAnimationSettings();
        [SerializeField] private Transform targetTransform; // アニメーションの対象となるTransform
        [SerializeField, ReadOnly] private RectTransform targetRectTransform; // ターゲットがRectTransformの場合のキャッシュ
        [SerializeField] private bool isLocal = true; // ローカル座標を使用するかどうか

        // Start で構築した Runtime 本体。
        private UIAnimationRuntime runtime;
        private Graphic targetGraphic; // 色変更の対象となるGraphicコンポーネント
        private bool isInitialized;

        private void Reset()
        {
            targetTransform = transform;
            ResolveTargetRectTransform();
            ResolveTargetGraphic();
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnValidate()
        {
            if (settings == null)
            {
                settings = new UIAnimationSettings();
            }

            if (targetTransform == null)
            {
                targetTransform = transform;
            }
            ResolveTargetRectTransform();
            ResolveTargetGraphic();
            if (Application.isPlaying)
            {
                runtime = null;
                isInitialized = false;
            }
        }

        private void Start()
        {
            InitializeIfNeeded();
        }

        private void Update()
        {
            if (!isInitialized)
            {
                InitializeIfNeeded();
            }

            // Runtime へ渡すフレーム共通データを組み立てる。
            UIAnimationRuntimeData runtimeData = CreateRuntimeData();
            runtime?.Update(ref runtimeData);
            ApplyRuntimeResult(runtimeData);
        }

        private void InitializeIfNeeded()
        {
            if (isInitialized)
            {
                return;
            }

            if (settings == null)
            {
                settings = new UIAnimationSettings();
            }

            if (targetTransform == null)
            {
                targetTransform = transform;
            }

            ResolveTargetGraphic();
            ResolveTargetRectTransform();
            BuildRuntime();
            isInitialized = true;
        }

        private void BuildRuntime()
        {
            // Setting をもとに Runtime を一度だけ組み立てる。
            runtime = new UIAnimationRuntime(settings);
        }

        private UIAnimationRuntimeData CreateRuntimeData()
        {
            UIAnimationRuntimeData runtimeData = new UIAnimationRuntimeData
            {
                TargetTransform = targetTransform,
                TargetRectTransform = targetRectTransform,
                TargetGraphic = targetGraphic,
                IsLocal = isLocal,
                DeltaTime = Time.deltaTime,
                Time = Time.time,
                FrameCount = (uint)Time.frameCount,
                ApplyScale = settings != null ? settings.applyScale : 1f,
            };

            runtimeData.ResetFrameResult();
            return runtimeData;
        }

        private void ApplyRuntimeResult(UIAnimationRuntimeData runtimeData)
        {
            ApplyTransformAnimation(runtimeData);

            if (runtimeData.HasColorOutput && targetGraphic != null)
            {
                targetGraphic.color = runtimeData.OutputColor;
            }
        }

        private void ResolveTargetGraphic()
        {
            if (targetTransform == null)
            {
                targetGraphic = null;
                return;
            }

            if (targetGraphic != null && targetGraphic.transform == targetTransform)
            {
                return;
            }

            targetGraphic = targetTransform.GetComponent<Graphic>();
        }

        private void ResolveTargetRectTransform()
        {
            targetRectTransform = targetTransform as RectTransform;
        }

        // Runtime が計算した差分だけをここで実体へ反映する。
        private void ApplyTransformAnimation(UIAnimationRuntimeData runtimeData)
        {
            Transform animationTarget = runtimeData.TargetTransform;
            if (animationTarget == null)
            {
                return;
            }

            if (runtimeData.DeltaPosition != Vector3.zero)
            {
                if (runtimeData.IsLocal)
                {
                    if (runtimeData.TargetRectTransform != null)
                    {
                        // RectTransform の場合は anchoredPosition を直接操作する。
                        RectTransform rectTransform = runtimeData.TargetRectTransform;
                        rectTransform.anchoredPosition += new Vector2(runtimeData.DeltaPosition.x, runtimeData.DeltaPosition.y);
                        // Z軸の移動は localPosition で行う（anchoredPosition には Z軸がないため）
                        Vector3 localPos = rectTransform.localPosition;
                        localPos.z += runtimeData.DeltaPosition.z;
                        rectTransform.localPosition = localPos;
                    }
                    else
                    {
                        animationTarget.localPosition += runtimeData.DeltaPosition;
                    }
                }
                else
                {
                    if (runtimeData.TargetRectTransform != null)
                    {
                        // ワールド座標での移動を anchoredPosition に反映させるためには、親の回転やスケールも考慮する必要がある。
                        RectTransform rectTransform = runtimeData.TargetRectTransform;
                        Vector3 worldDelta = runtimeData.DeltaPosition;
                        if (rectTransform.parent != null)
                        {
                            worldDelta = rectTransform.parent.InverseTransformVector(worldDelta);
                        }
                        rectTransform.anchoredPosition += new Vector2(worldDelta.x, worldDelta.y);
                        // Z軸の移動は localPosition で行う
                        Vector3 localPos = rectTransform.localPosition;
                        localPos.z += worldDelta.z;
                        rectTransform.localPosition = localPos;
                    }
                    else
                    {
                        animationTarget.position += runtimeData.DeltaPosition;
                    }
                }
            }

            if (runtimeData.DeltaScale != Vector3.zero)
            {
                animationTarget.localScale += runtimeData.DeltaScale;
            }

            if (runtimeData.DeltaRotation != Vector3.zero)
            {
                if (runtimeData.IsLocal)
                {
                    animationTarget.localEulerAngles += runtimeData.DeltaRotation;
                }
                else
                {
                    animationTarget.eulerAngles += runtimeData.DeltaRotation;
                }
            }
        }
    }
}