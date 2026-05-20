using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class ChainExplosivePlayModeTests
    {
        private const string BombTypeName = "BC.Bomb.BombMB";
        private const string ChainExplosiveTypeName = "BC.Bomb.ChainExplosiveMB";

        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                GameObject createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator BombExplosion_StartsChainFuse_AndChainExplosionPropagates()
        {
            Component bomb = CreateBomb(new Vector3(0f, 0f, 0f), fuseTime: 0.05f, explosionRadius: 2.1f, explosionForce: 24f);
            Component chain = CreateChainExplosive(new Vector3(1.2f, 0f, 0f), fuseTime: 0.2f, explosionRadius: 3.0f, explosionForce: 24f, triggerThreshold: 0.01f, canBeCarried: true);
            Component propagatedChain = CreateChainExplosive(new Vector3(3.4f, 0f, 0f), fuseTime: 0.2f, explosionRadius: 2.0f, explosionForce: 12f, triggerThreshold: 0.01f, canBeCarried: true);

            InvokeMethod(bomb, "BeginFuse");

            yield return WaitUntilOrTimeout(() => GetPropertyValue<bool>(chain, "FuseStarted"), 0.25f, "Chain explosive did not start its fuse from the bomb explosion.");

            Assert.IsFalse(GetPropertyValue<bool>(propagatedChain, "FuseStarted"), "Second chain explosive must stay idle until the first chain explosion happens.");

            yield return new WaitForSeconds(0.1f);

            Assert.IsFalse(GetPropertyValue<bool>(propagatedChain, "FuseStarted"), "Second chain explosive should still be waiting while the first chain fuse is burning.");

            yield return WaitUntilOrTimeout(() => GetPropertyValue<bool>(propagatedChain, "FuseStarted"), 0.5f, "Chain explosive did not propagate its explosion to a second chain explosive.");
        }

        [UnityTest]
        public IEnumerator ChainExplosionTrigger_DoesNotRestartFuse_WhenHitAgain()
        {
            Component chain = CreateChainExplosive(new Vector3(0f, 0f, 0f), fuseTime: 0.2f, explosionRadius: 2.0f, explosionForce: 12f, triggerThreshold: 0.01f, canBeCarried: true);

            InvokeMethod(chain, "OnExplosionImpact", Vector3.right, 5f);
            Assert.IsTrue(GetPropertyValue<bool>(chain, "FuseStarted"), "First explosion impact should start the fuse.");

            yield return new WaitForSeconds(0.08f);

            float remainingBeforeSecondImpact = GetPropertyValue<float>(chain, "RemainingFuseTime");
            InvokeMethod(chain, "OnExplosionImpact", Vector3.left, 5f);

            yield return null;

            Assert.Less(
                GetPropertyValue<float>(chain, "RemainingFuseTime"),
                remainingBeforeSecondImpact + 0.02f,
                "Repeated explosion impacts must not reset the active fuse.");
        }

        [Test]
        public void ChainExplosion_CanDisableCarryViaAuthoringFlag()
        {
            Component chain = CreateChainExplosive(new Vector3(0f, 0f, 0f), fuseTime: 0.2f, explosionRadius: 2.0f, explosionForce: 12f, triggerThreshold: 0.01f, canBeCarried: false);
            Assert.IsFalse(GetPropertyValue<bool>(chain, "CanBeCarried"), "carry authoring flag should disable pickup eligibility.");
        }

        private Component CreateBomb(Vector3 position, float fuseTime, float explosionRadius, float explosionForce)
        {
            GameObject bombObject = CreatePhysicsObject("BombUnderTest", position);
            Component bomb = bombObject.AddComponent(FindRuntimeType(BombTypeName));
            SetPrivateField(bomb, "fuseTime", fuseTime);
            SetPrivateField(bomb, "startFuseOnHandle", false);
            SetPrivateField(bomb, "explosionRadius", explosionRadius);
            SetPrivateField(bomb, "explosionForce", explosionForce);
            SetPrivateField<ParticleSystem>(bomb, "explosionEffectPrefab", null);
            SetPrivateField<ParticleSystem>(bomb, "startFuseEffect", null);
            return bomb;
        }

        private Component CreateChainExplosive(Vector3 position, float fuseTime, float explosionRadius, float explosionForce, float triggerThreshold, bool canBeCarried)
        {
            GameObject explosiveObject = CreatePhysicsObject("ChainExplosiveUnderTest", position);
            Component chainExplosive = explosiveObject.AddComponent(FindRuntimeType(ChainExplosiveTypeName));
            SetPrivateField(chainExplosive, "canBeCarried", canBeCarried);
            SetPrivateField(chainExplosive, "fuseTime", fuseTime);
            SetPrivateField(chainExplosive, "triggerExplosionThreshold", triggerThreshold);
            SetPrivateField(chainExplosive, "explosionRadius", explosionRadius);
            SetPrivateField(chainExplosive, "explosionForce", explosionForce);
            SetPrivateField<ParticleSystem>(chainExplosive, "explosionEffectPrefab", null);
            SetPrivateField<ParticleSystem>(chainExplosive, "startFuseEffect", null);
            return chainExplosive;
        }

        private GameObject CreatePhysicsObject(string name, Vector3 position)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = position;

            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static Type FindRuntimeType(string fullTypeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullTypeName);
                if (type != null)
                    return type;
            }

            Assert.Fail($"Expected runtime type to exist: {fullTypeName}");
            return null;
        }

        private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }

        private static IEnumerator WaitUntilOrTimeout(Func<bool> condition, float timeoutSeconds, string timeoutMessage)
        {
            float elapsed = 0f;

            while (elapsed < timeoutSeconds)
            {
                if (condition())
                    yield break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Fail(timeoutMessage);
        }
    }
}