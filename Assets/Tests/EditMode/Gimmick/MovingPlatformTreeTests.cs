using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BC.Base.Tests
{
    public sealed class MovingPlatformTreeTests
    {
        private const string TreeAuthoringTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformTreeAuthoring";
        private const string RailNodeAuthoringTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformRailNodeAuthoring";
        private const string SelectorNodeAuthoringTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformSelectorNodeAuthoring";
        private const string ControlNodeAuthoringTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformControlNodeAuthoring";
        private const string MoveNodeAuthoringTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformMoveNodeAuthoring";
        private const string TreeRuntimeTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformTreeRuntime";
        private const string TraversalControllerTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformTraversalController";
        private const string MigrationTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformTreeMigration";
        private const string TreeWindowTypeName = "BC.Editor.Gimmick.MovingPlatformTools.MovingPlatformTreeWindow";
        private const string MovingPlatformMbTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformMB";
        private const string ReactiveVector3TypeName = "BC.Base.ReactiveVector3";
        private const string ReactiveEvaluationModeTypeName = "BC.Base.ReactiveEvaluationMode";
        private const string ReactiveEvalContextTypeName = "BC.Base.ReactiveEvalContext";
        private const string BasePoseTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformBasePose";
        private const string LegacyRailNodeTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformRailNode";
        private const string LegacyRailConnectionTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformRailConnection";
        private const string LegacyLayerTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformLayer";
        private const string LegacyLayerSegmentTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformLayerSegment";
        private const string LegacyMoveSegmentTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformRailRouteSegment";
        private const string LegacyWaitSegmentTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformWaitSegment";
        private const string PlaybackModeTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformPlaybackMode";
        private const string WaitModeTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformWaitMode";

        private readonly List<UnityEngine.Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void TreeRuntimeRejectsMultipleRoots()
        {
            object tree = CreateTreeAuthoring(
                new[]
                {
                    CreateRailNode("RailA", string.Empty, Vector3.zero),
                    CreateRailNode("RailB", string.Empty, Vector3.right),
                },
                new[]
                {
                    CreateSelector("Selector", "RailA", CreateMoveStep("RailB")),
                });

            object runtime = BuildRuntime(tree);
            IReadOnlyList<object> issues = ReadIssues(runtime);

            Assert.That(issues, Has.Some.Matches<object>(issue =>
                string.Equals(GetMemberValue(issue, "Severity")?.ToString(), "Error", StringComparison.Ordinal) &&
                string.Equals(GetMemberValue(issue, "Code")?.ToString(), "Tree.MultipleRoots", StringComparison.Ordinal)));
        }

        [Test]
        public void TraversalReroutesThroughAncestorWhenSelectorChanges()
        {
            object tree = CreateTreeAuthoring(
                new[]
                {
                    CreateRailNode("Root", string.Empty, Vector3.zero),
                    CreateRailNode("West", "Root", new Vector3(-2f, 0f, 0f)),
                    CreateRailNode("North", "Root", new Vector3(0f, 0f, 2f)),
                },
                new[]
                {
                    CreateSelector("SelectorWest", "Root", CreateMoveStep("West")),
                    CreateSelector("SelectorNorth", "Root", CreateMoveStep("North")),
                });

            object runtime = BuildRuntime(tree);
            object controller = CreateInstance(TraversalControllerTypeName, runtime, 6f, 4f);
            object basePose = CreateInstance(BasePoseTypeName, Vector3.zero, Quaternion.identity, Vector3.one);

            InvokeMethod(controller, "Reset", basePose, Vector3.zero);

            bool began = (bool)InvokeMethod(controller, "BeginSelectorTransition", 0, basePose, Vector3.zero, true);
            Assert.That(began, Is.True);

            RailPose pose = TickController(controller, 1.0f, 0, basePose, Vector3.zero);
            Assert.That(pose.Position.x, Is.EqualTo(-2f).Within(0.001f));

            began = (bool)InvokeMethod(controller, "BeginSelectorTransition", 1, basePose, pose.Position, true);
            Assert.That(began, Is.True);

            RailPose transferPose = TickController(controller, 1.0f, 1, basePose, pose.Position);
            Assert.That(transferPose.Position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(transferPose.Position.z, Is.EqualTo(0f).Within(0.001f));

            RailPose finalPose = TickController(controller, 1.0f, 1, basePose, transferPose.Position);
            Assert.That(finalPose.Position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(finalPose.Position.z, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void MigrationInfersTreeFromSimplePingPongRoute()
        {
            Type legacyRailNodeType = FindType(LegacyRailNodeTypeName);
            Type legacyRailConnectionType = FindType(LegacyRailConnectionTypeName);
            Type legacyLayerType = FindType(LegacyLayerTypeName);

            Array legacyRailNodes = CreateTypedArray(
                legacyRailNodeType,
                CreateLegacyRailNode("Node1", Vector3.zero),
                CreateLegacyRailNode("Node2", new Vector3(2f, 0f, 0f)));

            object legacyLayer = CreateLegacyLayer(
                "LegacyLayer",
                "Node1",
                ParseEnum(PlaybackModeTypeName, "PingPong"),
                CreateLegacyMoveSegment("Node2"),
                CreateLegacyWaitSegment(0.5f),
                CreateLegacyMoveSegment("Node1"));

            Array legacyLayers = CreateTypedArray(legacyLayerType, legacyLayer);
            Array legacyConnections = Array.CreateInstance(legacyRailConnectionType, 0);

            object migrationResult = InvokeStaticMethod(
                FindType(MigrationTypeName),
                "TryMigrate",
                legacyRailNodes,
                legacyConnections,
                legacyLayers);

            bool success = (bool)GetFieldValue(migrationResult, "Success");
            Assert.That(success, Is.True);

            object migratedTree = GetFieldValue(migrationResult, "TreeAuthoring");
            Assert.That(migratedTree, Is.Not.Null);
            Assert.That(GetCollectionCount(GetMemberValue(migratedTree, "RailNodes")), Is.EqualTo(2));
            Assert.That(GetMemberValue(migratedTree, "RootRailNodeId")?.ToString(), Is.EqualTo("Node1"));
            Assert.That(GetCollectionCount(GetMemberValue(migratedTree, "Selectors")), Is.EqualTo(1));
        }

        [Test]
        public void TreeWindowBuildsForMigratedPlatform()
        {
            GameObject platformObject = CreateGameObject("MovingPlatform");
            Component movingPlatform = platformObject.AddComponent(FindType(MovingPlatformMbTypeName));

            SetField(movingPlatform, "railNodes", CreateTypedArray(
                FindType(LegacyRailNodeTypeName),
                CreateLegacyRailNode("Node1", Vector3.zero),
                CreateLegacyRailNode("Node2", Vector3.right)));
            SetField(movingPlatform, "layers", CreateTypedArray(
                FindType(LegacyLayerTypeName),
                CreateLegacyLayer("LegacyLayer", "Node1", ParseEnum(PlaybackModeTypeName, "Loop"), CreateLegacyMoveSegment("Node2"))));

            object[] migrationArguments = { null };
            bool migrated = (bool)InvokeMethod(movingPlatform, "TryApplyLegacyMigration", migrationArguments);
            string failureReason = migrationArguments[0] as string;
            Assert.That(migrated, Is.True, failureReason);

            Type treeWindowType = FindType(TreeWindowTypeName);
            EditorWindow window = ScriptableObject.CreateInstance(treeWindowType) as EditorWindow;
            Assert.IsNotNull(window);
            createdObjects.Add(window);

            InvokeMethod(window, "CreateGUI");
            InvokeMethod(window, "Bind", movingPlatform);

            Label footerLabel = GetFieldValue(window, "footerLabel") as Label;
            Assert.That(footerLabel, Is.Not.Null);
            Assert.That(footerLabel.text, Does.Contain("Rails: 2"));
        }

        [Test]
        public void TreeAuthoringEditApisKeepTreeValidAfterRootRemoval()
        {
            object tree = CreateInstance(TreeAuthoringTypeName);

            object rootRail = InvokeMethod(tree, "AddRailNode", "Root Rail", string.Empty, CreateReactiveVector3Literal(Vector3.zero));
            string rootRailId = GetMemberValue(rootRail, "StableId")?.ToString();
            Assert.That(rootRailId, Is.Not.Null.And.Not.Empty);

            object childRail = InvokeMethod(tree, "AddRailNode", "Child Rail", rootRailId, CreateReactiveVector3Literal(Vector3.right));
            string childRailId = GetMemberValue(childRail, "StableId")?.ToString();
            Assert.That(childRailId, Is.Not.Null.And.Not.Empty);

            object selector = InvokeMethod(tree, "AddSelectorNode", "Selector", rootRailId);
            string selectorId = GetMemberValue(selector, "StableId")?.ToString();
            Assert.That(selectorId, Is.Not.Null.And.Not.Empty);

            object step = InvokeMethod(tree, "AddSelectorStep", selectorId, FindType(MoveNodeAuthoringTypeName), null);
            Assert.That(step, Is.Not.Null);

            bool removed = (bool)InvokeMethod(tree, "RemoveRailNode", rootRailId);
            Assert.That(removed, Is.True);
            Assert.That(GetMemberValue(tree, "RootRailNodeId")?.ToString(), Is.EqualTo(childRailId));

            object runtime = BuildRuntime(tree);
            IReadOnlyList<object> issues = ReadIssues(runtime);
            Assert.That(issues, Is.Empty);
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static object CreateTreeAuthoring(object[] railNodes, object[] selectors)
        {
            object tree = CreateInstance(TreeAuthoringTypeName);
            Type railNodeType = FindType(RailNodeAuthoringTypeName);
            Type selectorNodeType = FindType(SelectorNodeAuthoringTypeName);
            SetField(tree, "railNodes", CreateTypedList(railNodeType, railNodes));
            SetField(tree, "selectors", CreateTypedList(selectorNodeType, selectors));
            string rootRailNodeId = railNodes.Length > 0
                ? GetMemberValue(railNodes[0], "StableId")?.ToString() ?? string.Empty
                : string.Empty;
            SetField(tree, "rootRailNodeId", rootRailNodeId);
            return tree;
        }

        private static object CreateRailNode(string stableId, string parentRailNodeId, Vector3 localPosition)
        {
            object railNode = CreateInstance(RailNodeAuthoringTypeName);
            SetField(railNode, "stableId", stableId);
            SetField(railNode, "label", stableId);
            SetField(railNode, "parentRailNodeId", parentRailNodeId);
            SetField(railNode, "localPosition", CreateReactiveVector3Literal(localPosition));
            return railNode;
        }

        private static object CreateSelector(string stableId, string anchorRailNodeId, params object[] steps)
        {
            object selector = CreateInstance(SelectorNodeAuthoringTypeName);
            SetField(selector, "stableId", stableId);
            SetField(selector, "label", stableId);
            SetField(selector, "anchorRailNodeId", anchorRailNodeId);
            SetField(selector, "orderedChildren", CreateTypedList(FindType(ControlNodeAuthoringTypeName), steps));
            return selector;
        }

        private static object CreateMoveStep(string targetRailId)
        {
            object step = CreateInstance(MoveNodeAuthoringTypeName);
            SetField(step, "stableId", $"move.{targetRailId}");
            SetField(step, "label", targetRailId);
            SetField(step, "targetRailNodeId", targetRailId);
            return step;
        }

        private static object CreateLegacyRailNode(string nodePath, Vector3 localPosition)
        {
            object legacyNode = CreateInstance(LegacyRailNodeTypeName);
            SetField(legacyNode, "nodePath", nodePath);
            SetField(legacyNode, "localPosition", CreateReactiveVector3Literal(localPosition));
            return legacyNode;
        }

        private static object CreateLegacyMoveSegment(string targetNodePath)
        {
            object segment = CreateInstance(LegacyMoveSegmentTypeName);
            SetField(segment, "targetNodePath", targetNodePath);
            return segment;
        }

        private static object CreateLegacyWaitSegment(float duration)
        {
            object segment = CreateInstance(LegacyWaitSegmentTypeName);
            SetField(segment, "waitMode", ParseEnum(WaitModeTypeName, "Duration"));
            SetField(segment, "duration", duration);
            return segment;
        }

        private static object CreateLegacyLayer(string layerName, string startNodePath, object playbackMode, params object[] segments)
        {
            object layer = CreateInstance(LegacyLayerTypeName);
            SetField(layer, "layerName", layerName);
            SetField(layer, "startNodePath", startNodePath);
            SetField(layer, "playbackMode", playbackMode);
            SetField(layer, "routeSegments", CreateTypedList(FindType(LegacyLayerSegmentTypeName), segments));
            return layer;
        }

        private static object BuildRuntime(object treeAuthoring)
        {
            return InvokeStaticMethod(
                FindType(TreeRuntimeTypeName),
                "Build",
                treeAuthoring,
                null,
                Activator.CreateInstance(FindType(ReactiveEvalContextTypeName)));
        }

        private static IReadOnlyList<object> ReadIssues(object runtime)
        {
            IEnumerable enumerable = GetMemberValue(runtime, "Issues") as IEnumerable;
            Assert.IsNotNull(enumerable);

            var issues = new List<object>();
            foreach (object issue in enumerable)
                issues.Add(issue);

            return issues;
        }

        private static RailPose TickController(
            object controller,
            float deltaTime,
            int requestedSelectorIndex,
            object basePose,
            Vector3 currentPosition)
        {
            MethodInfo tickMethod = FindMethod(controller.GetType(), "Tick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new object[]
            {
                deltaTime,
                requestedSelectorIndex,
                basePose,
                currentPosition,
                Quaternion.identity,
                Vector3.one,
                false,
            });

            Assert.IsNotNull(tickMethod);

            object[] arguments =
            {
                deltaTime,
                requestedSelectorIndex,
                basePose,
                currentPosition,
                Quaternion.identity,
                Vector3.one,
                false,
            };

            object pose = tickMethod.Invoke(controller, arguments);
            bool sequenceCompleted = arguments[6] is bool completed && completed;
            Vector3 position = (Vector3)GetMemberValue(pose, "Position");
            return new RailPose(position, sequenceCompleted);
        }

        private static object CreateReactiveVector3Literal(Vector3 value)
        {
            object evaluationMode = ParseEnum(ReactiveEvaluationModeTypeName, "Snapshot");
            return InvokeStaticMethod(FindType(ReactiveVector3TypeName), "LiteralValue", value, evaluationMode);
        }

        private static object ParseEnum(string fullTypeName, string memberName)
        {
            return Enum.Parse(FindType(fullTypeName), memberName);
        }

        private static object CreateInstance(string fullTypeName, params object[] arguments)
        {
            return CreateInstance(FindType(fullTypeName), arguments);
        }

        private static object CreateInstance(Type type, params object[] arguments)
        {
            ConstructorInfo constructor = FindConstructor(type, arguments);
            Assert.IsNotNull(constructor, $"Expected constructor: {type.FullName}");
            return constructor.Invoke(BuildInvokeArguments(constructor.GetParameters(), arguments));
        }

        private static object InvokeStaticMethod(Type type, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(type, methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, arguments);
            Assert.IsNotNull(method, $"Expected static method: {type.FullName}.{methodName}");
            return method.Invoke(null, BuildInvokeArguments(method.GetParameters(), arguments));
        }

        private static object InvokeMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, arguments);
            Assert.IsNotNull(method, $"Expected method: {target.GetType().FullName}.{methodName}");
            return method.Invoke(target, BuildInvokeArguments(method.GetParameters(), arguments));
        }

        private static object GetMemberValue(object target, string memberName)
        {
            Assert.IsNotNull(target);

            PropertyInfo property = FindProperty(target.GetType(), memberName);
            if (property != null)
                return property.GetValue(target);

            FieldInfo field = FindField(target.GetType(), memberName);
            if (field != null)
                return field.GetValue(target);

            Assert.Fail($"Expected member: {target.GetType().FullName}.{memberName}");
            return null;
        }

        private static object GetFieldValue(object target, string fieldName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.IsNotNull(field, $"Expected field: {target.GetType().FullName}.{fieldName}");
            return field.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.IsNotNull(field, $"Expected field: {target.GetType().FullName}.{fieldName}");
            field.SetValue(target, value);
        }

        private static Type FindType(string fullTypeName, bool failIfMissing = true)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullTypeName);
                if (type != null)
                    return type;
            }

            if (failIfMissing)
                Assert.Fail($"Expected type: {fullTypeName}");

            return null;
        }

        private static Array CreateTypedArray(Type elementType, params object[] items)
        {
            Array array = Array.CreateInstance(elementType, items.Length);
            for (int i = 0; i < items.Length; i++)
                array.SetValue(items[i], i);

            return array;
        }

        private static object CreateTypedList(Type elementType, params object[] items)
        {
            Type listType = typeof(List<>).MakeGenericType(elementType);
            IList list = (IList)Activator.CreateInstance(listType);
            for (int i = 0; i < items.Length; i++)
                list.Add(items[i]);

            return list;
        }

        private static int GetCollectionCount(object collection)
        {
            if (collection is ICollection nonGenericCollection)
                return nonGenericCollection.Count;

            PropertyInfo countProperty = collection?.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(countProperty);
            return (int)countProperty.GetValue(collection);
        }

        private static MethodInfo FindMethod(Type ownerType, string methodName, BindingFlags flags, object[] arguments)
        {
            MethodInfo[] methods = ownerType.GetMethods(flags);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != methodName)
                    continue;

                if (CanAcceptArguments(method.GetParameters(), arguments))
                    return method;
            }

            return null;
        }

        private static ConstructorInfo FindConstructor(Type ownerType, object[] arguments)
        {
            ConstructorInfo[] constructors = ownerType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < constructors.Length; i++)
            {
                if (CanAcceptArguments(constructors[i].GetParameters(), arguments))
                    return constructors[i];
            }

            return null;
        }

        private static bool CanAcceptArguments(ParameterInfo[] parameters, object[] arguments)
        {
            arguments ??= Array.Empty<object>();
            if (arguments.Length > parameters.Length)
                return false;

            for (int i = 0; i < arguments.Length; i++)
            {
                Type parameterType = GetEffectiveParameterType(parameters[i]);
                object argument = arguments[i];

                if (argument == null)
                {
                    if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null)
                        return false;

                    continue;
                }

                if (!parameterType.IsInstanceOfType(argument))
                    return false;
            }

            for (int i = arguments.Length; i < parameters.Length; i++)
            {
                if (!parameters[i].IsOptional)
                    return false;
            }

            return true;
        }

        private static object[] BuildInvokeArguments(ParameterInfo[] parameters, object[] arguments)
        {
            arguments ??= Array.Empty<object>();
            object[] resolved = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                resolved[i] = i < arguments.Length ? arguments[i] : Type.Missing;

            return resolved;
        }

        private static Type GetEffectiveParameterType(ParameterInfo parameter)
        {
            return parameter.ParameterType.IsByRef
                ? parameter.ParameterType.GetElementType()
                : parameter.ParameterType;
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            Type cursor = type;
            while (cursor != null)
            {
                FieldInfo field = cursor.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return field;

                cursor = cursor.BaseType;
            }

            return null;
        }

        private static PropertyInfo FindProperty(Type type, string propertyName)
        {
            Type cursor = type;
            while (cursor != null)
            {
                PropertyInfo property = cursor.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                    return property;

                cursor = cursor.BaseType;
            }

            return null;
        }

        private readonly struct RailPose
        {
            public RailPose(Vector3 position, bool sequenceCompleted)
            {
                Position = position;
                SequenceCompleted = sequenceCompleted;
            }

            public Vector3 Position { get; }
            public bool SequenceCompleted { get; }
        }
    }
}
