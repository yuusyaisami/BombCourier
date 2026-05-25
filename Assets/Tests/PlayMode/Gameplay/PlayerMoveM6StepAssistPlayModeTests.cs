using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerMoveM6StepAssistPlayModeTests
    {
        private const string StepAssistSettingsTypeName = "BC.Base.StepAssistSettings";
        private const string StepAssistSolverTypeName = "BC.Base.StepAssistSolver";
        private const string PositionCorrectionTypeName = "BC.Base.PositionCorrection";

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
        public void StepAssistSolver_Climbs_005m_WithLowSpeedIntent()
        {
            Scenario scenario = CreateScenario(stepHeight: 0.05f, addUpperBlock: false);
            bool resolved = ResolveStepOverTicks(
                scenario,
                desiredDirection: new Vector3(0.08f, 0.0f, 0.0f),
                bodyVelocity: Vector3.zero,
                maxTicks: 6,
                out object correction,
                out _);

            Assert.IsTrue(resolved);
            Assert.IsTrue(GetPropertyValue<bool>(correction, "HasCorrection"));
            Vector3 delta = GetPropertyValue<Vector3>(correction, "Delta");
            Assert.Greater(delta.y, 0.0f);
        }

        [Test]
        public void StepAssistSolver_Climbs_015m_WithNormalInput()
        {
            Scenario scenario = CreateScenario(stepHeight: 0.15f, addUpperBlock: false);
            bool resolved = ResolveStepOverTicks(
                scenario,
                desiredDirection: Vector3.right,
                bodyVelocity: new Vector3(2.0f, 0.0f, 0.0f),
                maxTicks: 6,
                out object correction,
                out _);

            Assert.IsTrue(resolved);
            Assert.IsTrue(GetPropertyValue<bool>(correction, "HasCorrection"));
            Assert.Greater(GetPropertyValue<Vector3>(correction, "Delta").y, 0.0f);
        }

        [Test]
        public void StepAssistSolver_Climbs_030m_WithinMaxStepHeight()
        {
            Scenario scenario = CreateScenario(stepHeight: 0.30f, addUpperBlock: false);
            bool resolved = ResolveStepOverTicks(
                scenario,
                desiredDirection: Vector3.right,
                bodyVelocity: new Vector3(2.0f, 0.0f, 0.0f),
                maxTicks: 8,
                out object correction,
                out _);

            Assert.IsTrue(resolved);
            Assert.IsTrue(GetPropertyValue<bool>(correction, "HasCorrection"));
            Assert.Greater(GetPropertyValue<Vector3>(correction, "Delta").y, 0.0f);
        }

        [Test]
        public void StepAssistSolver_DoesNotClimb_040m_OverMaxStepHeight()
        {
            Scenario scenario = CreateScenario(stepHeight: 0.40f, addUpperBlock: false);
            bool resolved = ResolveStepOverTicks(
                scenario,
                desiredDirection: Vector3.right,
                bodyVelocity: new Vector3(2.0f, 0.0f, 0.0f),
                maxTicks: 8,
                out object correction,
                out _);

            Assert.IsFalse(resolved);
            Assert.IsFalse(GetPropertyValue<bool>(correction, "HasCorrection"));
        }

        [Test]
        public void StepAssistSolver_DoesNotClimb_WhenUpperBlocked()
        {
            Scenario scenario = CreateScenario(stepHeight: 0.15f, addUpperBlock: true);
            bool resolved = ResolveStepOverTicks(
                scenario,
                desiredDirection: Vector3.right,
                bodyVelocity: new Vector3(2.0f, 0.0f, 0.0f),
                maxTicks: 6,
                out object correction,
                out _);

            Assert.IsFalse(resolved);
            Assert.IsFalse(GetPropertyValue<bool>(correction, "HasCorrection"));
        }

        [Test]
        public void StepAssistSolver_DoesNotMoveRigidbodyDirectly()
        {
            Scenario scenario = CreateScenario(stepHeight: 0.15f, addUpperBlock: false);
            Vector3 before = scenario.Body.position;

            bool resolved = TryResolveStep(
                scenario,
                desiredDirection: Vector3.right,
                bodyVelocity: new Vector3(2.0f, 0.0f, 0.0f),
                out object correction,
                out _);

            Assert.IsTrue(resolved);
            Assert.AreEqual(before.x, scenario.Body.position.x, 0.0001f);
            Assert.AreEqual(before.y, scenario.Body.position.y, 0.0001f);
            Assert.AreEqual(before.z, scenario.Body.position.z, 0.0001f);
            Assert.IsTrue(GetPropertyValue<bool>(correction, "HasCorrection"));
        }

        [Test]
        public void StepAssistSolver_DoesNotTriggerOnTinyInputNoise()
        {
            Scenario scenario = CreateScenario(stepHeight: 0.05f, addUpperBlock: false);
            bool resolved = TryResolveStep(
                scenario,
                desiredDirection: new Vector3(0.001f, 0.0f, 0.0f),
                bodyVelocity: Vector3.zero,
                out object correction,
                out _);

            Assert.IsFalse(resolved);
            Assert.IsFalse(GetPropertyValue<bool>(correction, "HasCorrection"));
        }

        [Test]
        public void StepAssistSolver_PartialCorrection_DoesNotReportResolved()
        {
            Scenario scenario = CreateScenario(stepHeight: 0.30f, addUpperBlock: false);
            SetFieldValue(scenario.Settings, "SnapSpeed", 2.0f);

            bool resolved = TryResolveStep(
                scenario,
                desiredDirection: Vector3.right,
                bodyVelocity: new Vector3(2.0f, 0.0f, 0.0f),
                out object correction,
                out _);

            Assert.IsFalse(resolved);
            Assert.IsTrue(GetPropertyValue<bool>(correction, "HasCorrection"));
        }

        private Scenario CreateScenario(float stepHeight, bool addUpperBlock)
        {
            GameObject ground = new GameObject($"M6_Ground_{stepHeight:F2}");
            createdObjects.Add(ground);
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(20.0f, 1.0f, 8.0f);
            ground.transform.position = new Vector3(0.0f, -0.5f, 0.0f);

            GameObject step = new GameObject($"M6_Step_{stepHeight:F2}");
            createdObjects.Add(step);
            BoxCollider stepCollider = step.AddComponent<BoxCollider>();
            stepCollider.size = new Vector3(0.4f, stepHeight, 2.0f);
            step.transform.position = new Vector3(0.55f, stepHeight * 0.5f, 0.0f);

            if (addUpperBlock)
            {
                GameObject upper = new GameObject($"M6_UpperBlock_{stepHeight:F2}");
                createdObjects.Add(upper);
                BoxCollider upperCollider = upper.AddComponent<BoxCollider>();
                upperCollider.size = new Vector3(0.4f, 0.2f, 2.0f);
                upper.transform.position = new Vector3(0.55f, stepHeight + 0.95f, 0.0f);
            }

            GameObject body = new GameObject($"M6_Body_{stepHeight:F2}");
            createdObjects.Add(body);
            body.transform.position = Vector3.zero;

            Rigidbody bodyRigidbody = body.AddComponent<Rigidbody>();
            bodyRigidbody.useGravity = false;
            bodyRigidbody.isKinematic = true;

            CapsuleCollider bodyCollider = body.AddComponent<CapsuleCollider>();
            bodyCollider.height = 1.6f;
            bodyCollider.radius = 0.25f;
            bodyCollider.center = new Vector3(0.0f, 0.8f, 0.0f);

            object settings = Activator.CreateInstance(FindRuntimeType(StepAssistSettingsTypeName));
            SetFieldValue(settings, "Enabled", true);
            SetFieldValue(settings, "MaxStepHeight", 0.32f);
            SetFieldValue(settings, "ForwardProbeDistance", 0.28f);
            SetFieldValue(settings, "LowerProbeHeight", 0.02f);
            SetFieldValue(settings, "UpperClearanceSkin", 0.03f);
            SetFieldValue(settings, "StepDownProbeDistance", 0.36f);
            SetFieldValue(settings, "MinIntentMagnitude", 0.05f);
            SetFieldValue(settings, "SnapSpeed", 12.0f);

            return new Scenario
            {
                OwnerTransform = body.transform,
                Body = bodyRigidbody,
                Capsule = bodyCollider,
                Settings = settings,
            };
        }

        private bool TryResolveStep(
            Scenario scenario,
            Vector3 desiredDirection,
            Vector3 bodyVelocity,
            out object correction,
            out object stepGround)
        {
            correction = Activator.CreateInstance(FindRuntimeType(PositionCorrectionTypeName));
            stepGround = null;

            object[] args =
            {
                scenario.Settings,
                scenario.OwnerTransform,
                scenario.Body,
                scenario.Capsule,
                (LayerMask)(~0),
                55.0f,
                0.18f,
                true,
                Time.time,
                0.12f,
                -0.5f,
                false,
                false,
                desiredDirection,
                bodyVelocity,
                0.02f,
                new Collider[16],
                correction,
                stepGround,
            };

            bool resolved = (bool)InvokeStaticWithRefOut(StepAssistSolverTypeName, "TryResolve", args);
            correction = args[17];
            stepGround = args[18];
            return resolved;
        }

        private bool ResolveStepOverTicks(
            Scenario scenario,
            Vector3 desiredDirection,
            Vector3 bodyVelocity,
            int maxTicks,
            out object correction,
            out object stepGround)
        {
            correction = Activator.CreateInstance(FindRuntimeType(PositionCorrectionTypeName));
            stepGround = null;

            for (int i = 0; i < maxTicks; i++)
            {
                bool resolved = TryResolveStep(scenario, desiredDirection, bodyVelocity, out correction, out stepGround);

                if (GetPropertyValue<bool>(correction, "HasCorrection"))
                {
                    Vector3 delta = GetPropertyValue<Vector3>(correction, "Delta");
                    scenario.Body.position += delta;
                }

                if (resolved)
                    return true;
            }

            return false;
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

        private static object InvokeStaticWithRefOut(string fullTypeName, string methodName, object[] arguments)
        {
            Type type = FindRuntimeType(fullTypeName);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method: {fullTypeName}.{methodName}");
            return method.Invoke(null, arguments);
        }

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
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

        private sealed class Scenario
        {
            public Transform OwnerTransform;
            public Rigidbody Body;
            public CapsuleCollider Capsule;
            public object Settings;
        }
    }
}
