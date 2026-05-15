using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    // Runtime では、1フレーム分の入力と出力をこの構造体でやり取りする。
    [System.Serializable]
    public struct UIAnimationRuntimeData
    {
        public Transform TargetTransform;
        public RectTransform TargetRectTransform;
        public Graphic TargetGraphic;
        public bool IsLocal;
        public float DeltaTime;
        public float Time;
        public uint FrameCount;
        public float ApplyScale;

        public Vector3 DeltaPosition;
        public Vector3 DeltaScale;
        public Vector3 DeltaRotation;
        public bool HasColorOutput;
        public Color OutputColor;

        public void ResetFrameResult()
        {
            DeltaPosition = Vector3.zero;
            DeltaScale = Vector3.zero;
            DeltaRotation = Vector3.zero;
            HasColorOutput = false;
            OutputColor = Color.clear;
        }
    }

    // UIAnimationMB が保持する Runtime の親クラス。
    public sealed class UIAnimationRuntime
    {
        private readonly UIAnimationMoveRuntime moveRuntime;
        private readonly UIAnimationScaleRuntime scaleRuntime;
        private readonly UIAnimationRotateRuntime rotateRuntime;
        private readonly UIAnimationColorRuntime colorRuntime;

        public UIAnimationRuntime(UIAnimationSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            // Flagsで選ばれたSettingだけRuntime化し、Update中の分岐と参照を最小限にする。
            if (settings.AnimationType.HasFlag(UIAnimationType.TransformMove) && settings.MoveSetting != null)
            {
                moveRuntime = new UIAnimationMoveRuntime(settings.MoveSetting);
            }

            if (settings.AnimationType.HasFlag(UIAnimationType.TransformScale) && settings.ScaleSetting != null)
            {
                scaleRuntime = new UIAnimationScaleRuntime(settings.ScaleSetting);
            }

            if (settings.AnimationType.HasFlag(UIAnimationType.TransformRotate) && settings.RotateSetting != null)
            {
                rotateRuntime = new UIAnimationRotateRuntime(settings.RotateSetting);
            }

            if (settings.AnimationType.HasFlag(UIAnimationType.ImageColor) && settings.ColorSetting != null)
            {
                colorRuntime = new UIAnimationColorRuntime(settings.ColorSetting);
            }
        }

        public void Update(ref UIAnimationRuntimeData runtimeData)
        {
            moveRuntime?.Update(ref runtimeData);
            scaleRuntime?.Update(ref runtimeData);
            rotateRuntime?.Update(ref runtimeData);
            colorRuntime?.Update(ref runtimeData);
        }
    }

    // 移動設定の Runtime。
    public sealed class UIAnimationMoveRuntime
    {
        private readonly UIAnimationMoveSetting moveSetting;
        private readonly Vector3 scrollVelocity;
        private Vector3 previousShakeOffset;
        private bool hasPreviousShakeOffset;

        public UIAnimationMoveRuntime(UIAnimationMoveSetting moveSetting)
        {
            this.moveSetting = moveSetting;
            // RandomRangeはRuntime生成時に一度だけ解決し、毎フレーム値が跳ねないようにする。
            scrollVelocity = ResolveVector3(
                moveSetting != null ? moveSetting.ValueSource : ValueSourceType.Constant,
                moveSetting != null ? moveSetting.ScrollVelocity : Vector3.zero,
                moveSetting != null ? moveSetting.ScrollVelocityMin : Vector3.zero,
                moveSetting != null ? moveSetting.ScrollVelocityMax : Vector3.zero);
        }

        private static Vector3 ResolveVector3(ValueSourceType valueSourceType, Vector3 constantValue, Vector3 minValue, Vector3 maxValue)
        {
            if (valueSourceType == ValueSourceType.RandomRange)
            {
                return new Vector3(
                    Random.Range(minValue.x, maxValue.x),
                    Random.Range(minValue.y, maxValue.y),
                    Random.Range(minValue.z, maxValue.z)
                );
            }

            return constantValue;
        }

        public void Update(ref UIAnimationRuntimeData runtimeData)
        {
            if (runtimeData.TargetTransform == null || moveSetting == null)
            {
                return;
            }

            // 速度は設定値を基準に計算し、必要なら減衰と揺れを重ねる。
            Vector3 deltaPosition = scrollVelocity * runtimeData.DeltaTime;

            if (moveSetting.Damping != 1f)
            {
                float damping = Mathf.Max(0f, moveSetting.Damping);
                deltaPosition *= Mathf.Pow(damping, runtimeData.DeltaTime);
            }

            if (moveSetting.ApplyShakeEffect)
            {
                // Shake は絶対オフセットの差分だけを足し、毎フレーム位置が積み上がらないようにする。
                Vector3 currentShakeOffset = ResolveShakeOffset(runtimeData);
                deltaPosition += hasPreviousShakeOffset
                    ? currentShakeOffset - previousShakeOffset
                    : currentShakeOffset;
                previousShakeOffset = currentShakeOffset;
                hasPreviousShakeOffset = true;
            }
            else
            {
                previousShakeOffset = Vector3.zero;
                hasPreviousShakeOffset = false;
            }

            runtimeData.DeltaPosition += deltaPosition * runtimeData.ApplyScale;
        }

        private Vector3 ResolveShakeOffset(UIAnimationRuntimeData runtimeData)
        {
            float shakeOffsetX = Mathf.Sin(runtimeData.Time * moveSetting.ShakeFrequency) * moveSetting.ShakeMagnitude;
            float shakeOffsetY = Mathf.Cos(runtimeData.Time * moveSetting.ShakeFrequency) * moveSetting.ShakeMagnitude;
            return new Vector3(shakeOffsetX, shakeOffsetY, 0f);
        }
    }

    // スケール設定の Runtime。
    public sealed class UIAnimationScaleRuntime
    {
        private readonly Vector3 scaleMultiplier;

        public UIAnimationScaleRuntime(UIAnimationScaleSetting scaleSetting)
        {
            // ScaleのRandomRangeも開始時に固定する。ループ中に倍率が変わる演出は別Settingで扱う。
            scaleMultiplier = ResolveVector3(
                scaleSetting != null ? scaleSetting.ValueSource : ValueSourceType.Constant,
                scaleSetting != null ? scaleSetting.ScaleMultiplier : Vector3.one,
                scaleSetting != null ? scaleSetting.ScaleMultiplierMin : Vector3.one,
                scaleSetting != null ? scaleSetting.ScaleMultiplierMax : Vector3.one);
        }

        private static Vector3 ResolveVector3(ValueSourceType valueSourceType, Vector3 constantValue, Vector3 minValue, Vector3 maxValue)
        {
            if (valueSourceType == ValueSourceType.RandomRange)
            {
                return new Vector3(
                    Random.Range(minValue.x, maxValue.x),
                    Random.Range(minValue.y, maxValue.y),
                    Random.Range(minValue.z, maxValue.z)
                );
            }

            return constantValue;
        }

        public void Update(ref UIAnimationRuntimeData runtimeData)
        {
            if (runtimeData.TargetTransform == null)
            {
                return;
            }

            // local/world の基準は旧実装に合わせて切り替える。
            Vector3 baseScale = runtimeData.IsLocal ? runtimeData.TargetTransform.localScale : runtimeData.TargetTransform.lossyScale;
            Vector3 deltaScale = Vector3.Scale(baseScale, scaleMultiplier - Vector3.one) * runtimeData.DeltaTime * runtimeData.ApplyScale;
            runtimeData.DeltaScale += deltaScale;
        }
    }

    // 回転設定の Runtime。
    public sealed class UIAnimationRotateRuntime
    {
        private readonly Vector3 rotateAngle;

        public UIAnimationRotateRuntime(UIAnimationRotateSetting rotateSetting)
        {
            // Rotateは角速度として扱うため、RandomRangeはRuntimeごとの個体差として保持する。
            rotateAngle = ResolveVector3(
                rotateSetting != null ? rotateSetting.ValueSource : ValueSourceType.Constant,
                rotateSetting != null ? rotateSetting.RotateAngle : Vector3.zero,
                rotateSetting != null ? rotateSetting.RotateAngleMin : Vector3.zero,
                rotateSetting != null ? rotateSetting.RotateAngleMax : Vector3.zero);
        }

        private static Vector3 ResolveVector3(ValueSourceType valueSourceType, Vector3 constantValue, Vector3 minValue, Vector3 maxValue)
        {
            if (valueSourceType == ValueSourceType.RandomRange)
            {
                return new Vector3(
                    Random.Range(minValue.x, maxValue.x),
                    Random.Range(minValue.y, maxValue.y),
                    Random.Range(minValue.z, maxValue.z)
                );
            }

            return constantValue;
        }

        public void Update(ref UIAnimationRuntimeData runtimeData)
        {
            if (runtimeData.TargetTransform == null)
            {
                return;
            }

            runtimeData.DeltaRotation += rotateAngle * runtimeData.DeltaTime * runtimeData.ApplyScale;
        }
    }

    // 色設定の Runtime。
    public sealed class UIAnimationColorRuntime
    {
        private readonly UIAnimationColorSetting colorSetting;
        private bool hasAppliedOnce;
        private float colorTimer;
        private int currentColorIndex;

        public UIAnimationColorRuntime(UIAnimationColorSetting colorSetting)
        {
            this.colorSetting = colorSetting;
        }

        public void Update(ref UIAnimationRuntimeData runtimeData)
        {
            Graphic targetGraphic = runtimeData.TargetGraphic;
            if (targetGraphic == null)
            {
                return;
            }

            if (colorSetting.ApplyOnce)
            {
                if (!hasAppliedOnce)
                {
                    runtimeData.OutputColor = ResolveColor(targetGraphic, false);
                    runtimeData.HasColorOutput = true;
                    hasAppliedOnce = true;
                }

                return;
            }

            float interval = Mathf.Max(0.001f, colorSetting.ColorChangeInterval);
            colorTimer += runtimeData.DeltaTime;

            if (!hasAppliedOnce || colorTimer >= interval)
            {
                colorTimer = 0f;
                runtimeData.OutputColor = ResolveColor(targetGraphic, true);
                runtimeData.HasColorOutput = true;
                hasAppliedOnce = true;
            }
        }

        private Color ResolveColor(Graphic targetGraphic, bool advanceCycle)
        {
            switch (colorSetting.ChangeMode)
            {
                case UIAnimationColorSetting.ColorChangeMode.Random:
                    return ResolveRandomColor(targetGraphic);
                case UIAnimationColorSetting.ColorChangeMode.Cycle:
                default:
                    return ResolveCycleColor(targetGraphic, advanceCycle);
            }
        }

        private Color ResolveCycleColor(Graphic targetGraphic, bool advanceCycle)
        {
            Color[] colors = colorSetting.Colors;
            if (colors != null && colors.Length > 0)
            {
                int index = Mathf.Clamp(currentColorIndex, 0, colors.Length - 1);
                Color selectedColor = colors[index];

                if (advanceCycle)
                {
                    currentColorIndex = (currentColorIndex + 1) % colors.Length;
                }

                return selectedColor;
            }

            return targetGraphic.color;
        }

        private Color ResolveRandomColor(Graphic targetGraphic)
        {
            ColorWeight[] colorWeights = colorSetting.ColorWeights;
            if (colorWeights != null && colorWeights.Length > 0)
            {
                // Weightが0以下の要素は抽選対象から外し、全滅した場合は通常Colorsへフォールバックする。
                float totalWeight = 0f;
                for (int i = 0; i < colorWeights.Length; i++)
                {
                    ColorWeight weight = colorWeights[i];
                    if (weight == null)
                    {
                        continue;
                    }

                    totalWeight += Mathf.Max(0f, weight.Weight);
                }

                if (totalWeight > 0f)
                {
                    float randomWeight = Random.value * totalWeight;
                    float cumulativeWeight = 0f;

                    for (int i = 0; i < colorWeights.Length; i++)
                    {
                        ColorWeight weight = colorWeights[i];
                        if (weight == null)
                        {
                            continue;
                        }

                        float normalizedWeight = Mathf.Max(0f, weight.Weight);
                        if (normalizedWeight <= 0f)
                        {
                            continue;
                        }

                        cumulativeWeight += normalizedWeight;
                        if (randomWeight <= cumulativeWeight)
                        {
                            return weight.Color;
                        }
                    }

                    for (int i = colorWeights.Length - 1; i >= 0; i--)
                    {
                        if (colorWeights[i] != null)
                        {
                            return colorWeights[i].Color;
                        }
                    }
                }
            }

            Color[] colors = colorSetting.Colors;
            if (colors != null && colors.Length > 0)
            {
                return colors[Random.Range(0, colors.Length)];
            }

            return targetGraphic.color;
        }
    }
}