using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
	[DisallowMultipleComponent]
	public sealed class ActionRunnerMB : MonoBehaviour
	{
		[SerializeField] private InlineAction action;
		[SerializeField] private SceneKernelMB sceneKernelSource;
		[SerializeField] private EntityMB selfEntitySource;

		private CompiledAction compiledAction;
		private bool compiled;

		public InlineAction Action => action;

		private void Reset()
		{
			ResolveAuthoringReferences();
		}

		private void Awake()
		{
			ResolveAuthoringReferences();
			EnsureCompiled();
		}

		private void OnValidate()
		{
			if (!Application.isPlaying)
				ResolveAuthoringReferences();
		}

		public bool Execute()
		{
			return Execute(default);
		}

		public bool Execute(EntityRef triggerEntity)
		{
			if (!EnsureCompiled())
				return false;

			SceneKernel sceneKernel = ResolveSceneKernel();

			if (sceneKernel == null)
				return false;

			EntityRef selfEntity = selfEntitySource != null && selfEntitySource.HasEntity
				? selfEntitySource.Entity
				: default;

			ActionExecutionContext context = new(sceneKernel, selfEntity, triggerEntity);
			return compiledAction.Execute(context);
		}

		public bool TryCompile(out IReadOnlyList<string> errors)
		{
			ActionValidationContext validationContext = new();
			action?.Validate(validationContext);
			errors = validationContext.Errors;

			if (!validationContext.IsValid)
			{
				compiledAction = null;
				compiled = false;
				return false;
			}

			compiledAction = action != null
				? action.Compile()
				: new ActionCompileContext().Build();
			compiled = true;
			return true;
		}

		private bool EnsureCompiled()
		{
			if (compiled)
				return compiledAction != null;

			return TryCompile(out _);
		}

		private SceneKernel ResolveSceneKernel()
		{
			if (sceneKernelSource == null)
				sceneKernelSource = GetComponentInParent<SceneKernelMB>();

			return sceneKernelSource != null ? sceneKernelSource.Kernel : null;
		}

		private void ResolveAuthoringReferences()
		{
			if (sceneKernelSource == null)
				sceneKernelSource = GetComponentInParent<SceneKernelMB>();

			if (selfEntitySource == null)
				selfEntitySource = GetComponentInParent<EntityMB>();
		}
	}
}
