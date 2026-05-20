using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class UIPlayerInteractionPromptPlayModeTests
    {
        private const string UIPlayerInteractionPromptTypeName = "BC.UI.UIPlayerInteractionPromptMB";
        private const string PlayerItemHandleStateTypeName = "BC.Player.PlayerItemHandleStateMB";
        private const string CarryableItemInteractableAdapterTypeName = "BC.Player.CarryableItemInteractableAdapter";
        private const string InteractionPromptDetailTextTypeName = "BC.Player.InteractionPromptDetailTextMB";
        private const string InteractionPromptResolverTypeName = "BC.Player.InteractionPromptResolver";
        private const string BombTypeName = "BC.Bomb.BombMB";
        private const string CushionTypeName = "BC.Gimmick.Cushion.CushionMB";

        private readonly List<UnityEngine.Object> createdObjects = new();
        private readonly List<InputAction> createdActions = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdActions.Count - 1; i >= 0; i--)
                createdActions[i]?.Dispose();

            createdActions.Clear();

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator BombInteractable_DisplaysCountdownDetailText()
        {
            LogAssert.Expect(LogType.Warning, "PlayerItemHandleStateMB: EntityMB is not found or not bound.");

            PlayerHandleFixture playerFixture = CreatePlayerHandleFixture();
            Component bomb = CreateBombItem("PromptBomb", new Vector3(0f, 0f, 0f), fuseTime: 8f, remainingFuseTime: 5.2f);
            PromptFixture promptFixture = CreatePromptFixture(playerFixture.HandleState);

            yield return null;

            SetCurrentBestInteractable(playerFixture.Controller, CreateCarryableAdapter(bomb));
            InvokeMethod(promptFixture.Prompt, "LateUpdate");

            Assert.AreEqual("6s", promptFixture.DetailText.text);
            Assert.IsTrue(promptFixture.DetailText.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator SwitchingToNonBomb_ClearsExistingDetailText()
        {
            LogAssert.Expect(LogType.Warning, "PlayerItemHandleStateMB: EntityMB is not found or not bound.");

            PlayerHandleFixture playerFixture = CreatePlayerHandleFixture();
            Component bomb = CreateBombItem("PromptBomb", new Vector3(0f, 0f, 0f), fuseTime: 6f, remainingFuseTime: 2.1f);
            Component nonBomb = CreateCarryItem("PromptCushion", new Vector3(0f, 0f, 0f));
            PromptFixture promptFixture = CreatePromptFixture(playerFixture.HandleState);

            yield return null;

            SetCurrentBestInteractable(playerFixture.Controller, CreateCarryableAdapter(bomb));
            InvokeMethod(promptFixture.Prompt, "LateUpdate");

            Assert.AreEqual("3s", promptFixture.DetailText.text);
            Assert.IsTrue(promptFixture.DetailText.gameObject.activeSelf);

            SetCurrentBestInteractable(playerFixture.Controller, CreateCarryableAdapter(nonBomb));
            InvokeMethod(promptFixture.Prompt, "LateUpdate");

            Assert.AreEqual(string.Empty, promptFixture.DetailText.text);
            Assert.IsFalse(promptFixture.DetailText.gameObject.activeSelf);
        }

        [Test]
        public void StaticItemDetailTextComponent_ResolvesConfiguredText()
        {
            Component nonBomb = CreateCarryItem("PromptCushion", new Vector3(0f, 0f, 0f));
            Component detailTextComponent = nonBomb.gameObject.AddComponent(FindRuntimeType(InteractionPromptDetailTextTypeName));

            SetPrivateField(detailTextComponent, "promptDetailText", "Fragile");

            string detailText = InvokeStaticMethod<string>(
                FindRuntimeType(InteractionPromptResolverTypeName),
                "ResolveDetailText",
                CreateCarryableAdapter(nonBomb));

            Assert.AreEqual("Fragile", detailText);
        }

        private PlayerHandleFixture CreatePlayerHandleFixture()
        {
            GameObject root = new GameObject("PlayerItemHandleStateTestRoot");
            root.SetActive(false);
            createdObjects.Add(root);

            GameObject handlePointObject = new GameObject("HandlePoint");
            handlePointObject.transform.SetParent(root.transform, false);
            handlePointObject.transform.localPosition = new Vector3(0f, 1f, 0.6f);

            GameObject playerModelObject = new GameObject("PlayerModel");
            playerModelObject.transform.SetParent(root.transform, false);
            playerModelObject.transform.localRotation = Quaternion.identity;

            Component handleState = root.AddComponent(FindRuntimeType(PlayerItemHandleStateTypeName));
            InputActionAsset inputActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            createdObjects.Add(inputActionAsset);

            InputActionMap gameplayMap = new InputActionMap("Gameplay");
            inputActionAsset.AddActionMap(gameplayMap);

            InputAction handleAction = gameplayMap.AddAction("Handle", InputActionType.Button);
            createdActions.Add(handleAction);
            InputActionReference handleActionReference = InputActionReference.Create(handleAction);
            createdObjects.Add(handleActionReference);

            SetPrivateField(handleState, "handleItemPoint", handlePointObject.transform);
            SetPrivateField(handleState, "playerModel", playerModelObject);
            SetPrivateField(handleState, "handleItemAction", handleActionReference);

            root.SetActive(true);

            return new PlayerHandleFixture
            {
                HandleState = handleState,
                Controller = GetPrivateField<object>(handleState, "interactionController"),
            };
        }

        private PromptFixture CreatePromptFixture(Component handleState)
        {
            GameObject cameraObject = new GameObject("PromptCamera");
            cameraObject.tag = "MainCamera";
            Camera worldCamera = cameraObject.AddComponent<Camera>();
            worldCamera.transform.position = new Vector3(0f, 0f, -10f);
            createdObjects.Add(cameraObject);

            GameObject canvasObject = new GameObject("PromptCanvas", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdObjects.Add(canvasObject);

            GameObject promptObject = new GameObject("InteractionPrompt", typeof(RectTransform), typeof(CanvasGroup));
            promptObject.SetActive(false);
            promptObject.transform.SetParent(canvasObject.transform, false);
            RectTransform promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.sizeDelta = new Vector2(200f, 120f);

            GameObject iconObject = new GameObject("ActionIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(promptObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(48f, 48f);
            Image iconImage = iconObject.GetComponent<Image>();

            GameObject detailTextObject = new GameObject("ActionDetailText", typeof(RectTransform), typeof(TextMeshProUGUI));
            detailTextObject.transform.SetParent(promptObject.transform, false);
            RectTransform detailTextRect = detailTextObject.GetComponent<RectTransform>();
            detailTextRect.anchoredPosition = new Vector2(0f, -32f);
            detailTextRect.sizeDelta = new Vector2(180f, 32f);

            TextMeshProUGUI detailText = detailTextObject.GetComponent<TextMeshProUGUI>();
            detailText.alignment = TextAlignmentOptions.Center;
            detailText.text = string.Empty;
            detailText.gameObject.SetActive(false);

            Component prompt = promptObject.AddComponent(FindRuntimeType(UIPlayerInteractionPromptTypeName));
            SetPrivateField(prompt, "resolvePlayerFromGameLogic", false);
            SetPrivateField(prompt, "interactionSource", handleState);
            SetPrivateField(prompt, "worldCamera", worldCamera);
            SetPrivateField(prompt, "canvas", canvas);
            SetPrivateField(prompt, "root", promptRect);
            SetPrivateField(prompt, "canvasGroup", promptObject.GetComponent<CanvasGroup>());
            SetPrivateField(prompt, "actionIconImage", iconImage);
            SetPrivateField(prompt, "actionDetailText", detailText);

            promptObject.SetActive(true);

            return new PromptFixture
            {
                Prompt = prompt,
                DetailText = detailText,
            };
        }

        private Component CreateBombItem(string name, Vector3 position, float fuseTime, float remainingFuseTime)
        {
            GameObject itemObject = new GameObject(name);
            itemObject.SetActive(false);
            itemObject.transform.position = position;
            createdObjects.Add(itemObject);

            SphereCollider collider = itemObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            Rigidbody rigidbody = itemObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            Component bomb = itemObject.AddComponent(FindRuntimeType(BombTypeName));
            SetPrivateField(bomb, "startFuseOnHandle", false);
            SetPrivateField(bomb, "fuseTime", fuseTime);

            itemObject.SetActive(true);

            SetPrivateField(bomb, "fuseStarted", true);
            SetPrivateField(bomb, "remainingFuseTime", remainingFuseTime);
            return bomb;
        }

        private Component CreateCarryItem(string name, Vector3 position)
        {
            GameObject itemObject = new GameObject(name);
            itemObject.transform.position = position;
            createdObjects.Add(itemObject);

            SphereCollider collider = itemObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            Rigidbody rigidbody = itemObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            return itemObject.AddComponent(FindRuntimeType(CushionTypeName));
        }

        private static object CreateCarryableAdapter(Component carryableItem)
        {
            return Activator.CreateInstance(
                FindRuntimeType(CarryableItemInteractableAdapterTypeName),
                carryableItem,
                carryableItem);
        }

        private static void SetCurrentBestInteractable(object controller, object interactable)
        {
            SetPrivateField(controller, "currentBestInteractable", interactable);
            SetPrivateField<object>(controller, "activeInteractable", null);
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

        private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }

        private static T InvokeStaticMethod<T>(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method on {type.Name}: {methodName}");
            return (T)method.Invoke(null, args);
        }

        private sealed class PlayerHandleFixture
        {
            public Component HandleState { get; set; }
            public object Controller { get; set; }
        }

        private sealed class PromptFixture
        {
            public Component Prompt { get; set; }
            public TextMeshProUGUI DetailText { get; set; }
        }
    }
}