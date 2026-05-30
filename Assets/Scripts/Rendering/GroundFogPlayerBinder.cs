using BC.Base;
using BC.Manager;
using BC.Player;
using UnityEngine;

public sealed class GroundFogPlayerBinder : MonoBehaviour
{
    [SerializeField] private Renderer[] fogRenderers = System.Array.Empty<Renderer>();
    [SerializeField] private Transform fallbackPlayerTransform;

    private MaterialPropertyBlock propertyBlock;
    private PlayerMB player;
    private bool isBoundToGameLogic;

    private static readonly int PlayerPositionId =
        Shader.PropertyToID("_PlayerPos");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        CacheRenderersIfNeeded();
    }

    private void OnEnable()
    {
        TryBindToGameLogic();
    }

    private void Update()
    {
        TryBindToGameLogic();
    }

    private void OnDisable()
    {
        UnbindFromGameLogic();
    }

    private void LateUpdate()
    {
        Transform target = ResolveTargetTransform();
        if (target == null || fogRenderers == null || fogRenderers.Length == 0)
            return;

        Vector3 playerPosition = target.position;

        for (int i = 0; i < fogRenderers.Length; i++)
        {
            Renderer targetRenderer = fogRenderers[i];
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetVector(PlayerPositionId, playerPosition);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void TryBindToGameLogic()
    {
        if (isBoundToGameLogic)
            return;

        GameLogicManagerMB gameLogicManager = GameLogicManagerMB.Instance;
        if (gameLogicManager == null)
            return;

        gameLogicManager.OnPlayerUpdated -= HandlePlayerUpdated;
        gameLogicManager.OnPlayerUpdated += HandlePlayerUpdated;
        HandlePlayerUpdated(gameLogicManager.PlayerInstance);
        isBoundToGameLogic = true;
    }

    private void UnbindFromGameLogic()
    {
        GameLogicManagerMB gameLogicManager = GameLogicManagerMB.Instance;
        if (gameLogicManager == null)
        {
            isBoundToGameLogic = false;
            return;
        }

        gameLogicManager.OnPlayerUpdated -= HandlePlayerUpdated;
        isBoundToGameLogic = false;
    }

    private void HandlePlayerUpdated(PlayerMB updatedPlayer)
    {
        player = updatedPlayer;
    }

    public void SetPlayerTransform(Transform playerTransform)
    {
        fallbackPlayerTransform = playerTransform;
    }

    private Transform ResolveTargetTransform()
    {
        if (player != null)
            return player.transform;

        return fallbackPlayerTransform;
    }

    private void CacheRenderersIfNeeded()
    {
        if (fogRenderers != null && fogRenderers.Length > 0)
            return;

        fogRenderers = GetComponentsInChildren<Renderer>(true);
    }
}