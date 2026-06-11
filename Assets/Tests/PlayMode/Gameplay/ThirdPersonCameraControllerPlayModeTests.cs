using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class ThirdPersonCameraControllerPlayModeTests
    {
        private const string ThirdPersonCameraControllerTypeName = "BC.Camera.ThirdPersonCameraController";
        private readonly List<InputDevice> createdDevices = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdDevices.Count - 1; i >= 0; i--)
            {
                InputDevice device = createdDevices[i];
                if (device != null && device.added)
                    InputSystem.RemoveDevice(device);
            }

            createdDevices.Clear();
        }

        [Test]
        public void LookDevicePolicyTreatsPointerAsMouseSensitivityAndGamepadAsGamepadSensitivity()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            createdDevices.Add(mouse);
            createdDevices.Add(gamepad);

            Type controllerType = FindRuntimeType(ThirdPersonCameraControllerTypeName);
            MethodInfo method = controllerType.GetMethod("IsPointerLookDevice", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Expected ThirdPersonCameraController to keep the device classification policy explicit.");

            Assert.IsTrue((bool)method.Invoke(null, new object[] { mouse }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { gamepad }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { null }));
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
    }
}
