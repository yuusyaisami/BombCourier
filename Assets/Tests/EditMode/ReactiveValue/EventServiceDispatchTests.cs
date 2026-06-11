using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace BC.Base.Tests
{
    // この asmdef から runtime assembly を直接参照できない環境でも契約を固定できるよう、
    // public API は reflection 経由で呼び出す。検証対象は「型名」ではなく publish 中の購読変更 policy。
    public sealed class EventServiceDispatchTests
    {
        private const string KernelEventServiceTypeName = "BC.Base.KernelEventService";
        private const string EntityEventServiceTypeName = "BC.Base.EntityEventService";
        private const string EntityRefTypeName = "BC.Base.EntityRef";
        private const string KernelEventTypeName = "BC.Base.EntityUnregisteredKernelEvent";
        private const string EntityEventTypeName = "BC.Base.EntityUnregisteredEvent";

        [Test]
        public void KernelEventPublishUsesStableSubscriptionSnapshot()
        {
            object service = Activator.CreateInstance(GetTypeByFullName(KernelEventServiceTypeName));
            Type eventType = GetTypeByFullName(KernelEventTypeName);
            object eventValue = Activator.CreateInstance(eventType);
            object selfSubscription = null;
            int selfCalls = 0;
            int secondCalls = 0;

            selfSubscription = SubscribeKernel(service, eventType, CreateEventHandler(eventType, () =>
            {
                selfCalls++;
                DisposeSubscription(selfSubscription);
            }));
            SubscribeKernel(service, eventType, CreateEventHandler(eventType, () => secondCalls++));

            PublishKernel(service, eventType, eventValue);
            PublishKernel(service, eventType, eventValue);

            Assert.AreEqual(1, selfCalls);
            Assert.AreEqual(2, secondCalls);
        }

        [Test]
        public void KernelEventPublishSkipsRemovedHandlerAndDefersNewHandler()
        {
            object service = Activator.CreateInstance(GetTypeByFullName(KernelEventServiceTypeName));
            Type eventType = GetTypeByFullName(KernelEventTypeName);
            object eventValue = Activator.CreateInstance(eventType);
            object removedSubscription = null;
            bool addedSubscribed = false;
            int firstCalls = 0;
            int removedCalls = 0;
            int addedCalls = 0;

            SubscribeKernel(service, eventType, CreateEventHandler(eventType, () =>
            {
                firstCalls++;
                DisposeSubscription(removedSubscription);

                if (!addedSubscribed)
                {
                    addedSubscribed = true;
                    SubscribeKernel(service, eventType, CreateEventHandler(eventType, () => addedCalls++));
                }
            }));
            removedSubscription = SubscribeKernel(service, eventType, CreateEventHandler(eventType, () => removedCalls++));

            PublishKernel(service, eventType, eventValue);
            PublishKernel(service, eventType, eventValue);

            Assert.AreEqual(2, firstCalls);
            Assert.AreEqual(0, removedCalls);
            Assert.AreEqual(1, addedCalls);
        }

        [Test]
        public void EntityEventPublishUsesSameSubscriptionMutationPolicy()
        {
            object service = Activator.CreateInstance(GetTypeByFullName(EntityEventServiceTypeName));
            Type eventType = GetTypeByFullName(EntityEventTypeName);
            object entity = CreateEntityRef(10u, 1);
            object eventValue = Activator.CreateInstance(eventType);
            object removedSubscription = null;
            bool addedSubscribed = false;
            int firstCalls = 0;
            int removedCalls = 0;
            int addedCalls = 0;

            SubscribeEntity(service, eventType, entity, CreateEventHandler(eventType, () =>
            {
                firstCalls++;
                DisposeSubscription(removedSubscription);

                if (!addedSubscribed)
                {
                    addedSubscribed = true;
                    SubscribeEntity(service, eventType, entity, CreateEventHandler(eventType, () => addedCalls++));
                }
            }));
            removedSubscription = SubscribeEntity(service, eventType, entity, CreateEventHandler(eventType, () => removedCalls++));

            PublishEntity(service, eventType, entity, eventValue);
            PublishEntity(service, eventType, entity, eventValue);

            Assert.AreEqual(2, firstCalls);
            Assert.AreEqual(0, removedCalls);
            Assert.AreEqual(1, addedCalls);
        }

        private static object SubscribeKernel(object service, Type eventType, object handler)
        {
            MethodInfo method = service.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(candidate => candidate.Name == "Subscribe" &&
                                    candidate.IsGenericMethodDefinition &&
                                    candidate.GetParameters().Length == 1)
                .MakeGenericMethod(eventType);
            return method.Invoke(service, new[] { handler });
        }

        private static void PublishKernel(object service, Type eventType, object eventValue)
        {
            MethodInfo method = service.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(candidate => candidate.Name == "Publish" &&
                                    candidate.IsGenericMethodDefinition &&
                                    candidate.GetParameters().Length == 1)
                .MakeGenericMethod(eventType);
            method.Invoke(service, new[] { eventValue });
        }

        private static object SubscribeEntity(object service, Type eventType, object entity, object handler)
        {
            MethodInfo method = service.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(candidate => candidate.Name == "Subscribe" &&
                                    candidate.IsGenericMethodDefinition &&
                                    candidate.GetParameters().Length == 2)
                .MakeGenericMethod(eventType);
            return method.Invoke(service, new[] { entity, handler });
        }

        private static void PublishEntity(object service, Type eventType, object entity, object eventValue)
        {
            MethodInfo method = service.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(candidate => candidate.Name == "Publish" &&
                                    candidate.IsGenericMethodDefinition &&
                                    candidate.GetParameters().Length == 2)
                .MakeGenericMethod(eventType);
            method.Invoke(service, new[] { entity, eventValue });
        }

        private static object CreateEventHandler(Type eventType, Action callback)
        {
            MethodInfo factory = typeof(EventServiceDispatchTests)
                .GetMethod(nameof(CreateTypedEventHandler), BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(eventType);
            Assert.IsNotNull(factory, "Expected typed event handler factory.");
            return factory.Invoke(null, new object[] { callback });
        }

        private static Action<TEvent> CreateTypedEventHandler<TEvent>(Action callback)
        {
            return _ => callback();
        }

        private static void DisposeSubscription(object subscription)
        {
            if (subscription == null)
                return;

            MethodInfo dispose = subscription.GetType().GetMethod("Dispose", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(dispose, "Expected event subscription to expose Dispose.");
            dispose.Invoke(subscription, null);
        }

        private static object CreateEntityRef(uint entityId, int version)
        {
            return Activator.CreateInstance(GetTypeByFullName(EntityRefTypeName), entityId, version);
        }

        private static Type GetTypeByFullName(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            Assert.Fail($"Expected type: {fullName}");
            return null;
        }
    }
}
