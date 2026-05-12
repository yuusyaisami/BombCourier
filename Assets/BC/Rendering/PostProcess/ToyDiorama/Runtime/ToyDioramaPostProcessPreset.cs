using UnityEngine;

namespace BC.Rendering
{
    [CreateAssetMenu(
        fileName = "ToyDioramaPreset",
        menuName = "BC/Rendering/Toy Diorama Preset",
        order = 1200)]
    public sealed class ToyDioramaPostProcessPreset : ScriptableObject
    {
        [SerializeField] private ToyDioramaPresetKind presetKind = ToyDioramaPresetKind.SoftToy;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private ToyDioramaPostProcessSettings settings = new ToyDioramaPostProcessSettings();

        public ToyDioramaPresetKind PresetKind => presetKind;

        public string Description => description;

        public ToyDioramaPostProcessSettings Settings => settings;

        public void ApplyTo(ToyDioramaPostProcessSettings target)
        {
            if (target == null)
            {
                return;
            }

            target.CopyAuthoringValuesFrom(settings);
        }
    }
}