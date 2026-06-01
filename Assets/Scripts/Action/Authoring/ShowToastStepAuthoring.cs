using System;
using BC.Managers;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ShowToastStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private ToastRequestData toastRequestData;

        public override void Validate(ActionValidationContext context)
        {
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new ShowToastStepRuntime(toastRequestData));
        }
    }

    [Serializable]
    public sealed class ShowScreenOverlayStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private ScreenOverlayShowRequestData screenOverlayShowRequestData;

        public override void Validate(ActionValidationContext context)
        {
            screenOverlayShowRequestData.EnsureDefaultsInitialized();
            ScreenOverlayShowRequestData request = screenOverlayShowRequestData.Sanitize();

            if (!request.displayId.IsValid)
            {
                context.AddError("Screen overlay display id is required.");
                return;
            }

            switch (request.contentKind)
            {
                case ScreenOverlayContentKind.Image:
                    if (request.sprite == null)
                        context.AddError("Screen overlay image sprite is required.");

                    if (request.size.x <= 0f || request.size.y <= 0f)
                        context.AddError("Screen overlay image size must be greater than zero.");
                    break;

                case ScreenOverlayContentKind.Text:
                    if (string.IsNullOrWhiteSpace(request.text))
                        context.AddError("Screen overlay text is required.");

                    if (request.fontSize <= 0f)
                        context.AddError("Screen overlay font size must be greater than zero.");
                    break;

                case ScreenOverlayContentKind.Prefab:
                    if (request.prefab == null)
                    {
                        context.AddError("Screen overlay prefab is required.");
                        break;
                    }

                    if (!request.prefab.TryGetComponent(out RectTransform _))
                        context.AddError("Screen overlay prefab must contain a RectTransform on the root object.");
                    break;

                default:
                    context.AddError("Unsupported screen overlay content kind.");
                    break;
            }
        }

        public override void Compile(ActionCompileContext context)
        {
            screenOverlayShowRequestData.EnsureDefaultsInitialized();
            context.AddStep(new ShowScreenOverlayStepRuntime(screenOverlayShowRequestData));
        }
    }

    [Serializable]
    public sealed class HideScreenOverlayStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private ScreenOverlayHideRequestData screenOverlayHideRequestData;

        public override void Validate(ActionValidationContext context)
        {
            if (!screenOverlayHideRequestData.displayId.IsValid)
                context.AddError("Screen overlay display id is required.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new HideScreenOverlayStepRuntime(screenOverlayHideRequestData));
        }
    }
}
