using System;
using System.Collections;
using System.Collections.Generic;

namespace BC.Base
{
    public sealed class KernelEventService : IGameEventBus
    {
        private readonly Dictionary<Type, object> handlersByType = new();

        public EventSubscription Subscribe<TEvent>(Action<TEvent> handler)
            where TEvent : struct, IKernelEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Type type = typeof(TEvent);

            if (!handlersByType.TryGetValue(type, out object listObject))
            {
                listObject = new List<Action<TEvent>>();
                handlersByType.Add(type, listObject);
            }

            var list = (List<Action<TEvent>>)listObject;
            list.Add(handler);

            return new EventSubscription(() =>
            {
                list.Remove(handler);

                if (list.Count == 0)
                    handlersByType.Remove(type);
            });
        }

        public void Publish<TEvent>(in TEvent kernelEvent)
            where TEvent : struct, IKernelEvent
        {
            Type type = typeof(TEvent);

            if (!handlersByType.TryGetValue(type, out object listObject))
                return;

            var list = (List<Action<TEvent>>)listObject;

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Invoke(kernelEvent);
            }
        }

        public void Clear()
        {
            foreach (object listObject in handlersByType.Values)
            {
                if (listObject is IList list)
                    list.Clear();
            }

            handlersByType.Clear();
        }
    }

    public sealed class EntityEventService : IEntityEventService
    {
        private readonly Dictionary<EntityEventKey, object> entityHandlersByKey = new();

        public EntityEventHandle For(EntityRef entity)
        {
            return new EntityEventHandle(entity, this);
        }

        public EventSubscription Subscribe<TEvent>(EntityRef entity, Action<TEvent> handler)
            where TEvent : struct, IEntityEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var key = new EntityEventKey(entity, typeof(TEvent));

            if (!entityHandlersByKey.TryGetValue(key, out object listObject))
            {
                listObject = new List<Action<TEvent>>();
                entityHandlersByKey.Add(key, listObject);
            }

            var list = (List<Action<TEvent>>)listObject;
            list.Add(handler);

            return new EventSubscription(() =>
            {
                list.Remove(handler);

                if (list.Count == 0)
                    entityHandlersByKey.Remove(key);
            });
        }

        public void Publish<TEvent>(EntityRef entity, in TEvent entityEvent)
            where TEvent : struct, IEntityEvent
        {
            var key = new EntityEventKey(entity, typeof(TEvent));

            if (!entityHandlersByKey.TryGetValue(key, out object listObject))
                return;

            var list = (List<Action<TEvent>>)listObject;

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Invoke(entityEvent);
            }
        }

        public void ClearEntity(EntityRef entity)
        {
            // 短期制作なら単純走査でよい。
            // 大量Entity対応するなら entity -> keys の逆引きを持つ。
            var removeKeys = new List<EntityEventKey>();

            foreach (var pair in entityHandlersByKey)
            {
                if (pair.Key.Entity.Equals(entity))
                    removeKeys.Add(pair.Key);
            }

            for (int i = 0; i < removeKeys.Count; i++)
            {
                entityHandlersByKey.Remove(removeKeys[i]);
            }
        }

        public void Clear()
        {
            foreach (object listObject in entityHandlersByKey.Values)
            {
                if (listObject is IList list)
                    list.Clear();
            }

            entityHandlersByKey.Clear();
        }
    }

    public sealed class EventService : IGameEventBus, IEntityEventService
    {
        public KernelEventService KernelEvents { get; } = new KernelEventService();
        public EntityEventService EntityEvents { get; } = new EntityEventService();

        public EventSubscription Subscribe<TEvent>(Action<TEvent> handler)
            where TEvent : struct, IKernelEvent
        {
            return KernelEvents.Subscribe(handler);
        }

        public void Publish<TEvent>(in TEvent kernelEvent)
            where TEvent : struct, IKernelEvent
        {
            KernelEvents.Publish(kernelEvent);
        }

        public EntityEventHandle For(EntityRef entity)
        {
            return EntityEvents.For(entity);
        }

        public EventSubscription Subscribe<TEvent>(EntityRef entity, Action<TEvent> handler)
            where TEvent : struct, IEntityEvent
        {
            return EntityEvents.Subscribe(entity, handler);
        }

        public void Publish<TEvent>(EntityRef entity, in TEvent entityEvent)
            where TEvent : struct, IEntityEvent
        {
            EntityEvents.Publish(entity, entityEvent);
        }

        public void ClearEntity(EntityRef entity)
        {
            EntityEvents.ClearEntity(entity);
        }

        public void Clear()
        {
            KernelEvents.Clear();
            EntityEvents.Clear();
        }
    }
}