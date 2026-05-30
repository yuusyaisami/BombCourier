using UnityEngine;

namespace BC.Audio
{
    // AudioSystem への再生命令に付けるクリップ固有パラメーターをまとめた SO。
    //
    // baseVolume: このクリップ固有の基準音量 (0〜1)。
    //             ゲーム全体の SE/BGM スライダー値とは独立したパラメーターで、
    //             音声ごとの音量バランスを整えるために使用する。
    //             実際の再生音量 = baseVolume × (ゲーム側の SE または BGM 音量)。
    //
    // pitch: AudioSource.pitch と同義。再生速度とピッチを同時に変える。1 = 通常速度。
    [CreateAssetMenu(menuName = "BombCourier/Audio/Audio Data", fileName = "AudioData")]
    public sealed class AudioDataSO : ScriptableObject
    {
        [SerializeField] private AudioClip clip;

        [SerializeField, Range(0f, 1f)]
        private float baseVolume = 1f;

        [Tooltip("再生速度とピッチを同時に変える。1 = 通常速度。")]
        [SerializeField, Range(0.1f, 3f)]
        private float pitch = 1f;

        public AudioClip Clip => clip;
        public float BaseVolume => baseVolume;
        public float Pitch => pitch;
    }
}
