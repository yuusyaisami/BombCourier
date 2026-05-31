using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class StageCheckpointServicePlayModeTests
    {
        private const string StageCheckpointServiceTypeName = "BC.Stage.StageCheckpointServiceMB";
        private const string StageSaveMarkTypeName = "BC.Stage.StageSaveMarkMB";
        private const string LeverObjectTypeName = "BC.Gimmick.LeverObject.LeverObjectMB";
        private const string LeverDirectionTypeName = "BC.Gimmick.LeverObject.LeverDirection";
        private const string CushionTypeName = "BC.Gimmick.Cushion.CushionMB";

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
        public IEnumerator RestoreSnapshot_ReactivatesTargetBeforeParticipantRestore()
        {
            GameObject root = CreateObject("StageRoot");
            Component checkpointService = root.AddComponent(FindRuntimeType(StageCheckpointServiceTypeName));

            GameObject target = CreateObject("LeverTarget");
            target.transform.SetParent(root.transform, false);
            target.AddComponent(FindRuntimeType(StageSaveMarkTypeName));
            Component lever = target.AddComponent(FindRuntimeType(LeverObjectTypeName));

            SetPrivateField(lever, "currentState", ParseEnumValue(LeverDirectionTypeName, "Right"));
            InvokeMethod(checkpointService, "Capture");

            target.SetActive(false);
            yield return null;

            SetPrivateField(lever, "currentState", ParseEnumValue(LeverDirectionTypeName, "Left"));
            InvokeMethod(checkpointService, "Restore");

            Assert.IsTrue(target.activeSelf, "Restore should reactivate the target when the snapshot was captured as active.");
            Assert.AreEqual(
                "Right",
                GetPrivateField<object>(lever, "currentState").ToString(),
                "Participant restore must run after reactivation so OnEnable resets do not overwrite checkpoint state.");
        }

        [UnityTest]
        public IEnumerator RestoreSnapshot_RestoresGenericRigidbodyRuntimeState()
        {
            GameObject root = CreateObject("StageRoot");
            Component checkpointService = root.AddComponent(FindRuntimeType(StageCheckpointServiceTypeName));

            GameObject target = CreateObject("RigidbodyTarget");
            target.transform.localPosition = new Vector3(1f, 2f, 3f);
            target.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            target.AddComponent<BoxCollider>();
            Rigidbody rb = target.AddComponent<Rigidbody>();
            target.AddComponent(FindRuntimeType(StageSaveMarkTypeName));

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
            rb.linearVelocity = new Vector3(2f, 0f, -1f);
            rb.angularVelocity = new Vector3(0f, 3f, 0f);

            InvokeMethod(checkpointService, "Capture");

            target.transform.position = new Vector3(12f, 4f, -6f);
            target.transform.rotation = Quaternion.Euler(45f, 90f, 0f);
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();

            InvokeMethod(checkpointService, "Restore");
            yield return null;

            AssertVector3Close(target.transform.localPosition, new Vector3(1f, 2f, 3f), 0.0001f, "localPosition should be restored.");
            Assert.That(Mathf.DeltaAngle(target.transform.localRotation.eulerAngles.y, 30f), Is.EqualTo(0f).Within(0.01f));
            Assert.IsFalse(rb.isKinematic, "Dynamic rigidbody state should be restored.");
            Assert.IsTrue(rb.useGravity, "useGravity should be restored.");
            Assert.IsTrue(rb.detectCollisions, "detectCollisions should be restored.");
            AssertVector3Close(rb.linearVelocity, new Vector3(2f, 0f, -1f), 0.0001f, "linearVelocity should be restored.");
            AssertVector3Close(rb.angularVelocity, new Vector3(0f, 3f, 0f), 0.0001f, "angularVelocity should be restored.");
        }

        [UnityTest]
        public IEnumerator CaptureSnapshot_IncludesMarkedObjectsOutsideStageRoot()
        {
            GameObject serviceRoot = CreateObject("CheckpointServiceRoot");
            GameObject stageRoot = CreateObject("StageRoot");
            Component checkpointService = serviceRoot.AddComponent(FindRuntimeType(StageCheckpointServiceTypeName));
            SetPrivateField(checkpointService, "stageRoot", stageRoot.transform);

            GameObject externalMarkedRoot = CreateObject("ExternalMarkedRoot");
            externalMarkedRoot.transform.position = new Vector3(6f, 1f, -2f);
            externalMarkedRoot.AddComponent<BoxCollider>();
            Rigidbody rb = externalMarkedRoot.AddComponent<Rigidbody>();
            externalMarkedRoot.AddComponent(FindRuntimeType(StageSaveMarkTypeName));

            InvokeMethod(checkpointService, "Capture");

            externalMarkedRoot.transform.position = new Vector3(-8f, 4f, 9f);
            rb.linearVelocity = new Vector3(5f, 0f, 0f);

            InvokeMethod(checkpointService, "Restore");
            yield return null;

            AssertVector3Close(externalMarkedRoot.transform.position, new Vector3(6f, 1f, -2f), 0.0001f, "Scene-level capture should include marked objects outside stageRoot.");
            AssertVector3Close(rb.linearVelocity, Vector3.zero, 0.0001f, "External rigidbody state should also be restored.");
        }

        [UnityTest]
        public IEnumerator RestoreSnapshot_RestoresCushionHeldStateToCapturedRuntimeState()
        {
            GameObject root = CreateObject("StageRoot");
            Component checkpointService = root.AddComponent(FindRuntimeType(StageCheckpointServiceTypeName));

            GameObject target = CreateObject("CushionTarget");
            target.transform.SetParent(root.transform, false);
            target.transform.localPosition = new Vector3(-2f, 0.5f, 4f);
            target.AddComponent<BoxCollider>();
            Rigidbody rb = target.AddComponent<Rigidbody>();
            Component cushion = target.AddComponent(FindRuntimeType(CushionTypeName));
            target.AddComponent(FindRuntimeType(StageSaveMarkTypeName));

            InvokeMethod(checkpointService, "Capture");

            GameObject holder = CreateObject("Holder");
            Transform handlePoint = new GameObject("HandlePoint").transform;
            createdObjects.Add(handlePoint.gameObject);
            handlePoint.SetParent(holder.transform, false);

            InvokeMethod(cushion, "OnHandle", handlePoint);
            Assert.IsTrue(GetPrivateField<bool>(cushion, "isHandled"), "Test setup must put the cushion into handled state.");

            InvokeMethod(checkpointService, "Restore");
            yield return null;

            Assert.IsFalse(GetPrivateField<bool>(cushion, "isHandled"), "Checkpoint restore should clear stale handled state when the snapshot was captured unhandled.");
            Assert.AreSame(root.transform, target.transform.parent, "Cushion parent should return to the checkpoint parent.");
            Assert.IsFalse(rb.isKinematic, "Cushion rigidbody should return to its checkpoint dynamics.");
            Assert.IsTrue(rb.useGravity, "Cushion rigidbody gravity should be restored.");
            AssertVector3Close(target.transform.localPosition, new Vector3(-2f, 0.5f, 4f), 0.0001f, "Cushion localPosition should be restored.");
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static Type FindRuntimeType(string fullTypeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullTypeName);
                if (type != null)
                    return type;
            }

            Assert.Fail($"Expected runtime type to exist: {fullTypeName}");
            return null;
        }

        private static object ParseEnumValue(string fullTypeName, string enumName)
        {
            Type enumType = FindRuntimeType(fullTypeName);
            return Enum.Parse(enumType, enumName, ignoreCase: false);
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static void AssertVector3Close(Vector3 actual, Vector3 expected, float tolerance, string message)
        {
            Assert.That(Mathf.Abs(actual.x - expected.x), Is.LessThanOrEqualTo(tolerance), $"{message} (x)");
            Assert.That(Mathf.Abs(actual.y - expected.y), Is.LessThanOrEqualTo(tolerance), $"{message} (y)");
            Assert.That(Mathf.Abs(actual.z - expected.z), Is.LessThanOrEqualTo(tolerance), $"{message} (z)");
        }
    }
}
