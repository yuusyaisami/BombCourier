using BC.Base;
using BC.Camera;
using BC.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveController : EntityMoveController
{
    public StateMachine<EntityMoveState> stateMachine { get; private set; } = new StateMachine<EntityMoveState>();
    public SceneKernel SceneKernel { get; private set; }
    public EntityRef Entity { get; private set; }


    private Vector2 targetVelocity;

    private CharacterController _characterController;
    private ICameraController _cameraController;

    public InputActionReference MoveInputAction;
    public InputActionReference JumpInputAction;
    public InputActionReference SprintInputAction;

    private void Start()
    {
        // 初期化
        SceneKernel = GetComponentInParent<SceneKernel>();
        EntityMB entity = GetComponent<EntityMB>();
        Entity = entity.Entity;

        // 初期状態をIdleに設定
        stateMachine.ChangeState(EntityMoveState.Idle);

        // CharacterControllerの取得
        _characterController = GetComponentInChildren<CharacterController>();

        // カメラコントローラーの取得
        _cameraController = GetComponentInChildren<ThirdPersonCameraController>();
    }

    public void Move(Vector3 direction, bool isSprinting)
    {
        if (!IsActive) return;

        // 移動処理
        float speed = SceneKernel.ValueStore.Get<float>(Entity, ValueKeys.Move.BaseSpeed);
        if (isSprinting)
        {
            float sprintMul = SceneKernel.ValueStore.Get<float>(Entity, ValueKeys.Move.SprintMul);
            speed *= sprintMul;
        }

        Vector3 velocity = direction.normalized * speed * Time.deltaTime;

        // 移動速度をなめらかにするための補間
        targetVelocity = Vector3.Lerp(targetVelocity, velocity, 0.1f);
        _characterController.Move(targetVelocity);

        // 状態遷移
        if (direction.magnitude > 0.1f)
        {
            stateMachine.ChangeState(EntityMoveState.Moving);
        }
        else
        {
            stateMachine.ChangeState(EntityMoveState.Idle);
        }
    }


}
