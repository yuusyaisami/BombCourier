using System.Collections.Generic;
using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.PressurePlate
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(PressurePlateSurfaceMB))]
    public sealed class PressurePlateMB : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private PressurePlateSurfaceMB surface;
        [SerializeField] private bool autoSetTrigger = true;

        [Header("Signals")]
        [SerializeField] private bool publishKernelSignals = true;

        [ShowIf(nameof(publishKernelSignals))]
        [SerializeField, SignalDropdown("Gimmick.PressurePlate")]
        private KernelSignalReference pressedSignal =
            KernelSignalReference.From(Signals.Gimmick.PressurePlate.Pressed);

        [ShowIf(nameof(publishKernelSignals))]
        [SerializeField, SignalDropdown("Gimmick.PressurePlate")]
        private KernelSignalReference releasedSignal =
            KernelSignalReference.From(Signals.Gimmick.PressurePlate.Released);

        [Header("Actions")]
        [SerializeField] private WiringAction[] onPressedActions;
        [SerializeField] private WiringAction[] onReleasedActions;

        [InlineProperty]
        [HideLabel]
        [SerializeField]
        private WiringSequenceDefinition sequence;

        private readonly Dictionary<EntityRef, Occupant> occupants = new();

        private Collider triggerCollider;
        private SceneKernel sceneKernel;
        private EntityMB selfEntityMB;
        private WiringSequenceRuntime sequenceRuntime;
        private bool isPressed;

        public bool IsPressed => isPressed;
        public int OccupantCount => occupants.Count;

        private void Reset()
        {
            surface = GetComponent<PressurePlateSurfaceMB>();
            triggerCollider = GetComponent<Collider>();

            if (triggerCollider != null)
                triggerCollider.isTrigger = true;
        }

        private void Awake()
        {
            surface = surface != null ? surface : GetComponent<PressurePlateSurfaceMB>();
            triggerCollider = GetComponent<Collider>();

            if (autoSetTrigger && triggerCollider != null)
                triggerCollider.isTrigger = true;

            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

            if (kernelMB == null || kernelMB.Kernel == null)
            {
                Debug.LogError($"{nameof(PressurePlateMB)}: SceneKernelMB was not found.", this);
                enabled = false;
                return;
            }

            sceneKernel = kernelMB.Kernel;
            selfEntityMB = GetComponentInParent<EntityMB>();
            sequenceRuntime = new WiringSequenceRuntime(sequence);
        }

        private void OnDisable()
        {
            occupants.Clear();
            isPressed = false;
            sequenceRuntime?.Reset();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!TryBuildContact(other, out PressurePlateContactData contactData, out EntityMB triggerEntityMB))
                return;

            if (surface != null && !surface.TryEvaluate(contactData))
                return;

            bool wasPressed = isPressed;

            if (occupants.TryGetValue(contactData.SourceEntity, out Occupant occupant))
            {
                occupant.ContactCount++;
                occupants[contactData.SourceEntity] = occupant;
            }
            else
            {
                occupants.Add(contactData.SourceEntity, new Occupant(contactData, 1));
            }

            if (!wasPressed && occupants.Count > 0)
                SetPressed(true, contactData, triggerEntityMB);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryBuildContact(other, out PressurePlateContactData contactData, out EntityMB triggerEntityMB))
                return;

            if (!occupants.TryGetValue(contactData.SourceEntity, out Occupant occupant))
                return;

            occupant.ContactCount--;

            if (occupant.ContactCount > 0)
            {
                occupants[contactData.SourceEntity] = occupant;
                return;
            }

            occupants.Remove(contactData.SourceEntity);

            if (isPressed && occupants.Count == 0)
                SetPressed(false, contactData, triggerEntityMB);
        }

        private bool TryBuildContact(Collider sourceCollider, out PressurePlateContactData contactData, out EntityMB entityMB)
        {
            contactData = default;
            entityMB = null;

            if (!WiringEntityUtility.TryGetEntity(sourceCollider, out entityMB))
                return false;

            Transform sourceRoot = entityMB.transform;
            contactData = new PressurePlateContactData(
                sourceCollider,
                sourceRoot.gameObject,
                sourceRoot,
                entityMB.Entity,
                entityMB.Tag);
            return true;
        }

        private void SetPressed(bool pressed, in PressurePlateContactData contactData, EntityMB triggerEntityMB)
        {
            isPressed = pressed;
            WiringActionContext context = BuildContext(contactData, triggerEntityMB);

            if (publishKernelSignals && sceneKernel != null)
            {
                sceneKernel.KernelEvents.RaiseSignal(pressed ? pressedSignal : releasedSignal);
            }

            if (pressed)
            {
                WiringActionRunner.ExecuteAll(onPressedActions, context);
                sequenceRuntime?.TryEnterNext(context, out _);
            }
            else
            {
                sequenceRuntime?.TryExitActive(context);
                WiringActionRunner.ExecuteAll(onReleasedActions, context);
            }
        }

        private WiringActionContext BuildContext(in PressurePlateContactData contactData, EntityMB triggerEntityMB)
        {
            EntityRef selfEntity = selfEntityMB != null && selfEntityMB.HasEntity ? selfEntityMB.Entity : default;
            EntityTagId selfTag = selfEntityMB != null ? selfEntityMB.Tag : default;
            EntityRef triggerEntity = triggerEntityMB != null && triggerEntityMB.HasEntity ? triggerEntityMB.Entity : default;
            EntityTagId triggerTag = triggerEntityMB != null ? triggerEntityMB.Tag : default;

            return new WiringActionContext(
                sceneKernel,
                gameObject,
                transform,
                selfEntity,
                selfTag,
                contactData.SourceObject,
                contactData.SourceRoot,
                triggerEntity,
                triggerTag);
        }

        private struct Occupant
        {
            public readonly EntityRef Entity;
            public readonly EntityTagId Tag;
            public readonly GameObject GameObject;
            public readonly Transform Transform;
            public int ContactCount;

            public Occupant(in PressurePlateContactData contactData, int contactCount)
            {
                Entity = contactData.SourceEntity;
                Tag = contactData.SourceTag;
                GameObject = contactData.SourceObject;
                Transform = contactData.SourceRoot;
                ContactCount = contactCount;
            }
        }
    }
}