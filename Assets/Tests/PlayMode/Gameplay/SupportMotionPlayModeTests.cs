using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class SupportMotionPlayModeTests
    {
        private const string SupportMotionUtilityTypeName = "BC.Base.SupportMotionUtility";
        private const string EntityTypeName = "BC.Base.EntityMB";
        private const string RigidbodySupportRiderTypeName = "BC.Base.RigidbodySupportRiderMB";
        private const string KinematicSupportMotionSourceTypeName = "BC.Base.KinematicSupportMotionSourceMB";

        private readonly List<UnityEngine.Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [Test]
        public void SupportMotionFromDeltaReportsPassengerRotationDeltaAndVelocity()
        {
            GameObject source = new GameObject("SupportMotionSource");
            createdObjects.Add(source);

            Vector3 passengerPoint = new Vector3(2f, 0f, 0f);
            Quaternion rotationDelta = Quaternion.Euler(0f, 90f, 0f);
            object snapshot = InvokeStaticMethod(
                SupportMotionUtilityTypeName,
                "FromDelta",
                source.transform,
                null,
                Vector3.zero,
                passengerPoint,
                Vector3.zero,
                rotationDelta,
                0.5f);

            Vector3 expectedDelta = rotationDelta * passengerPoint - passengerPoint;
            AssertVectorApproximately(expectedDelta, GetFieldValue<Vector3>(snapshot, "PassengerDelta"));
            AssertVectorApproximately(expectedDelta / 0.5f, GetFieldValue<Vector3>(snapshot, "PassengerVelocity"));
            Assert.Greater(GetFieldValue<Vector3>(snapshot, "SourceAngularVelocity").magnitude, 0.1f);
        }

        [UnityTest]
        public IEnumerator EntityAutoInstallsRigidbodySupportRiderForRigidbodyOnlyEntity()
        {
            GameObject root = new GameObject("RigidbodyOnlyEntity");
            root.SetActive(false);
            createdObjects.Add(root);

            root.AddComponent<BoxCollider>();
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;
            root.AddComponent(FindRuntimeType(EntityTypeName));

            root.SetActive(true);
            yield return null;

            Assert.IsNotNull(root.GetComponent(FindRuntimeType(RigidbodySupportRiderTypeName)));
        }

        [UnityTest]
        public IEnumerator EntityAutoInstallSkipsExplicitSupportMotionSources()
        {
            GameObject root = new GameObject("SupportSourceEntity");
            root.SetActive(false);
            createdObjects.Add(root);

            root.AddComponent<BoxCollider>();
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            root.AddComponent(FindRuntimeType(KinematicSupportMotionSourceTypeName));
            root.AddComponent(FindRuntimeType(EntityTypeName));

            root.SetActive(true);
            yield return null;

            Assert.IsNull(root.GetComponent(FindRuntimeType(RigidbodySupportRiderTypeName)));
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

        private static object InvokeStaticMethod(string fullTypeName, string methodName, params object[] arguments)
        {
            Type type = FindRuntimeType(fullTypeName);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method: {fullTypeName}.{methodName}");
            return method.Invoke(null, arguments);
        }

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field: {target.GetType().Name}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }
    }
}