using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    // 新スナップショット（開始ベースライン / レジストリ自己登録）の往復テスト。
    public sealed class StageCheckpointServicePlayModeTests
    {
        private const string SnapshotServiceTypeName = "BC.Stage.Snapshot.StageSnapshotServiceMB";
        private const string RestorableTypeName = "BC.Stage.Snapshot.StageRestorableMB";
        private const string LeverObjectTypeName = "BC.Gimmick.LeverObject.LeverObjectMB";
        private const string LeverDirectionTypeName = "BC.Gimmick.LeverObject.LeverDirection";
        private const string CushionTypeName = "BC.Gimmick.Cushion.CushionMB";
        private const string BreakableGateTypeName = "BC.Gimmick.BreakableGateObjectMB";

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
        public IEnumerator RestoreBaseline_ReactivatesTargetBeforeParticipantRestore()
        {
            Component service = CreateService();

            GameObject target = CreateObject("LeverTarget");
            AddRestorable(target);
            Component lever = target.AddComponent(FindRuntimeType(LeverObjectTypeName));

            SetPrivateField(lever, "currentState", ParseEnumValue(LeverDirectionTypeName, "Right"));
            InvokeMethod(service, "CaptureBaseline");

            target.SetActive(false);
            yield return null;

            SetPrivateField(lever, "currentState", ParseEnumValue(LeverDirectionTypeName, "Left"));
            InvokeMethod(service, "RestoreBaseline");

            Assert.IsTrue(target.activeSelf, "Restore should reactivate the target captured as active.");
            Assert.AreEqual(
                "Right",
                GetPrivateField<object>(lever, "currentState").ToString(),
                "Participant restore must run after reactivation so OnEnable resets do not overwrite baseline state.");
        }

        [UnityTest]
        public IEnumerator RestoreBaseline_RestoresGenericRigidbodyRuntimeState()
        {
            Component service = CreateService();

            GameObject target = CreateObject("RigidbodyTarget");
            target.transform.position = new Vector3(1f, 2f, 3f);
            target.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            target.AddComponent<BoxCollider>();
            Rigidbody rb = target.AddComponent<Rigidbody>();
            AddRestorable(target);

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
            rb.linearVelocity = new Vector3(2f, 0f, -1f);
            rb.angularVelocity = new Vector3(0f, 3f, 0f);

            InvokeMethod(service, "CaptureBaseline");

            target.transform.position = new Vector3(12f, 4f, -6f);
            target.transform.rotation = Quaternion.Euler(45f, 90f, 0f);
            rb.linearVelocity = new Vector3(-9f, 1f, 5f);
            rb.angularVelocity = new Vector3(1f, -2f, 0f);

            InvokeMethod(service, "RestoreBaseline");
            yield return null;

            AssertVector3Close(target.transform.position, new Vector3(1f, 2f, 3f), 0.0001f, "World position should be restored.");
            Assert.That(Mathf.DeltaAngle(target.transform.rotation.eulerAngles.y, 30f), Is.EqualTo(0f).Within(0.01f));
            Assert.IsFalse(rb.isKinematic, "Dynamic rigidbody state should be restored.");
            AssertVector3Close(rb.linearVelocity, new Vector3(2f, 0f, -1f), 0.0001f, "linearVelocity should be restored.");
            AssertVector3Close(rb.angularVelocity, new Vector3(0f, 3f, 0f), 0.0001f, "angularVelocity should be restored.");
        }

        // Player所有のCushion漏れ対策: レジストリ自己登録なら stageRoot 配下でなくても捕捉される。
        [UnityTest]
        public IEnumerator CaptureBaseline_IncludesMarkedObjectsAnywhere()
        {
            Component service = CreateService();

            GameObject externalMarkedRoot = CreateObject("ExternalMarkedRoot");
            externalMarkedRoot.transform.position = new Vector3(6f, 1f, -2f);
            externalMarkedRoot.AddComponent<BoxCollider>();
            Rigidbody rb = externalMarkedRoot.AddComponent<Rigidbody>();
            AddRestorable(externalMarkedRoot);

            InvokeMethod(service, "CaptureBaseline");

            externalMarkedRoot.transform.position = new Vector3(-8f, 4f, 9f);
            rb.linearVelocity = new Vector3(5f, 0f, 0f);

            InvokeMethod(service, "RestoreBaseline");
            yield return null;

            AssertVector3Close(externalMarkedRoot.transform.position, new Vector3(6f, 1f, -2f), 0.0001f, "Registry capture should include marked objects anywhere.");
            AssertVector3Close(rb.linearVelocity, Vector3.zero, 0.0001f, "External rigidbody velocity should be restored.");
        }

        [UnityTest]
        public IEnumerator RestoreBaseline_RestoresCushionHeldStateToBaseline()
        {
            Component service = CreateService();

            GameObject root = CreateObject("StageRoot");
            GameObject target = CreateObject("CushionTarget");
            target.transform.SetParent(root.transform, false);
            target.transform.localPosition = new Vector3(-2f, 0.5f, 4f);
            target.AddComponent<BoxCollider>();
            Rigidbody rb = target.AddComponent<Rigidbody>();
            Component cushion = target.AddComponent(FindRuntimeType(CushionTypeName));
            AddRestorable(target);

            InvokeMethod(service, "CaptureBaseline");

            GameObject holder = CreateObject("Holder");
            Transform handlePoint = new GameObject("HandlePoint").transform;
            createdObjects.Add(handlePoint.gameObject);
            handlePoint.SetParent(holder.transform, false);

            InvokeMethod(cushion, "OnHandle", handlePoint);
            Assert.IsTrue(GetPrivateField<bool>(cushion, "isHandled"), "Test setup must put the cushion into handled state.");

            InvokeMethod(service, "RestoreBaseline");
            yield return null;

            Assert.IsFalse(GetPrivateField<bool>(cushion, "isHandled"), "Restore should clear stale handled state.");
            Assert.AreSame(root.transform, target.transform.parent, "Cushion parent should return to the baseline parent.");
            Assert.IsFalse(rb.isKinematic, "Cushion rigidbody should return to its baseline dynamics.");
            AssertVector3Close(target.transform.localPosition, new Vector3(-2f, 0.5f, 4f), 0.0001f, "Cushion localPosition should be restored.");
        }

        [UnityTest]
        public IEnumerator RestoreBaseline_RestoresBrokenGateToClosed()
        {
            Component service = CreateService();

            GameObject gateGo = CreateObject("Gate");
            gateGo.SetActive(false); // Awake/OnEnable を遅らせて breakableParts/gateCollider を先に注入する。

            GameObject part = CreateObject("Part");
            part.transform.SetParent(gateGo.transform, false);
            part.transform.localPosition = new Vector3(0f, 1f, 0f);
            part.AddComponent<BoxCollider>();
            Rigidbody partRb = part.AddComponent<Rigidbody>();

            BoxCollider gateCollider = gateGo.AddComponent<BoxCollider>();
            Component gate = gateGo.AddComponent(FindRuntimeType(BreakableGateTypeName));
            SetPrivateField(gate, "breakableParts", new List<Rigidbody> { partRb });
            SetPrivateField(gate, "gateCollider", gateCollider);

            gateGo.SetActive(true); // Awake -> InitializeAuthoringState（閉じた状態）, RequireComponent でマーク自動付与。
            yield return null;

            InvokeMethod(service, "CaptureBaseline");

            // 破壊する。
            InvokeMethod(gate, "OnBombImpactReceived", Vector3.up, 1000f);
            Assert.IsTrue(GetPrivateField<bool>(gate, "isBroken"), "Gate should be broken after a sufficient impact.");
            Assert.IsFalse(gateCollider.enabled, "Gate collider should be disabled while broken.");

            // 破片を動かして「壊れて飛んだ」状態を再現。
            part.transform.localPosition = new Vector3(3f, 5f, -2f);

            InvokeMethod(service, "RestoreBaseline");
            yield return null;

            Assert.IsFalse(GetPrivateField<bool>(gate, "isBroken"), "Restore should reset the gate to not-broken.");
            Assert.IsTrue(gateCollider.enabled, "Gate collider should be re-enabled after restore.");
            Assert.IsTrue(partRb.isKinematic, "Restored gate part should be kinematic (closed state).");
            AssertVector3Close(part.transform.localPosition, new Vector3(0f, 1f, 0f), 0.0001f, "Gate part should return to its baseline local position.");
        }

        [UnityTest]
        public IEnumerator RestoreBaseline_LogsWarningForDestroyedTarget()
        {
            Component service = CreateService();

            GameObject target = CreateObject("MissingTarget");
            AddRestorable(target);

            InvokeMethod(service, "CaptureBaseline");

            createdObjects.Remove(target);
            UnityEngine.Object.DestroyImmediate(target);
            yield return null;

            LogAssert.Expect(LogType.Warning, new Regex("復元漏れ"));
            InvokeMethod(service, "RestoreBaseline");
        }

        private Component CreateService()
        {
            GameObject serviceRoot = CreateObject("SnapshotService");
            return serviceRoot.AddComponent(FindRuntimeType(SnapshotServiceTypeName));
        }

        private void AddRestorable(GameObject go)
        {
            go.AddComponent(FindRuntimeType(RestorableTypeName));
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
