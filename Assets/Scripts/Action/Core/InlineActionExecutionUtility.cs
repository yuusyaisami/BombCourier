using System;
using System.Threading;
using BC.Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.ActionSystem
{
    public static class InlineActionExecutionUtility
    {
        public static UniTask<ActionExecutionResult> ExecuteAsync(
            Component contextComponent,
            EntityRef actor,
            InlineAction inlineAction,
            EntityRef triggerEntity = default,
            CancellationToken cancellationToken = default)
        {
            if (inlineAction == null)
            {
                return UniTask.FromResult(ActionExecutionResult.Completed());
            }

            if (!actor.IsValid)
            {
                return UniTask.FromResult(ActionExecutionResult.Failed("Actor entity is invalid."));
            }

            if (!TryResolveSceneKernel(contextComponent, out SceneKernel sceneKernel) || sceneKernel.Actions == null)
            {
                return UniTask.FromResult(ActionExecutionResult.Failed("SceneKernel.Actions is not available."));
            }

            return sceneKernel.Actions.ExecuteAsync(actor, inlineAction.Compile(), triggerEntity, cancellationToken);
        }

        public static void ExecuteAndForget(
            Component contextComponent,
            EntityRef actor,
            InlineAction inlineAction,
            EntityRef triggerEntity = default,
            string logContext = null)
        {
            ExecuteAndForgetInternal(contextComponent, actor, inlineAction, triggerEntity, logContext).Forget();
        }

        public static UniTask<ActionExecutionResult> ExecuteDetachedAsync(
            Component contextComponent,
            EntityRef actor,
            InlineAction inlineAction,
            EntityRef triggerEntity = default,
            CancellationToken cancellationToken = default)
        {
            if (inlineAction == null)
            {
                return UniTask.FromResult(ActionExecutionResult.Completed());
            }

            if (!actor.IsValid)
            {
                return UniTask.FromResult(ActionExecutionResult.Failed("Actor entity is invalid."));
            }

            if (!TryResolveSceneKernel(contextComponent, out SceneKernel sceneKernel) || sceneKernel.Actions == null)
            {
                return UniTask.FromResult(ActionExecutionResult.Failed("SceneKernel.Actions is not available."));
            }

            return sceneKernel.Actions.ExecuteDetachedAsync(actor, inlineAction.Compile(), triggerEntity, cancellationToken);
        }

        public static void ExecuteDetachedAndForget(
            Component contextComponent,
            EntityRef actor,
            InlineAction inlineAction,
            EntityRef triggerEntity = default,
            string logContext = null)
        {
            ExecuteDetachedAndForgetInternal(contextComponent, actor, inlineAction, triggerEntity, logContext).Forget();
        }

        public static bool TryResolveSceneKernel(Component contextComponent, out SceneKernel sceneKernel)
        {
            sceneKernel = null;
            if (contextComponent == null)
                return false;

            SceneKernelMB kernelMB = contextComponent.GetComponentInParent<SceneKernelMB>();
            if (kernelMB == null)
                return false;

            sceneKernel = kernelMB.Kernel;
            return sceneKernel != null;
        }

        private static async UniTaskVoid ExecuteAndForgetInternal(
            Component contextComponent,
            EntityRef actor,
            InlineAction inlineAction,
            EntityRef triggerEntity,
            string logContext)
        {
            try
            {
                ActionExecutionResult result = await ExecuteAsync(contextComponent, actor, inlineAction, triggerEntity);

                if (result.IsFailed)
                {
                    Debug.LogWarning($"{nameof(InlineActionExecutionUtility)}: {logContext ?? "InlineAction"} failed. {result.Message}", contextComponent);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, contextComponent);
            }
        }

        private static async UniTaskVoid ExecuteDetachedAndForgetInternal(
            Component contextComponent,
            EntityRef actor,
            InlineAction inlineAction,
            EntityRef triggerEntity,
            string logContext)
        {
            try
            {
                ActionExecutionResult result = await ExecuteDetachedAsync(contextComponent, actor, inlineAction, triggerEntity);

                if (result.IsFailed)
                {
                    Debug.LogWarning($"{nameof(InlineActionExecutionUtility)}: {logContext ?? "InlineAction"} failed. {result.Message}", contextComponent);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, contextComponent);
            }
        }
    }
}
