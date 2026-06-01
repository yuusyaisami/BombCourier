using System;
using System.Collections.Generic;
using System.Reflection;
using BC.Base;
using BC.Tutorial;
using BC.UI;
using NUnit.Framework;
using UnityEngine;

namespace BC.Editor.Tests
{
    public sealed class TutorialRuntimeServiceEditModeTests
    {
        private readonly List<GameObject> createdObjects = new();

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
        public void ValidateDefinition_RejectsDuplicateStepIdsAndUnknownJumpTarget()
        {
            TutorialStageAuthoringMB authoring = CreateAuthoring();
            var first = CreateStep("intro", nextStepId: "missing");
            var duplicate = CreateStep("intro");
            SetPrivateField(authoring, "steps", new List<TutorialStepAuthoring> { first, duplicate });

            TutorialValidationContext result = authoring.ValidateDefinition();

            Assert.IsFalse(result.IsValid);
            Assert.That(result.Errors, Has.Some.Contains("Duplicate tutorial step id 'intro'."));
            Assert.That(result.Errors, Has.Some.Contains("unknown next step id 'missing'."));
        }

        [Test]
        public void CaptureSnapshot_ReturnsCompletedAfterFinalToDoCompletes()
        {
            SceneKernel kernel = new SceneKernel
            {
                EntityValueStore = new ValueStoreService(),
            };

            TutorialStageAuthoringMB authoring = CreateAuthoring();
            UITutorialToDoListMB todoList = CreateToDoList();
            var condition = new TestConditionAuthoring();
            var step = CreateStep("move", todoEntries: new List<TutorialToDoEntryAuthoring>
            {
                CreateToDo("Move", condition),
            });
            SetPrivateField(authoring, "steps", new List<TutorialStepAuthoring> { step });

            TutorialRuntimeService service = new TutorialRuntimeService(kernel);
            EntityRef player = new EntityRef(1, 1);

            bool started = service.Start(authoring, player, todoList);
            Assert.IsTrue(started);

            Assert.IsNotNull(condition.Runtime);
            condition.Runtime.Complete();

            TutorialProgressSnapshot snapshot = service.CaptureSnapshot();
            Assert.IsTrue(snapshot.IsValid);
            Assert.IsTrue(snapshot.IsCompleted);
        }

        private TutorialStageAuthoringMB CreateAuthoring()
        {
            GameObject gameObject = new GameObject("TutorialAuthoring");
            createdObjects.Add(gameObject);
            return gameObject.AddComponent<TutorialStageAuthoringMB>();
        }

        private UITutorialToDoListMB CreateToDoList()
        {
            GameObject gameObject = new GameObject("TutorialToDoList");
            createdObjects.Add(gameObject);
            return gameObject.AddComponent<UITutorialToDoListMB>();
        }

        private static TutorialStepAuthoring CreateStep(
            string stepId,
            string nextStepId = null,
            List<TutorialToDoEntryAuthoring> todoEntries = null)
        {
            var step = new TutorialStepAuthoring();
            SetPrivateField(step, "stepId", new TutorialStepId(stepId));

            if (!string.IsNullOrWhiteSpace(nextStepId))
                SetPrivateField(step, "nextStepId", new TutorialStepId(nextStepId));

            if (todoEntries != null)
                SetPrivateField(step, "todoEntries", todoEntries);

            return step;
        }

        private static TutorialToDoEntryAuthoring CreateToDo(string label, TutorialConditionAuthoring condition)
        {
            var entry = new TutorialToDoEntryAuthoring();
            SetPrivateField(entry, "labelText", label);
            SetPrivateField(entry, "condition", condition);
            return entry;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        [Serializable]
        private sealed class TestConditionAuthoring : TutorialConditionAuthoring
        {
            public TestConditionRuntime Runtime { get; private set; }

            public override ITutorialConditionRuntime CreateRuntime()
            {
                Runtime = new TestConditionRuntime();
                return Runtime;
            }
        }

        private sealed class TestConditionRuntime : ITutorialConditionRuntime
        {
            public event Action Completed;
            public bool IsCompleted { get; private set; }

            public void Start(in TutorialConditionContext context, object restoredState)
            {
            }

            public void Tick(float deltaTime)
            {
            }

            public object CaptureState()
            {
                return null;
            }

            public void Complete()
            {
                IsCompleted = true;
                Completed?.Invoke();
            }

            public void Dispose()
            {
            }
        }
    }
}
