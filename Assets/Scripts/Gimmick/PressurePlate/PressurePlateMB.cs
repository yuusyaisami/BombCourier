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
        [Tooltip("踏み判定に使うフィルター設定です。未指定なら同じオブジェクトから自動取得します。")]
        [SerializeField] private PressurePlateSurfaceMB surface;
        [Tooltip("起動時にコライダーを Trigger に自動設定するかを指定します。")]
        [SerializeField] private bool autoSetTrigger = true;

        [Header("Signals")]
        [Tooltip("押下と解放のタイミングで Kernel Signal を送るかを指定します。")]
        [SerializeField] private bool publishKernelSignals = true;

        [ShowIf(nameof(publishKernelSignals))]
        [Tooltip("感圧板が押された瞬間に送る Signal です。")]
        [SerializeField, SignalDropdown("Gimmick.PressurePlate")]
        private KernelSignalReference pressedSignal =
            KernelSignalReference.From(Signals.Gimmick.PressurePlate.Pressed);

        [ShowIf(nameof(publishKernelSignals))]
        [Tooltip("感圧板が解放された瞬間に送る Signal です。")]
        [SerializeField, SignalDropdown("Gimmick.PressurePlate")]
        private KernelSignalReference releasedSignal =
            KernelSignalReference.From(Signals.Gimmick.PressurePlate.Released);

        [Header("Actions")]
        [LabelText("Pressed Actions")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [Tooltip("感圧板が押された瞬間に実行する WiringAction の一覧です。")]
        [SerializeField]
        private WiringAction[] onPressedActions = System.Array.Empty<WiringAction>();

        [LabelText("Released Actions")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [Tooltip("感圧板が解放された瞬間に実行する WiringAction の一覧です。")]
        [SerializeField]
        private WiringAction[] onReleasedActions = System.Array.Empty<WiringAction>();

        [InlineProperty]
        [HideLabel]
        [Tooltip("押下中のシーケンス再生設定です。")]
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

        // 押下中のみ、破棄済みの占有者を掃除するための防御的スイープ。
        // 占有コライダーが破棄されたとき Unity は通常 OnTriggerExit を送るが、
        // 同フレーム破棄などで取りこぼすと occupants に死んだエントリが残り、
        // 感圧板が押されっぱなしで固着してギミック進行が詰む恐れがある。
        // 一時的な非アクティブを誤って解放しないよう、対象は「Destroy 済み(== null)」が
        // 確実な占有者のみに限定する。押下していない間は何もしない（常時コストを避ける）。
        private void Update()
        {
            if (!isPressed || occupants.Count == 0)
                return;

            PruneDestroyedOccupants();
        }

        private void PruneDestroyedOccupants()
        {
            // 列挙中に Dictionary を変更できないため、破棄済みキーを集めてから削除する。
            // 破棄が無い通常フレームではリストを確保しないようにして alloc を避ける。
            List<EntityRef> destroyedKeys = null;
            foreach (KeyValuePair<EntityRef, Occupant> pair in occupants)
            {
                if (pair.Value.GameObject == null) // Unity fake-null: GameObject が Destroy 済み
                {
                    destroyedKeys ??= new List<EntityRef>();
                    destroyedKeys.Add(pair.Key);
                }
            }

            if (destroyedKeys == null)
                return;

            for (int i = 0; i < destroyedKeys.Count; i++)
                occupants.Remove(destroyedKeys[i]);

            // 占有者が全て消えたら解放する。破棄起因のため発火元 entity 情報は既に無く、
            // default の contact 情報で解放通知を出す（onReleasedActions 側は entity 非依存で動く想定）。
            if (occupants.Count == 0)
                SetPressed(false, default, null);
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