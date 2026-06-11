using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerMoveM1BodyFoundationPlayModeTests
    {
        private const string EntityMoveMotorTypeName = "BC.Base.EntityMoveMotorMB";
        private const string ValidatorTypeName = "BC.Base.MovementColliderPolicyValidator";
        private const string GroundProbeSolverTypeName = "BC.Base.GroundProbeSolver";

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
        public void MovementBodyResolver_EnsuresBodyAndDisablesLegacyCharacterController()
        {
            GameObject root = new GameObject("M1BodyResolverRoot");
            createdObjects.Add(root);

            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.radius = 0.61f;
            characterController.height = 1.85f;
            characterController.center = new Vector3(0.1f, 0.9f, -0.2f);

            Component moveMotor = root.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));

            InvokeMethod(moveMotor, "EnsureMovementBody");

            Rigidbody resolvedRigidbody = root.GetComponent<Rigidbody>();
            CapsuleCollider resolvedCapsule = root.GetComponent<CapsuleCollider>();

            Assert.IsNotNull(resolvedRigidbody);
            Assert.IsNotNull(resolvedCapsule);
            Assert.IsFalse(characterController.enabled);
            Assert.Greater(resolvedCapsule.radius, 0.01f);
            Assert.Greater(resolvedCapsule.height, 0.1f);
        }

        [Test]
        public void MovementPhysicsMaterialFactory_AppliesLowFrictionMaterial()
        {
            GameObject root = new GameObject("M1MaterialRoot");
            createdObjects.Add(root);

            Component moveMotor = root.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));
            InvokeMethod(moveMotor, "EnsureMovementBody");

            CapsuleCollider bodyCollider = (CapsuleCollider)GetPrivateField<object>(moveMotor, "bodyCollider");
            PhysicsMaterial material = bodyCollider.sharedMaterial;

            Assert.IsNotNull(material);
            Assert.AreEqual(0.0f, material.dynamicFriction, 0.0001f);
            Assert.AreEqual(0.0f, material.staticFriction, 0.0001f);
            Assert.AreEqual(PhysicsMaterialCombine.Minimum, material.frictionCombine);
            Assert.AreEqual(PhysicsMaterialCombine.Minimum, material.bounceCombine);
        }

        [Test]
        public void ColliderPolicyValidator_DetectsUnregisteredNonTriggerCollider()
        {
            GameObject root = new GameObject("M1PolicyRoot");
            createdObjects.Add(root);

            Component moveMotor = root.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));
            CapsuleCollider bodyCollider = root.GetComponent<CapsuleCollider>();
            BoxCollider extraCollider = root.AddComponent<BoxCollider>();
            extraCollider.isTrigger = false;

            bool valid = InvokeValidator(root.transform, bodyCollider, null, out string errorMessage);

            Assert.IsFalse(valid);
            StringAssert.Contains("Unregistered non-trigger collider", errorMessage);
            StringAssert.Contains("BoxCollider", errorMessage);
        }

        [Test]
        public void ColliderPolicyValidator_RequiresFootColliderTrigger()
        {
            GameObject root = new GameObject("M1FootPolicyRoot");
            createdObjects.Add(root);

            Component moveMotor = root.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));
            CapsuleCollider bodyCollider = root.GetComponent<CapsuleCollider>();

            SphereCollider footCollider = root.AddComponent<SphereCollider>();
            footCollider.isTrigger = false;

            bool valid = InvokeValidator(root.transform, bodyCollider, footCollider, out string errorMessage);

            Assert.IsFalse(valid);
            StringAssert.Contains("Foot collider must be trigger", errorMessage);
        }

        [Test]
        public void ColliderPolicyValidator_AllowsBodyPlusTriggerFootOnly()
        {
            GameObject root = new GameObject("M1ValidPolicyRoot");
            createdObjects.Add(root);

            Component moveMotor = root.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));
            CapsuleCollider bodyCollider = root.GetComponent<CapsuleCollider>();

            SphereCollider footCollider = root.AddComponent<SphereCollider>();
            footCollider.isTrigger = true;

            bool valid = InvokeValidator(root.transform, bodyCollider, footCollider, out string errorMessage);

            Assert.IsTrue(valid);
            Assert.IsNull(errorMessage);
        }

        [Test]
        public void PlayerRagdollController_RunsBeforeMoveMotorColliderValidation()
        {
            Type ragdollType = FindRuntimeType("BC.Manager.PlayerRagdollControllerMB");
            Type moveMotorType = FindRuntimeType(EntityMoveMotorTypeName);

            Assert.Less(
                ResolveDefaultExecutionOrder(ragdollType),
                ResolveDefaultExecutionOrder(moveMotorType),
                "Ragdoll colliders must be disabled before EntityMoveMotor validates the movement collider policy.");
        }

        [Test]
        public void GroundProbeSolver_FindsClosestFlatGround()
        {
            GameObject ground = new GameObject("M1GroundProbeGround");
            createdObjects.Add(ground);
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(10.0f, 1.0f, 10.0f);
            ground.transform.position = new Vector3(0.0f, -0.5f, 0.0f);

            GameObject root = new GameObject("M1GroundProbeRoot");
            createdObjects.Add(root);
            root.transform.position = new Vector3(0.0f, 0.55f, 0.0f);

            CapsuleCollider bodyCollider = root.AddComponent<CapsuleCollider>();
            bodyCollider.height = 2.0f;
            bodyCollider.radius = 0.5f;
            bodyCollider.center = new Vector3(0.0f, 0.5f, 0.0f);

            Type solverType = FindRuntimeType(GroundProbeSolverTypeName);
            MethodInfo probeMethod = solverType.GetMethod("Probe", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(probeMethod, $"Expected static method: {GroundProbeSolverTypeName}.Probe");

            object hit = probeMethod.Invoke(
                null,
                new object[]
                {
                    root.transform,
                    bodyCollider,
                    (LayerMask)(~0),
                    0.18f,
                    0.03f,
                    55.0f,
                    new RaycastHit[8],
                });

            Assert.IsTrue(GetPublicProperty<bool>(hit, "IsValid"));
            AssertVectorApproximately(Vector3.up, GetPublicProperty<Vector3>(hit, "Normal"));
        }

        private static bool InvokeValidator(Transform root, Collider bodyCollider, Collider footCollider, out string errorMessage)
        {
            Type validatorType = FindRuntimeType(ValidatorTypeName);
            MethodInfo method = validatorType.GetMethod("TryValidate", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, $"Expected static method: {ValidatorTypeName}.TryValidate");

            object[] args = { root, bodyCollider, footCollider, null };
            bool valid = (bool)method.Invoke(null, args);
            errorMessage = (string)args[3];
            return valid;
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

        private static int ResolveDefaultExecutionOrder(Type type)
        {
            DefaultExecutionOrder order = type.GetCustomAttribute<DefaultExecutionOrder>();
            return order != null ? order.order : 0;
        }

        private static object InvokeMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, arguments);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
        }

        private static T GetPublicProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }
    }
}
