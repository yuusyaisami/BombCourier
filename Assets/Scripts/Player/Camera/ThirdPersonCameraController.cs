using BC.Base;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BC.Camera
{
    public interface ICameraController
    {
        Quaternion GetYawRotation();
        Quaternion GetPitchRotation();
    }
    public sealed class ThirdPersonCameraController : MonoBehaviour, ICameraController
    {
        [Header("Target")]
        [SerializeField] private Transform cameraTarget;

        [Header("Input")]
        [SerializeField] private InputActionReference lookAction;

        [Header("Sensitivity")]
        [SerializeField] private float mouseSensitivity = 0.08f;
        [SerializeField] private float gamepadSensitivity = 120f;

        [Header("Pitch Limit")]
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;

        [Header("Options")]
        [SerializeField] private bool invertY;

        [Header("Throw Camera")]
        [SerializeField] private Vector3 throwShoulderOffset = new(0.85f, 0.15f, 0.0f);
        [SerializeField, Min(0.0f)] private float throwShoulderOffsetBlendSharpness = 12.0f;

        [Header("Runtime Debug")]
        [SerializeField] private bool currentCanLookByInput = true;

        private float yaw;
        private float pitch;
        private ValueStoreService valueStore;
        private EntityRef entityRef;
        private ValueWatchHandle<bool> canLookByInputHandle;

        public Transform CameraTarget => cameraTarget;
        public Vector3 ThrowShoulderOffset => throwShoulderOffset;
        public float ThrowShoulderOffsetBlendSharpness => throwShoulderOffsetBlendSharpness;
        public bool CanLookByInput => currentCanLookByInput;

        private void Reset()
        {
            cameraTarget = transform;
        }

        private void Awake()
        {
            if (cameraTarget == null)
            {
                Debug.LogError($"{nameof(ThirdPersonCameraController)}: cameraTarget is null.", this);
                enabled = false;
                return;
            }

            Vector3 euler = cameraTarget.rotation.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);
            ApplyCameraOrientation();
        }

        private void OnEnable()
        {
            lookAction?.action?.Enable();
            RegisterCameraTarget();
        }

        private void Update()
        {
            RefreshLookGateDebugValue();

            if (!currentCanLookByInput)
                return;

            if (lookAction == null || lookAction.action == null)
                return;

            Vector2 look = lookAction.action.ReadValue<Vector2>();

            bool isMouse = Mouse.current != null && Mouse.current.delta.ReadValue() == look;

            float deltaYaw;
            float deltaPitch;

            if (isMouse)
            {
                deltaYaw = look.x * mouseSensitivity;
                deltaPitch = look.y * mouseSensitivity;
            }
            else
            {
                deltaYaw = look.x * gamepadSensitivity * Time.deltaTime;
                deltaPitch = look.y * gamepadSensitivity * Time.deltaTime;
            }

            yaw += deltaYaw;

            if (invertY)
                pitch += deltaPitch;
            else
                pitch -= deltaPitch;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            ApplyCameraOrientation();
        }

        public Quaternion GetYawRotation()
        {
            return Quaternion.Euler(0f, yaw, 0f);
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;

            if (angle > 180f)
                angle -= 360f;

            return angle;
        }

        public Quaternion GetPitchRotation()
        {
            return Quaternion.Euler(pitch, 0f, 0f);
        }

        private void ApplyCameraOrientation()
        {
            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void ResolveRuntimeReferences()
        {
            if (valueStore == null)
            {
                SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

                if (kernelMB != null && kernelMB.Kernel != null)
                    valueStore = kernelMB.Kernel.ValueStore;
            }

            if (!entityRef.IsValid)
            {
                EntityMB entityMB = GetComponentInParent<EntityMB>();

                if (entityMB != null && entityMB.HasEntity)
                    entityRef = entityMB.Entity;
            }

            if (canLookByInputHandle == null && valueStore != null && entityRef.IsValid)
                canLookByInputHandle = valueStore.GetHandle(entityRef, ValueKeys.Camera.CanLookByInput);
        }

        private void RefreshLookGateDebugValue()
        {
            ResolveRuntimeReferences();

            currentCanLookByInput = canLookByInputHandle == null || canLookByInputHandle.CurrentValue;

            if (valueStore != null && entityRef.IsValid)
                valueStore.Set(entityRef, ValueKeys.Runtime.CanLookByInput, currentCanLookByInput);
        }

        private void RegisterCameraTarget()
        {
            if (cameraTarget == null)
                return;

            CameraManager.Instance?.RegisterThirdPersonTarget(cameraTarget);
        }

        private void OnDisable()
        {
            lookAction?.action?.Disable();

            CameraManager.Instance?.UnregisterThirdPersonTarget(cameraTarget);
        }
    }
}