using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class BreakableGateObjectPlayModeTests
    {
        private const string BreakableGateObjectTypeName = "BC.Gimmick.BreakableGateObjectMB";

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

        [Test]
        public void ImpactBelowThreshold_DoesNotBreakGate()
        {
            (Component gate, Collider gateCollider, Rigidbody partBody) = CreateBreakableGate("BelowThreshold", threshold: 10f, explosionForce: 5f);

            InvokeMethod(gate, "OnBombImpactReceived", Vector3.forward, 4f);

            Assert.IsTrue(gateCollider.enabled, "Gate collider should remain enabled when impact is below the threshold.");
            Assert.IsFalse(GetPropertyValue<bool>(gate, "IsBroken"), "Gate should remain unbroken when impact is below threshold.");
            Assert.IsTrue(partBody.isKinematic, "Breakable part should remain kinematic before the gate breaks.");
        }

        [Test]
        public void ImpactAboveThreshold_BreaksGateAndActivatesParts()
        {
            (Component gate, Collider gateCollider, Rigidbody partBody) = CreateBreakableGate("AboveThreshold", threshold: 5f, explosionForce: 12f);

            InvokeMethod(gate, "OnBombImpactReceived", Vector3.forward, 8f);

            Assert.IsFalse(gateCollider.enabled, "Gate collider should be disabled after the gate breaks.");
            Assert.IsTrue(GetPropertyValue<bool>(gate, "IsBroken"), "Gate should report broken state after threshold impact.");
            Assert.IsFalse(partBody.isKinematic, "Breakable part should become dynamic after the gate breaks.");
            Assert.IsTrue(partBody.useGravity, "Breakable part should enable gravity after the gate breaks.");
        }

        private (Component Gate, Collider GateCollider, Rigidbody PartBody) CreateBreakableGate(string name, float threshold, float explosionForce)
        {
            GameObject root = new GameObject(name);
            createdObjects.Add(root);

            BoxCollider gateCollider = root.AddComponent<BoxCollider>();
            GameObject part = new GameObject("Part");
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = Vector3.right;
            BoxCollider partCollider = part.AddComponent<BoxCollider>();
            Rigidbody partBody = part.AddComponent<Rigidbody>();
            partBody.isKinematic = true;
            partBody.useGravity = false;
            createdObjects.Add(part);

            Component gate = root.AddComponent(FindRuntimeType(BreakableGateObjectTypeName));
            SetPrivateField(gate, "breakableParts", new List<Rigidbody> { partBody });
            SetPrivateField(gate, "gateCollider", gateCollider);
            SetPrivateField(gate, "breakForceDirection", Vector3.up);
            SetPrivateField(gate, "breakForceOrigin", root.transform);
            SetPrivateField(gate, "breakForceThreshold", threshold);
            SetPrivateField(gate, "explosionForce", explosionForce);
            SetPrivateField(gate, "partCollisionEnableDelay", 0f);
            SetPrivateField(gate, "breakStabilizeDuration", 0f);

            InvokeMethod(gate, "OnEnable");
            return (gate, gateCollider, partBody);
        }

        private static Type FindRuntimeType(string fullTypeName)
        {
            foreach (Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
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
