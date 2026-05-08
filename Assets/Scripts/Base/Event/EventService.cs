using System;
using System.Collections.Generic;

namespace BC.Base
{
    public sealed class EventService : IGameEventBus, IEntityEventService
    {
        private readonly Dictionary<Type, object> globalHandlersByType = new();
        private readonly Dictionary<EntityEventKey, object> entityHandlersByKey = new();

        public EventSubscription Subscribe<TEvent>(Action<TEvent> handler)
            where TEvent : struct, IGameEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Type type = typeof(TEvent);

            if (!globalHandlersByType.TryGetValue(type, out object listObject))
            {
                listObject = new List<Action<TEvent>>();
                globalHandlersByType.Add(type, listObject);
            }

            var list = (List<Action<TEvent>>)listObject;
            list.Add(handler);

            return new EventSubscription(() =>
            {
                list.Remove(handler);

                if (list.Count == 0)
                    globalHandlersByType.Remove(type);
            });
        }

        public void Publish<TEvent>(in TEvent gameEvent)
            where TEvent : struct, IGameEvent
        {
            Type type = typeof(TEvent);

            if (!globalHandlersByType.TryGetValue(type, out object listObject))
                return;

            var list = (List<Action<TEvent>>)listObject;

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Invoke(gameEvent);
            }
        }

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
    }
}