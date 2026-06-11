using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace BC.Base
{
    // ライフタイムインターフェース
    public interface ITickable
    {
        void Tick(float deltaTime);
    }

    public interface IKernelInstaller
    {
        int Order { get; }
        void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel;
    }

    public sealed class KernelBuilder
    {
        public TKernel Build<TKernel>(GameObject[] obj) where TKernel : BaseKernel, new()
        {
            var kernel = new TKernel();

            // installer は Build ごとに収集し直す。インスタンスフィールドに持つと
            // 複数回 Build した際に前回分が累積する潜在バグになるため、ローカルに閉じる。
            var installers = new List<IKernelInstaller>();
            foreach (var go in obj)
            {
                // targetObjects に空スロットが残っていても bootstrap 全体を NRE で止めない。
                // 未設定参照は silent skip にせず、明示的にエラー化してから飛ばす。
                if (go == null)
                {
                    Debug.LogError($"{nameof(KernelBuilder)}: null GameObject in installer targets was skipped. Fix the kernel's targetObjects list.");
                    continue;
                }

                installers.AddRange(go.GetComponents<IKernelInstaller>());
            }

            // Order 昇順で初期化する。OrderBy は安定ソートなので、同一 Order の installer
            // (例: EventMB と ScopedEntityRegistryMB はともに Order = -5) の相対順序が宣言順で固定され、
            // List.Sort の不安定さに由来する初期化順の揺れ (heisenbug) を防ぐ。
            foreach (var installer in installers.OrderBy(installer => installer.Order))
            {
                installer.Setup<TKernel>(kernel);
            }

            return kernel;
        }
    }
}