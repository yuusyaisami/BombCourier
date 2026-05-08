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

        private float yaw;
        private float pitch;

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

        private void OnDisable()
        {
            lookAction?.action?.Disable();
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
    }
}