using BC.Base;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BC.Camera
{
    public interface ICameraController
    {
        Quaternion GetYawRotation();
        Quaternion GetPitchRotation();
        float GetYawAngle();
        float GetPitchAngle();
    }
    public sealed class ThirdPersonCameraController : MonoBehaviour, ICameraController
    {
        private const string ExpectedLookActionName = "Look";
        private const string KeyCameraSensitivity = "Settings.CameraSensitivity";
        private const string KeyInvertYAxis = "Settings.InvertYAxis";

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
        private InputAction boundLookInputAction;
        private InputDevice lastLookInputDevice;
        // ApplicationKernel.KernelValueStore 経由で Setting 画面から書き込まれる感度・反転設定。
        // null の場合は Inspector のフォールバック値を使う。
        private ValueWatchHandle<float> appSensitivityHandle;
        private ValueWatchHandle<bool> appInvertYHandle;

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
            BindLookInputAction();
            RegisterCameraTarget();
        }

        private void Update()
        {
            RefreshLookGateDebugValue();

            if (!currentCanLookByInput)
                return;

            InputAction resolvedLookAction = ResolveLookInputAction();
            if (resolvedLookAction == null)
                return;

            Vector2 look = resolvedLookAction.ReadValue<Vector2>();

            bool isPointerInput = IsPointerLookDevice(ResolveLookInputDevice(resolvedLookAction));

            float deltaYaw;
            float deltaPitch;

            float effectiveSensitivity = appSensitivityHandle != null
                ? appSensitivityHandle.CurrentValue
                : PlayerPrefs.GetFloat(KeyCameraSensitivity, mouseSensitivity);
            bool effectiveInvertY = appInvertYHandle != null
                ? appInvertYHandle.CurrentValue
                : PlayerPrefs.GetInt(KeyInvertYAxis, invertY ? 1 : 0) == 1;

            if (isPointerInput)
            {
                deltaYaw = look.x * effectiveSensitivity;
                deltaPitch = look.y * effectiveSensitivity;
            }
            else
            {
                deltaYaw = look.x * gamepadSensitivity * Time.deltaTime;
                deltaPitch = look.y * gamepadSensitivity * Time.deltaTime;
            }

            yaw += deltaYaw;

            if (effectiveInvertY)
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

        public float GetYawAngle()
        {
            return yaw;
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

        public float GetPitchAngle()
        {
            return pitch;
        }

        // 会話 camera など別 rig が同じ look state を使うときの明示同期入口です。
        // yaw/pitch の正規化と pitch clamp をここに閉じ込め、外側が独自実装を持たないようにします。
        public void SetLookAngles(float nextYaw, float nextPitch)
        {
            yaw = NormalizeAngle(nextYaw);
            pitch = Mathf.Clamp(nextPitch, minPitch, maxPitch);
            ApplyCameraOrientation();
        }

        public void SyncYawToWorldForward(Vector3 worldForward)
        {
            worldForward.y = 0f;

            if (worldForward.sqrMagnitude <= 0.0001f)
                return;

            float targetYaw = Mathf.Atan2(worldForward.x, worldForward.z) * Mathf.Rad2Deg;
            SetLookAngles(targetYaw, pitch);
        }

        // スポーン時など、3D のワールド向きから yaw と pitch を計算して camera を即時初期化する。
        // カメラ上方向（y = 1）は minPitch でクランプされる。既存 SetLookAngles でクランプ済み。
        public void SetLookDirection(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude <= 0.0001f)
                return;

            Vector3 dir = worldDirection.normalized;
            float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            // Unity の pitch 正方向は下向きなので、仰角（y が正 = 上向き）は符号反転する。
            float elevationDeg = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            SetLookAngles(targetYaw, -elevationDeg);
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

            // AppSettings ハンドルは ApplicationKernel から一度だけ取得する。
            ApplicationKernelMB appKernelMB = ApplicationKernelMB.Instance;
            if (appKernelMB == null)
                appKernelMB = UnityEngine.Object.FindAnyObjectByType<ApplicationKernelMB>();

            KernelValueStoreService appStore = appKernelMB != null
                ? appKernelMB.Kernel?.KernelValueStore
                : null;
            if (appStore != null)
            {
                if (appSensitivityHandle == null)
                    appSensitivityHandle = appStore.GetHandle(ValueKeys.AppSettings.CameraSensitivity);
                if (appInvertYHandle == null)
                    appInvertYHandle = appStore.GetHandle(ValueKeys.AppSettings.InvertYAxis);
            }
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
            UnbindLookInputAction();

            CameraManager.Instance?.UnregisterThirdPersonTarget(cameraTarget);
        }

        private void BindLookInputAction()
        {
            InputAction resolvedAction = ResolveLookInputAction();
            if (resolvedAction == null)
                return;

            if (boundLookInputAction == resolvedAction)
            {
                boundLookInputAction.Enable();
                return;
            }

            UnbindLookInputAction();
            boundLookInputAction = resolvedAction;
            boundLookInputAction.performed += HandleLookInputPerformed;
            boundLookInputAction.started += HandleLookInputPerformed;
            boundLookInputAction.Enable();
        }

        private void UnbindLookInputAction()
        {
            if (boundLookInputAction == null)
                return;

            boundLookInputAction.performed -= HandleLookInputPerformed;
            boundLookInputAction.started -= HandleLookInputPerformed;
            boundLookInputAction.Disable();
            boundLookInputAction = null;
            lastLookInputDevice = null;
        }

        private void HandleLookInputPerformed(InputAction.CallbackContext context)
        {
            InputDevice device = context.control != null ? context.control.device : null;
            if (device != null)
                lastLookInputDevice = device;
        }

        private InputDevice ResolveLookInputDevice(InputAction action)
        {
            return action != null && action.activeControl != null
                ? action.activeControl.device
                : lastLookInputDevice;
        }

        private static bool IsPointerLookDevice(InputDevice device)
        {
            return device is Pointer;
        }

        // 参照が崩れても Look 以外を読まないよう、同じ asset 内から期待アクション名を引き直す。
        private InputAction ResolveLookInputAction()
        {
            InputAction assignedAction = lookAction != null ? lookAction.action : null;
            if (assignedAction == null)
                return null;

            if (assignedAction.name == ExpectedLookActionName)
                return assignedAction;

            InputActionAsset actionAsset = assignedAction.actionMap != null ? assignedAction.actionMap.asset : null;
            return actionAsset != null
                ? actionAsset.FindAction($"Player/{ExpectedLookActionName}", throwIfNotFound: false)
                : assignedAction;
        }
    }
}
