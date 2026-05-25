using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerMoveM9CushionContactPushPlayModeTests
    {
        private const string CushionImpactResultTypeName = "BC.Gimmick.Cushion.CushionImpactResult";
        private const string CushionHighJumpBufferTypeName = "BC.Base.CushionHighJumpBuffer";
        private const string CushionImpactHandlerTypeName = "BC.Base.CushionImpactHandler";
        private const string ContactPushEmitterTypeName = "BC.Base.ContactPushEmitter";
        private const string MoveContactInfoTypeName = "BC.Base.MoveContactInfo";

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
        public void CushionHighJumpBuffer_ArmAndExpire_FollowsM9Contract()
        {
            object buffer = Activator.CreateInstance(FindRuntimeType(CushionHighJumpBufferTypeName));
            object impactResult = InvokeStatic(
                CushionImpactResultTypeName,
                "Bounce",
                new Vector3(0.0f, 3.0f, 4.0f),
                8.0f,
                1.5f,
                true);

            InvokeInstance(buffer, "Arm", impactResult, 1.0f, 0.25f);

            object[] getPendingArguments =
            {
                1.1f,
                true,
                Vector3.zero,
                0.0f,
                1.0f,
            };

            bool availableBeforeExpire = (bool)InvokeInstance(buffer, "TryGetPending", getPendingArguments);
            Assert.IsTrue(availableBeforeExpire);
            Assert.AreEqual(5.0f, ((Vector3)getPendingArguments[2]).magnitude, 0.0001f);
            Assert.AreEqual(8.0f, (float)getPendingArguments[3], 0.0001f);
            Assert.AreEqual(1.5f, (float)getPendingArguments[4], 0.0001f);

            object[] expiredArguments =
            {
                1.3f,
                true,
                Vector3.zero,
                0.0f,
                1.0f,
            };

            bool availableAfterExpire = (bool)InvokeInstance(buffer, "TryGetPending", expiredArguments);
            Assert.IsFalse(availableAfterExpire);
        }

        [Test]
        public void CushionImpactHandler_TryBuildHighJumpBounceVelocity_UsesBufferedInputAndCap()
        {
            object[] buildArguments =
            {
                new Vector3(0.0f, 3.0f, 4.0f),
                6.0f,
                1.5f,
                true,
                Vector3.zero,
            };

            bool built = (bool)InvokeStatic(CushionImpactHandlerTypeName, "TryBuildHighJumpBounceVelocity", buildArguments);
            Vector3 boostedVelocity = (Vector3)buildArguments[4];

            Assert.IsTrue(built);
            Assert.AreEqual(7.5f, boostedVelocity.magnitude, 0.0001f);
            Assert.AreEqual(0.6f, boostedVelocity.normalized.y, 0.0001f);
            Assert.AreEqual(0.8f, boostedVelocity.normalized.z, 0.0001f);

            object[] withoutInputArguments =
            {
                new Vector3(0.0f, 3.0f, 4.0f),
                6.0f,
                1.5f,
                false,
                Vector3.zero,
            };

            bool builtWithoutInput = (bool)InvokeStatic(CushionImpactHandlerTypeName, "TryBuildHighJumpBounceVelocity", withoutInputArguments);
            Assert.IsFalse(builtWithoutInput);
        }

        [UnityTest]
        public IEnumerator ContactPushEmitter_TryApply_EmitsImpulseOnlyAboveThreshold()
        {
            GameObject actor = new GameObject("M9_Actor");
            createdObjects.Add(actor);

            GameObject target = new GameObject("M9_Target");
            createdObjects.Add(target);

            Rigidbody targetBody = target.AddComponent<Rigidbody>();
            targetBody.isKinematic = false;
            targetBody.useGravity = false;

            BoxCollider targetCollider = target.AddComponent<BoxCollider>();
            Component impactResponse = target.AddComponent(FindRuntimeType("BC.Base.EntityImpactResponseMB"));
            SetFieldValue(impactResponse, "targetRigidbody", targetBody);
            SetFieldValue(impactResponse, "canReceiveContactImpact", true);
            InvokeInstance(impactResponse, "SetContactImpactEnabled", true);

            object contact = Activator.CreateInstance(FindRuntimeType(MoveContactInfoTypeName));
            SetFieldValue(contact, "Collider", targetCollider);
            SetFieldValue(contact, "Point", target.transform.position);
            SetFieldValue(contact, "Normal", Vector3.left);
            SetFieldValue(contact, "RelativeSpeed", 5.0f);

            InvokeStatic(
                ContactPushEmitterTypeName,
                "TryApply",
                contact,
                actor.transform,
                new Vector3(4.0f, 0.0f, 0.0f),
                true,
                false,
                false,
                1.0f,
                2.0f,
                0.5f,
                6.0f);

            yield return new WaitForFixedUpdate();

            Assert.Greater(targetBody.linearVelocity.magnitude, 0.0001f);

            targetBody.linearVelocity = Vector3.zero;

            InvokeStatic(
                ContactPushEmitterTypeName,
                "TryApply",
                contact,
                actor.transform,
                new Vector3(0.2f, 0.0f, 0.0f),
                true,
                false,
                false,
                1.0f,
                2.0f,
                0.5f,
                6.0f);

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0.0f, targetBody.linearVelocity.magnitude, 0.0001f);
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

        private static object InvokeStatic(string fullTypeName, string methodName, params object[] arguments)
        {
            Type type = FindRuntimeType(fullTypeName);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method: {fullTypeName}.{methodName}");
            return method.Invoke(null, arguments);
        }

        private static object InvokeInstance(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected instance method: {target.GetType().Name}.{methodName}");
            return method.Invoke(target, arguments);
        }

        private static void SetFieldValue<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
