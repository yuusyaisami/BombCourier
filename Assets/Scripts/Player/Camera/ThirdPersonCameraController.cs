using BC.Base;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
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
        [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;
        [SerializeField] private Vector3 throwShoulderOffset = new(0.85f, 0.15f, 0.0f);
        [SerializeField, Min(0.0f)] private float throwShoulderOffsetBlendSharpness = 12.0f;

        private float yaw;
        private float pitch;
        private ValueStoreService valueStore;
        private EntityRef entityRef;
        private ValueWatchHandle<bool> throwPoseHandle;
        private Vector3 defaultShoulderOffset;
        private bool hasDefaultShoulderOffset;

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
        }

        private void OnEnable()
        {
            lookAction?.action?.Enable();
        }

        private void Update()
        {
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

            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
            UpdateThrowShoulderOffset();
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

        private void UpdateThrowShoulderOffset()
        {
            ResolveRuntimeReferences();
            ResolveThirdPersonFollow();

            if (thirdPersonFollow == null)
                return;

            if (!hasDefaultShoulderOffset)
            {
                defaultShoulderOffset = thirdPersonFollow.ShoulderOffset;
                hasDefaultShoulderOffset = true;
            }

            Vector3 targetOffset = IsThrowPoseActive()
                ? throwShoulderOffset
                : defaultShoulderOffset;

            float t = throwShoulderOffsetBlendSharpness <= 0.0f
                ? 1.0f
                : 1.0f - Mathf.Exp(-throwShoulderOffsetBlendSharpness * Time.deltaTime);

            thirdPersonFollow.ShoulderOffset = Vector3.Lerp(
                thirdPersonFollow.ShoulderOffset,
                targetOffset,
                t);
        }

        private bool IsThrowPoseActive()
        {
            if (throwPoseHandle == null)
                return false;

            return throwPoseHandle.CurrentValue;
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

            if (throwPoseHandle == null && valueStore != null && entityRef.IsValid)
                throwPoseHandle = valueStore.GetHandle(entityRef, ValueKeys.Runtime.IsThrowPoseActive);
        }

        private void ResolveThirdPersonFollow()
        {
            if (thirdPersonFollow != null || cameraTarget == null)
                return;

            CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>();

            for (int i = 0; i < cameras.Length; i++)
            {
                CinemachineCamera candidate = cameras[i];

                if (candidate == null || candidate.Follow != cameraTarget)
                    continue;

                thirdPersonFollow = candidate.GetComponent<CinemachineThirdPersonFollow>();

                if (thirdPersonFollow != null)
                    return;
            }
        }

        private void OnDisable()
        {
            lookAction?.action?.Disable();

            if (thirdPersonFollow != null && hasDefaultShoulderOffset)
                thirdPersonFollow.ShoulderOffset = defaultShoulderOffset;
        }
    }
}