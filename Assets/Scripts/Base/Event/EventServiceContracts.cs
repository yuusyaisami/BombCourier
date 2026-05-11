using System;
namespace BC.Base
{
    public interface IKernelEvent
    {
    }

    // 旧名互換。新規コードでは IKernelEvent を使う。
    public interface IGameEvent : IKernelEvent
    {
    }

    public interface IEntityEvent
    {
    }
    public sealed class EventSubscription : IDisposable
    {
        private Action disposeAction;

        public bool IsDisposed => disposeAction == null;

        public EventSubscription(Action disposeAction)
        {
            this.disposeAction = disposeAction;
        }

        public void Dispose()
        {
            var action = disposeAction;
            if (action == null)
                return;

            disposeAction = null;
            action.Invoke();
        }
    }
    public readonly struct EntityEventHandle
    {
        private readonly EntityRef entity;
        private readonly IEntityEventService service;

        public EntityRef Entity => entity;

        public EntityEventHandle(EntityRef entity, IEntityEventService service)
        {
            this.entity = entity;
            this.service = service;
        }


        public EventSubscription Subscribe<TEvent>(Action<TEvent> handler)
            where TEvent : struct, IEntityEvent
        {
            return service.Subscribe(entity, handler);
        }

        public void Publish<TEvent>(in TEvent entityEvent)
            where TEvent : struct, IEntityEvent
        {
            service.Publish(entity, entityEvent);
        }
    }
    // Sceneならシーン全体、Applicationならアプリ全体のイベントを管理する。
    public interface IKernelEventBus
    {
        EventSubscription Subscribe<TEvent>(Action<TEvent> handler)
            where TEvent : struct, IKernelEvent;

        void Publish<TEvent>(in TEvent kernelEvent)
            where TEvent : struct, IKernelEvent;
    }

    // 旧名互換。新規コードでは IKernelEventBus を使う。
    public interface IGameEventBus : IKernelEventBus
    {
    }
    // Entityに関連するイベントを管理するサービスのインターフェース
    public interface IEntityEventService
    {
        EntityEventHandle For(EntityRef entity);

        EventSubscription Subscribe<TEvent>(EntityRef entity, Action<TEvent> handler)
            where TEvent : struct, IEntityEvent;

        void Publish<TEvent>(EntityRef entity, in TEvent entityEvent)
            where TEvent : struct, IEntityEvent;

        void ClearEntity(EntityRef entity);
    }

    public readonly struct EntityEventKey : IEquatable<EntityEventKey>
    {
        public readonly EntityRef Entity;
        public readonly Type EventType;

        public EntityEventKey(EntityRef entity, Type eventType)
        {
            Entity = entity;
            EventType = eventType;
        }

        public bool Equals(EntityEventKey other)
        {
            return Entity.Equals(other.Entity) && EventType == other.EventType;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityEventKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Entity, EventType);
        }
    }
}