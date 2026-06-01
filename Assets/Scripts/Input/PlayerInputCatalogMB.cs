using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Inputs
{
    // 全プレイヤー操作の InputActionReference を一か所にまとめたカタログ。
    // OperationGuide 等がアクション ID から InputActionReference を引くために使う。
    // 既存の PlayerMoveController 等には変更を加えない。
    public sealed class PlayerInputCatalogMB : MonoBehaviour
    {
        [SerializeField] private List<PlayerInputEntry> entries = new();

        public bool TryGetAction(string id, out InputActionReference action)
        {
            action = null;
            if (string.IsNullOrEmpty(id)) return false;

            foreach (PlayerInputEntry entry in entries)
            {
                if (entry.Id == id && entry.Action != null)
                {
                    action = entry.Action;
                    return true;
                }
            }
            return false;
        }
    }

    [Serializable]
    public sealed class PlayerInputEntry
    {
        [SerializeField] private string id;
        [SerializeField] private InputActionReference action;

        public string Id => id;
        public InputActionReference Action => action;
    }

    // 既定のアクション ID 定数。PlayerInputCatalogMB の entries の id と合わせる。
    public static class PlayerInputCatalog
    {
        public static class Ids
        {
            public const string Move      = "Move";
            public const string Jump      = "Jump";
            public const string Sprint    = "Sprint";
            public const string CarryThrow = "CarryThrow";
            public const string Interact  = "Interact";
        }
    }
}
