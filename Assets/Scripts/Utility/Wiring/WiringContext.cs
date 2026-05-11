using UnityEngine;

namespace BC.Base
{
    public readonly struct WiringActionContext
    {
        public readonly SceneKernel SceneKernel;
        public readonly GameObject SelfObject;
        public readonly Transform SelfTransform;
        public readonly EntityRef SelfEntity;
        public readonly EntityTagId SelfTag;
        public readonly GameObject TriggerObject;
        public readonly Transform TriggerTransform;
        public readonly EntityRef TriggerEntity;
        public readonly EntityTagId TriggerTag;

        public WiringActionContext(
            SceneKernel sceneKernel,
            GameObject selfObject,
            Transform selfTransform,
            EntityRef selfEntity,
            EntityTagId selfTag,
            GameObject triggerObject,
            Transform triggerTransform,
            EntityRef triggerEntity,
            EntityTagId triggerTag)
        {
            SceneKernel = sceneKernel;
            SelfObject = selfObject;
            SelfTransform = selfTransform;
            SelfEntity = selfEntity;
            SelfTag = selfTag;
            TriggerObject = triggerObject;
            TriggerTransform = triggerTransform;
            TriggerEntity = triggerEntity;
            TriggerTag = triggerTag;
        }
    }

    public static class WiringEntityUtility
    {
        public static bool TryGetEntity(Component component, out EntityMB entityMB)
        {
            entityMB = null;

            if (component == null)
                return false;

            entityMB = component.GetComponentInParent<EntityMB>();
            return entityMB != null && entityMB.HasEntity;
        }

        public static bool TryGetEntity(GameObject gameObject, out EntityMB entityMB)
        {
            entityMB = null;

            if (gameObject == null)
                return false;

            entityMB = gameObject.GetComponentInParent<EntityMB>();
            return entityMB != null && entityMB.HasEntity;
        }
    }
}