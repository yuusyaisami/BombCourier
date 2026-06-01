using System.Collections.Generic;
using UnityEngine;

namespace BC.UI
{
    // チュートリアル ToDoList の定義データ。
    // UITutorialToDoListMB に渡してリストを生成する。
    [CreateAssetMenu(fileName = "TutorialToDoList", menuName = "BombCourier/Tutorial/ToDoList")]
    public sealed class TutorialToDoListSO : ScriptableObject
    {
        [SerializeField] private List<TutorialToDoItemDefinition> items = new();

        public IReadOnlyList<TutorialToDoItemDefinition> Items => items;
    }
}
