namespace BC.Managers
{
    // ゲームの設定を管理するクラス。
    // ゲーム全体の設定を保持し、UIと連携して設定の変更を反映させる役割を持つ。
    public class SettingManagerMB : MonoBehaviour
    {
        public static SettingManagerMB Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Multiple instances of {nameof(SettingManagerMB)} detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンを跨いで設定を保持する場合はこれを有効にする
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ここにゲーム全体の設定を保持するプロパティやメソッドを追加していく
        public bool IsInvertYAxis { get; private set; }
        public float CameraSensitivity { get; private set; }
        public float MusicVolume { get; private set; }
        public float SFXVolume { get; private set; }
    }
}