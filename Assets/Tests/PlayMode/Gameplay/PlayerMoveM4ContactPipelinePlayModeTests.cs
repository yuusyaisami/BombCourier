using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerMoveM4ContactPipelinePlayModeTests
    {
        private const string MoveContactBufferTypeName = "BC.Base.MoveContactBuffer";
        private const string MoveContactInfoTypeName = "BC.Base.MoveContactInfo";
        private const string MoveContactKindTypeName = "BC.Base.MoveContactKind";
        private const string MovementBodyGeometryTypeName = "BC.Base.MovementBodyGeometry";
        private const string GroundHitInfoTypeName = "BC.Base.GroundHitInfo";
        private const string GroundSurfaceKindTypeName = "BC.Base.GroundSurfaceKind";
        private const string ContactClassifierTypeName = "BC.Base.ContactClassifier";
        private const string CollisionConstraintSolverTypeName = "BC.Base.CollisionConstraintSolver";
        private const string EntityMoveRuntimeStateTypeName = "BC.Base.EntityMoveRuntimeState";
        private const string EntityMoveMotorTypeName = "BC.Base.EntityMoveMotorMB";

        [Test]
        public void ContactClassifier_AssignsExpectedKinds()
        {
            object buffer = Activator.CreateInstance(FindRuntimeType(MoveContactBufferTypeName));
            AppendBufferedContact(buffer, CreateContact(new Vector3(0.0f, 0.10f, 0.0f), Vector3.up, "None"));
            AppendBufferedContact(buffer, CreateContact(new Vector3(0.0f, 0.12f, 0.0f), new Vector3(0.8f, 0.6f, 0.0f).normalized, "None"));
            AppendBufferedContact(buffer, CreateContact(new Vector3(0.0f, 1.20f, 0.0f), Vector3.right, "None"));
            AppendBufferedContact(buffer, CreateContact(new Vector3(0.0f, 1.96f, 0.0f), Vector3.down, "None"));

            object geometry = CreateGeometry();
            object groundHit = Activator.CreateInstance(
                FindRuntimeType(GroundHitInfoTypeName),
                false,
                null,
                null,
                Vector3.zero,
                Vector3.up,
                0.0f,
                0.0f,
                ParseEnum(GroundSurfaceKindTypeName, "None"),
                false);

            InvokeStatic(
                ContactClassifierTypeName,
                "Classify",
                buffer,
                geometry,
                groundHit,
                55.0f,
                0.08f);

            Assert.AreEqual("FootGround", GetContactKindName(buffer, 0));
            Assert.AreEqual("FootEdge", GetContactKindName(buffer, 1));
            Assert.AreEqual("BodyWall", GetContactKindName(buffer, 2));
            Assert.AreEqual("Ceiling", GetContactKindName(buffer, 3));
        }

        [Test]
        public void CollisionConstraintSolver_DoesNotRemoveWallVelocity_ForFootKinds()
        {
            object buffer = Activator.CreateInstance(FindRuntimeType(MoveContactBufferTypeName));
            AppendBufferedContact(buffer, CreateContact(new Vector3(0.0f, 0.10f, 0.0f), Vector3.up, "FootGround"));
            AppendBufferedContact(buffer, CreateContact(new Vector3(0.0f, 0.12f, 0.0f), new Vector3(0.8f, 0.6f, 0.0f).normalized, "FootEdge"));

            object state = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));
            SetFieldValue(state, "VerticalVelocity", -10.0f);
            SetFieldValue(state, "LastGroundedTime", -999.0f);

            int wallRemovalCalls = 0;
            InvokeStatic(
                CollisionConstraintSolverTypeName,
                "Resolve",
                buffer,
                state,
                -3.0f,
                12.0f,
                (Action<Vector3>)(_ => wallRemovalCalls++));

            Assert.AreEqual(0, wallRemovalCalls);
            Assert.AreEqual(12.0f, GetFieldValue<float>(state, "LastGroundedTime"));
            Assert.AreEqual(-3.0f, GetFieldValue<float>(state, "VerticalVelocity"));
        }

        [Test]
        public void CollisionConstraintSolver_AppliesBodyWallAndCeilingConstraints()
        {
            object buffer = Activator.CreateInstance(FindRuntimeType(MoveContactBufferTypeName));
            AppendBufferedContact(buffer, CreateContact(new Vector3(0.0f, 1.20f, 0.0f), Vector3.right, "BodyWall"));
            AppendBufferedContact(buffer, CreateContact(new Vector3(0.0f, 1.96f, 0.0f), Vector3.down, "Ceiling"));

            object state = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));
            SetFieldValue(state, "VerticalVelocity", 3.5f);

            int wallRemovalCalls = 0;
            Vector3 lastWallNormal = Vector3.zero;

            InvokeStatic(
                CollisionConstraintSolverTypeName,
                "Resolve",
                buffer,
                state,
                -3.0f,
                20.0f,
                (Action<Vector3>)(normal =>
                {
                    wallRemovalCalls++;
                    lastWallNormal = normal;
                }));

            Assert.AreEqual(1, wallRemovalCalls);
            Assert.AreEqual(Vector3.right, lastWallNormal);
            Assert.AreEqual(0.0f, GetFieldValue<float>(state, "VerticalVelocity"));
        }

        [Test]
        public void BufferedContact_DoesNotAffectState_UntilProcessed()
        {
            GameObject gameObject = new GameObject("M4_BufferedContactTest");

            try
            {
                Type motorType = FindRuntimeType(EntityMoveMotorTypeName);
                object motor = gameObject.AddComponent(motorType);

                object runtimeState = GetFieldValue<object>(motor, "runtimeState");
                object contactBuffer = GetFieldValue<object>(motor, "moveContactBuffer");

                SetFieldValue(runtimeState, "VerticalVelocity", -10.0f);
                SetFieldValue(runtimeState, "LastGroundedTime", -999.0f);

                float groundedStickVelocity = GetFieldValue<float>(motor, "groundedStickVelocity");
                AppendBufferedContact(contactBuffer, CreateContact(new Vector3(0.0f, -10.0f, 0.0f), Vector3.up, "None"));

                // バッファ投入時点では副作用を出さず、固定順処理時のみ状態が変わることを保証する。
                Assert.AreEqual(-10.0f, GetFieldValue<float>(runtimeState, "VerticalVelocity"));
                Assert.AreEqual(-999.0f, GetFieldValue<float>(runtimeState, "LastGroundedTime"));

                InvokeInstance(motor, "ProcessBufferedContacts");

                Assert.AreEqual(groundedStickVelocity, GetFieldValue<float>(runtimeState, "VerticalVelocity"));
                Assert.Greater(GetFieldValue<float>(runtimeState, "LastGroundedTime"), -999.0f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static object CreateContact(Vector3 point, Vector3 normal, string kindName)
        {
            float upDot = Vector3.Dot(normal, Vector3.up);
            object contact = Activator.CreateInstance(FindRuntimeType(MoveContactInfoTypeName));
            SetFieldValue(contact, "Point", point);
            SetFieldValue(contact, "Normal", normal);
            SetFieldValue(contact, "UpDot", upDot);
            SetFieldValue(contact, "Angle", Vector3.Angle(normal, Vector3.up));
            SetFieldValue(contact, "Kind", ParseEnum(MoveContactKindTypeName, kindName));
            return contact;
        }

        private static object CreateGeometry()
        {
            return Activator.CreateInstance(
                FindRuntimeType(MovementBodyGeometryTypeName),
                Vector3.zero,
                Vector3.zero,
                new Vector3(0.0f, 0.5f, 0.0f),
                new Vector3(0.0f, 1.5f, 0.0f),
                0.5f,
                0.0f,
                2.0f,
                0.5f);
        }

        private static void AppendBufferedContact(object buffer, object contact)
        {
            InvokeInstance(buffer, "AddDirect", contact);
        }

        private static string GetContactKindName(object buffer, int index)
        {
            object contact = InvokeInstance(buffer, "Get", index);
            object kind = GetFieldValue<object>(contact, "Kind");
            return kind.ToString();
        }

        private static object ParseEnum(string fullTypeName, string enumValue)
        {
            return Enum.Parse(FindRuntimeType(fullTypeName), enumValue);
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
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, $"Expected static method: {fullTypeName}.{methodName}");
            return method.Invoke(null, arguments);
        }

        private static object InvokeInstance(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Expected instance method: {target.GetType().Name}.{methodName}");
            return method.Invoke(target, arguments);
        }

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
        }

        private static void SetFieldValue<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
