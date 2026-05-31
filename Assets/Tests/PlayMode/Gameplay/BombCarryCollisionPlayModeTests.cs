using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class BombCarryCollisionPlayModeTests
    {
        private const string BombTypeName = "BC.Bomb.BombMB";

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
        public IEnumerator HeldBomb_DoesNotExplodeWhileColliderIsDisabled()
        {
            Component bomb = CreateBomb(Vector3.zero);
            GameObject handleRoot = CreateObject("HandleRoot");
            Transform handlePoint = CreateObject("HandlePoint").transform;
            handlePoint.SetParent(handleRoot.transform, false);

            GameObject obstacle = CreateObject("Obstacle");
            obstacle.transform.position = new Vector3(1f, 0f, 0f);
            BoxCollider obstacleCollider = obstacle.AddComponent<BoxCollider>();
            obstacleCollider.size = new Vector3(2f, 2f, 2f);

            InvokeMethod(bomb, "OnHandle", handlePoint);

            Assert.IsFalse(((Component)bomb).GetComponent<Collider>().enabled, "Held bomb collider should be disabled during the test.");

            handleRoot.transform.position = new Vector3(1f, 0f, 0f);
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(GetPropertyValue<bool>(bomb, "HasExploded"), "Held bomb must not explode from the overlap probe while its collider is disabled.");
        }

        private Component CreateBomb(Vector3 position)
        {
            GameObject bombObject = CreateObject("BombUnderTest");
            bombObject.transform.position = position;

            SphereCollider collider = bombObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            Rigidbody rigidbody = bombObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            Component bomb = bombObject.AddComponent(FindRuntimeType(BombTypeName));
            SetPrivateField(bomb, "startFuseOnHandle", false);
            SetPrivateField(bomb, "impactExplosionGraceTime", 0f);
            SetPrivateField(bomb, "heldImpactExplosionSpeed", 0f);
            return bomb;
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
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
    }
}
