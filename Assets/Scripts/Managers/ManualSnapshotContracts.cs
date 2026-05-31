using System;

namespace BC.Manager
{
    public enum RetryCheckpointSourceKind
    {
        BombPickup,
        Manual,
    }

    [Flags]
    public enum ManualSnapshotBlockReason
    {
        None = 0,
        MissingGameStateManager = 1 << 0,
        GameStateNotSetupPlaying = 1 << 1,
        MissingPlayer = 1 << 2,
        MissingSceneKernel = 1 << 3,
        MissingStageManager = 1 << 4,
        PlayerCannotMoveByInput = 1 << 5,
        PlayerCannotInteract = 1 << 6,
        BombStateDirty = 1 << 7,
        HoldingItem = 1 << 8,
        ShuttingDown = 1 << 9,
    }

    public readonly struct ManualSnapshotAvailability
    {
        private const ManualSnapshotBlockReason HiddenUiMask =
            ManualSnapshotBlockReason.MissingGameStateManager |
            ManualSnapshotBlockReason.GameStateNotSetupPlaying |
            ManualSnapshotBlockReason.MissingPlayer |
            ManualSnapshotBlockReason.MissingSceneKernel |
            ManualSnapshotBlockReason.MissingStageManager |
            ManualSnapshotBlockReason.ShuttingDown;

        public ManualSnapshotAvailability(ManualSnapshotBlockReason blockReasons)
        {
            BlockReasons = blockReasons;
        }

        public ManualSnapshotBlockReason BlockReasons { get; }
        public bool CanCapture => BlockReasons == ManualSnapshotBlockReason.None;
        public bool ShouldShowUi => (BlockReasons & HiddenUiMask) == 0;
        public bool ShouldShowUnavailableOverlay => ShouldShowUi && !CanCapture;
    }
}
