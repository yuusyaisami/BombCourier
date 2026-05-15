using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.UI
{
    public enum FadeType
    {
        Single, // 単一フェード
        TopBottom // 上下フェード
    }

    public interface IUIFadeEffect
    {
        void SetFadeType(FadeType fadeType);
        UniTask StartFadeAsync(FadeType fadeType, float amount, float duration);
    }

    public enum FallEffectPlayMode
    {
        Loop, // ループ再生
        Once // 一回再生
    }

    public interface IUIFallEffect
    {
        // 落下エフェクトの開始
        void StartFallEffect(FallEffectPlayMode playMode = FallEffectPlayMode.Loop);
        // 落下エフェクトの終了
        void EndFallEffect();
    }

    [Serializable]
    public class FallEffectSettings
    {
        // 落下エフェクトの持続時間
        public float Duration = 1f;
        // 落下エフェクトの最大強度
        public float MaxIntensity = 1f;
    }

    [Serializable]
    public class FallEffectElementData
    {
        public Sprite sprite;
        public int weight;
    }

    [System.Flags]
    public enum UIAnimationType : byte
    {
        None = 0,
        TransformMove = 1 << 0,
        TransformScale = 1 << 1,
        TransformRotate = 1 << 2,
        ImageColor = 1 << 3,
    }
    public enum ValueSourceType
    {
        Constant = 10,
        RandomRange = 20,
    }
    [Serializable]
    public class UIAnimationMoveSetting
    {
        // 指定された方向に移動する速度
        public ValueSourceType ValueSource = ValueSourceType.Constant;
        [ShowIf("@ValueSource == ValueSourceType.Constant")]
        public Vector3 ScrollVelocity = Vector3.zero;
        [ShowIf("@ValueSource == ValueSourceType.RandomRange")]
        public Vector3 ScrollVelocityMin = Vector3.zero;
        [ShowIf("@ValueSource == ValueSourceType.RandomRange")]
        public Vector3 ScrollVelocityMax = Vector3.zero;
        // 移動の減衰率。1以上で加速、0-1で減速
        public float Damping = 1f;
        public bool ApplyShakeEffect = false; // 追加で振動エフェクトを適用するかどうか
        [ShowIf(nameof(ApplyShakeEffect))]
        public float ShakeMagnitude = 5f; // 振動の強さ
        [ShowIf(nameof(ApplyShakeEffect))]
        public float ShakeFrequency = 20f; // 振動の頻度
    }


    [Serializable]
    public class UIAnimationScaleSetting
    {
        public ValueSourceType ValueSource = ValueSourceType.Constant;
        [ShowIf("@ValueSource == ValueSourceType.Constant")]
        public Vector3 ScaleMultiplier = Vector3.one;
        [ShowIf("@ValueSource == ValueSourceType.RandomRange")]
        public Vector3 ScaleMultiplierMin = Vector3.one;
        [ShowIf("@ValueSource == ValueSourceType.RandomRange")]
        public Vector3 ScaleMultiplierMax = Vector3.one;
    }

    [Serializable]
    public class UIAnimationRotateSetting
    {
        public ValueSourceType ValueSource = ValueSourceType.Constant;
        [ShowIf("@ValueSource == ValueSourceType.Constant")]
        public Vector3 RotateAngle = Vector3.zero;
        [ShowIf("@ValueSource == ValueSourceType.RandomRange")]
        public Vector3 RotateAngleMin = Vector3.zero;
        [ShowIf("@ValueSource == ValueSourceType.RandomRange")]
        public Vector3 RotateAngleMax = Vector3.zero;
    }

    [Serializable]
    public class ColorWeight
    {
        public Color Color = Color.white;
        public float Weight = 1f;
    }

    [Serializable]
    public class UIAnimationColorSetting
    {
        public bool ApplyOnce = false; // 一度だけ色を変えるかどうか
        public enum ColorChangeMode
        {
            Cycle, // 色を順番に切り替える
            Random // 色をランダムに切り替える
        }

        public ColorChangeMode ChangeMode = ColorChangeMode.Cycle;
        [ShowIf("@!ApplyOnce")]
        public float ColorChangeInterval = 1f; // 色を切り替える間隔
        [ShowIf("@ChangeMode == ColorChangeMode.Random")]
        public ColorWeight[] ColorWeights; // 色とその重みの配列
        [ShowIf("@ChangeMode == ColorChangeMode.Cycle")]
        public Color[] Colors;
    }

    [Serializable]
    public class UIAnimationSettings
    {
        public UIAnimationType AnimationType = UIAnimationType.None;
        public float applyScale = 1f; // アニメーションの適用倍率。これを大きくするとアニメーションが強くなる
        // 指定された方向に移動する速度
        [ShowIf("@AnimationType.HasFlag(UIAnimationType.TransformMove)")]
        public UIAnimationMoveSetting MoveSetting = new UIAnimationMoveSetting();
        [ShowIf("@AnimationType.HasFlag(UIAnimationType.TransformScale)")]
        public UIAnimationScaleSetting ScaleSetting = new UIAnimationScaleSetting();
        [ShowIf("@AnimationType.HasFlag(UIAnimationType.TransformRotate)")]
        public UIAnimationRotateSetting RotateSetting = new UIAnimationRotateSetting();
        [ShowIf("@AnimationType.HasFlag(UIAnimationType.ImageColor)")]
        public UIAnimationColorSetting ColorSetting = new UIAnimationColorSetting();
    }
}