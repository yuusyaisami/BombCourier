using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Base.Tests
{
    public sealed class MovingPlatformRailRoutingTests
    {
        private const string RailGraphTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformRailGraph";
        private const string RailControllerTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformRailController";
        private const string RailNodeTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformRailNode";
        private const string RailConnectionTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformRailConnection";
        private const string LayerTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformLayer";
        private const string RouteSegmentTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformRailRouteSegment";
        private const string RouteTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformRailLayerRoute";
        private const string PlaybackModeTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformPlaybackMode";
        private const string EasingModeTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformEasingMode";
        private const string BasePoseTypeName = "BC.Gimmick.MovingPlatform.MovingPlatformBasePose";
        private const string ReactiveVector3TypeName = "BC.Base.ReactiveVector3";
        private const string ReactiveEvaluationModeTypeName = "BC.Base.ReactiveEvaluationMode";

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
        public void RailControllerSwitchesLayerOnlyAfterReachingSharedConnectionNode()
        {
            RailFixture fixture = CreateFixture();

            RailPose pose = fixture.Tick(0.5f, 0);
            Assert.That(pose.Position.x, Is.EqualTo(-1f).Within(0.001f));
            Assert.That(pose.Position.z, Is.EqualTo(0f).Within(0.001f));

            pose = fixture.Tick(0.25f, 1);
            Assert.That(pose.Position.x, Is.EqualTo(-0.5f).Within(0.001f));
            Assert.That(pose.Position.z, Is.EqualTo(0f).Within(0.001f));

            pose = fixture.Tick(0.25f, 1);
            Assert.That(pose.Position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(pose.Position.z, Is.EqualTo(0f).Within(0.001f));

            pose = fixture.Tick(0.5f, 1);
            Assert.That(pose.Position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(pose.Position.z, Is.GreaterThan(0.1f));
        }

        [Test]
        public void RailControllerUsesShortestRerouteWhenReactivatedFromStoppedSegment()
        {
            RailFixture fixture = CreateFixture();

            RailPose pose = fixture.Tick(0.5f, 0);
            Assert.That(pose.Position.x, Is.EqualTo(-1f).Within(0.001f));

            pose = fixture.Tick(0.1f, -1);
            Assert.That(pose.Position.x, Is.EqualTo(-1f).Within(0.001f));
            Assert.That(pose.Position.z, Is.EqualTo(0f).Within(0.001f));

            pose = fixture.Tick(0.25f, 1);
            Assert.That(pose.Position.x, Is.EqualTo(-0.5f).Within(0.001f));
            Assert.That(pose.Position.z, Is.EqualTo(0f).Within(0.001f));

            pose = fixture.Tick(0.25f, 1);
            Assert.That(pose.Position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(pose.Position.z, Is.EqualTo(0f).Within(0.001f));

            pose = fixture.Tick(0.5f, 1);
            Assert.That(pose.Position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(pose.Position.z, Is.GreaterThan(0.1f));
        }

        [Test]
        public void RailControllerUsesRouteSegmentDurationOverride()
        {
            RailFixture fixture = CreateFixture(horizontalFirstSegmentDuration: 2f);

            RailPose pose = fixture.Tick(0.5f, 0);
            Assert.That(pose.Position.x, Is.EqualTo(-1.5f).Within(0.001f));
            Assert.That(pose.Position.z, Is.EqualTo(0f).Within(0.001f));
        }

        private RailFixture CreateFixture(float? horizontalFirstSegmentDuration = null)
        {
            GameObject platformRoot = CreateGameObject("MovingPlatformRailRoot");

            CreateNode(platformRoot.transform, "West", new Vector3(-2f, 0f, 0f));
            CreateNode(platformRoot.transform, "Center", Vector3.zero);
            CreateNode(platformRoot.transform, "East", new Vector3(2f, 0f, 0f));
            CreateNode(platformRoot.transform, "South", new Vector3(0f, 0f, -2f));
            CreateNode(platformRoot.transform, "North", new Vector3(0f, 0f, 2f));

            Array railNodes = CreateRuntimeArray(
                RailNodeTypeName,
                CreateRailNode("West", new Vector3(-2f, 0f, 0f)),
                CreateRailNode("Center", Vector3.zero),
                CreateRailNode("East", new Vector3(2f, 0f, 0f)),
                CreateRailNode("South", new Vector3(0f, 0f, -2f)),
                CreateRailNode("North", new Vector3(0f, 0f, 2f)));

            Array railConnections = CreateRuntimeArray(
                RailConnectionTypeName,
                CreateRailConnection("West", "Center"),
                CreateRailConnection("Center", "East"),
                CreateRailConnection("South", "Center"),
                CreateRailConnection("Center", "North"));

            object horizontalLayer = CreateLayer(
                "West",
                new[]
                {
                    CreateRouteSegment("Center", horizontalFirstSegmentDuration),
                    CreateRouteSegment("East"),
                },
                "Once");
            object verticalLayer = CreateLayer(
                "South",
                new[]
                {
                    CreateRouteSegment("Center"),
                    CreateRouteSegment("North"),
                },
                "Once");

            object graph = InvokeStaticMethod(
                FindRuntimeType(RailGraphTypeName),
                "Build",
                platformRoot.transform,
                railNodes,
                railConnections);
            Assert.IsNotNull(graph, "Expected shared rail graph to build successfully.");

            Type routeType = FindRuntimeType(RouteTypeName);
            Array routes = Array.CreateInstance(routeType, 2);
            routes.SetValue(CreateRoute(horizontalLayer, graph), 0);
            routes.SetValue(CreateRoute(verticalLayer, graph), 1);

            Type controllerType = FindRuntimeType(RailControllerTypeName);
            object controller = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { graph, routes },
                null);
            Assert.IsNotNull(controller, "Expected rail controller instance.");

            object basePose = CreateInstance(
                BasePoseTypeName,
                platformRoot.transform.position,
                platformRoot.transform.rotation,
                platformRoot.transform.localScale);
            InvokeMethod(controller, "Reset", basePose, new Vector3(-2f, 0f, 0f));

            return new RailFixture(controller, basePose, new Vector3(-2f, 0f, 0f));
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private Transform CreateNode(Transform parent, string name, Vector3 localPosition)
        {
            GameObject nodeObject = CreateGameObject(name);
            nodeObject.transform.SetParent(parent, false);
            nodeObject.transform.localPosition = localPosition;
            return nodeObject.transform;
        }

        private static object CreateRailNode(string nodePath, Vector3 localPosition)
        {
            object node = CreateInstance(RailNodeTypeName);
            SetField(node, "nodePath", nodePath);
            SetField(node, "nodeName", nodePath);
            SetField(node, "localPosition", CreateReactiveVector3Literal(localPosition));
            return node;
        }

        private static object CreateReactiveVector3Literal(Vector3 value)
        {
            Type reactiveVector3Type = FindRuntimeType(ReactiveVector3TypeName);
            MethodInfo literalValueMethod = reactiveVector3Type.GetMethod(
                "LiteralValue",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Vector3), FindRuntimeType(ReactiveEvaluationModeTypeName) },
                null);
            Assert.IsNotNull(literalValueMethod, $"Expected static method: {reactiveVector3Type.FullName}.LiteralValue");

            return literalValueMethod.Invoke(null, new[] { (object)value, CreateEnumValue(ReactiveEvaluationModeTypeName, "Snapshot") });
        }

        private static object CreateRailConnection(string fromNodePath, string toNodePath)
        {
            object connection = CreateInstance(RailConnectionTypeName);
            SetField(connection, "fromNodePath", fromNodePath);
            SetField(connection, "toNodePath", toNodePath);
            SetField(connection, "bidirectional", true);
            SetField(connection, "duration", 1f);
            SetField(connection, "easingMode", CreateEnumValue(EasingModeTypeName, "Linear"));
            return connection;
        }

        private static object CreateRouteSegment(string targetNodePath, float? durationOverride = null)
        {
            object segment = CreateInstance(RouteSegmentTypeName);
            SetField(segment, "segmentName", targetNodePath);
            SetField(segment, "targetNodePath", targetNodePath);
            SetField(segment, "overrideConnectionTiming", durationOverride.HasValue);

            if (durationOverride.HasValue)
            {
                SetField(segment, "duration", durationOverride.Value);
                SetField(segment, "easingMode", CreateEnumValue(EasingModeTypeName, "Linear"));
            }

            return segment;
        }

        private static object CreateLayer(string startNodePath, object[] routeSegments, string playbackMode)
        {
            object layer = CreateInstance(LayerTypeName);
            SetField(layer, "playbackMode", CreateEnumValue(PlaybackModeTypeName, playbackMode));
            SetField(layer, "startNodePath", startNodePath);
            SetField(layer, "routeSegments", CreateRuntimeArray(RouteSegmentTypeName, routeSegments));
            return layer;
        }

        private static object CreateRoute(object layer, object graph)
        {
            return Activator.CreateInstance(
                FindRuntimeType(RouteTypeName),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { layer, graph },
                null);
        }

        private static Array CreateRuntimeArray(string typeName, params object[] items)
        {
            Type elementType = FindRuntimeType(typeName);
            Array array = Array.CreateInstance(elementType, items.Length);
            for (int i = 0; i < items.Length; i++)
                array.SetValue(items[i], i);

            return array;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field: {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field: {target.GetType().Name}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static object CreateEnumValue(string fullTypeName, string memberName)
        {
            Type enumType = FindRuntimeType(fullTypeName);
            return Enum.Parse(enumType, memberName);
        }

        private static object CreateInstance(string fullTypeName, params object[] arguments)
        {
            Type type = FindRuntimeType(fullTypeName);
            object instance = Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                arguments,
                null);
            Assert.IsNotNull(instance, $"Expected instance: {fullTypeName}");
            return instance;
        }

        private static object InvokeStaticMethod(Type type, string methodName, params object[] arguments)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method: {type.FullName}.{methodName}");
            return method.Invoke(null, arguments);
        }

        private static object InvokeMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method: {target.GetType().FullName}.{methodName}");
            return method.Invoke(target, arguments);
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

        private sealed class RailFixture
        {
            private readonly object controller;
            private readonly object basePose;
            private Vector3 currentWorldPosition;

            public RailFixture(object controller, object basePose, Vector3 initialWorldPosition)
            {
                this.controller = controller;
                this.basePose = basePose;
                currentWorldPosition = initialWorldPosition;
            }

            public RailPose Tick(float deltaTime, int requestedLayerIndex)
            {
                MethodInfo tickMethod = controller.GetType().GetMethod("Tick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.IsNotNull(tickMethod, "Expected MovingPlatform rail Tick method.");

                object[] arguments = { deltaTime, requestedLayerIndex, basePose, currentWorldPosition, false };
                object pose = tickMethod.Invoke(controller, arguments);
                Vector3 position = GetFieldValue<Vector3>(pose, "Position");
                bool sequenceCompleted = arguments[4] is bool value && value;
                currentWorldPosition = position;
                return new RailPose(position, sequenceCompleted);
            }
        }

        private readonly struct RailPose
        {
            public readonly Vector3 Position;
            public readonly bool SequenceCompleted;

            public RailPose(Vector3 position, bool sequenceCompleted)
            {
                Position = position;
                SequenceCompleted = sequenceCompleted;
            }
        }
    }
}
