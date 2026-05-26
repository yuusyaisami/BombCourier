using BC.Manager;
using BC.UI;
using UnityEngine;

namespace BC.Managers
{
    [DisallowMultipleComponent]
    public sealed class UIManagerMB : MonoBehaviour
    {
        public static UIManagerMB Instance { get; private set; }

        [Header("References")]
        [SerializeField] private UIFadeEffectMB fadeEffect;
        [SerializeField] private UIGameSceneManagerMB gameSceneManager;
        [SerializeField] private UIIntroPathSkipMB introPathSkipUI;
        [SerializeField] private UIToastStackMB toastStackUI;
        [SerializeField] private UITalkSystemMB talkSystemUI;
        [SerializeField] private UITalkChoiceSystemMB talkChoiceSystemUI;

        public UIFadeEffectMB FadeEffect => fadeEffect;
        public UIGameSceneManagerMB GameSceneManager => gameSceneManager;
        public UIIntroPathSkipMB IntroPathSkipUI => introPathSkipUI;
        public UIToastStackMB ToastStackUI => toastStackUI;
        public UITalkSystemMB TalkSystemUI => talkSystemUI;
        public UITalkChoiceSystemMB TalkChoiceSystemUI => talkChoiceSystemUI;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void ResolveReferences()
        {
            fadeEffect ??= GetComponentInChildren<UIFadeEffectMB>(true);
            gameSceneManager ??= GetComponentInChildren<UIGameSceneManagerMB>(true);
            introPathSkipUI ??= GetComponentInChildren<UIIntroPathSkipMB>(true);
            toastStackUI ??= GetComponentInChildren<UIToastStackMB>(true);
            talkSystemUI ??= GetComponentInChildren<UITalkSystemMB>(true);
            talkChoiceSystemUI ??= GetComponentInChildren<UITalkChoiceSystemMB>(true);
        }
    }
}
