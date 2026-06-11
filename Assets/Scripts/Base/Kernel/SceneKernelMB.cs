using System.Collections.Generic;
using UnityEngine;
namespace BC.Base
{
    // SceneKernel を生成し、Unity の MonoBehaviour ライフサイクルに接続する入口。
    public class SceneKernelMB : MonoBehaviour
    {
        private SceneKernel kernel;
        // Authoring 対象となる root objects の一覧。
        public List<GameObject> targetObjects; // Authoring対象者のObjectたち基本は自分だけ
        public SceneKernel Kernel => kernel;

        private void Awake()
        {
            KernelBuilder kernelBuilder = new KernelBuilder();
            kernel = kernelBuilder.Build<SceneKernel>(targetObjects.ToArray());
            EnsureCoreServices();

            // 最初からシーン内に存在するEntityを登録するためのブートストラッパーを作成して実行
            var bootstrapper = new SceneEntityBootstrapper(kernel, transform);
            bootstrapper.RegisterSceneEntities();
        }

        private void EnsureCoreServices()
        {
            if (kernel == null)
            {
                Debug.LogError($"{nameof(SceneKernelMB)}: failed to build {nameof(SceneKernel)}.", this);
                return;
            }

            // ValueStoreMB 未配線でも scene 構築を止めないための degraded fallback。
            // 失敗を隠すためではなく、警告つきで明示的に続行する domain contract として許容する。
            // 本番配線では ValueStoreMB を必ず置き、この警告が出たら配線漏れとして扱う。
            if (kernel.EntityValueStore == null)
            {
                kernel.EntityValueStore = new ValueStoreService();
                Debug.LogWarning($"{nameof(SceneKernelMB)}: {nameof(SceneKernel.EntityValueStore)} was not installed. Auto-created fallback store. Add {nameof(ValueStoreMB)} to targetObjects for explicit wiring.", this);
            }

            if (kernel.KernelValueStore == null)
            {
                // Scene scope の ReactiveValue / Action write は KernelValueStore を要求する。
                // EntityValueStore へ代替しないことで、scope 指定ミスを隠さず警告として残す。
                kernel.KernelValueStore = new KernelValueStoreService();
                Debug.LogWarning($"{nameof(SceneKernelMB)}: {nameof(SceneKernel.KernelValueStore)} was not installed. Auto-created fallback store. Add {nameof(ValueStoreMB)} to targetObjects for explicit wiring.", this);
            }
        }

        // SceneKernel はここから毎フレーム tick される。
        // 破棄処理 (OnDestroy) との競合や構築失敗時に NRE を出さないよう null をガードする。
        private void Update()
        {
            kernel?.Tick(Time.deltaTime);
        }

        // シーン終了時は kernel を破棄し、保持している service を順に解放する。
        private void OnDestroy()
        {
            kernel?.Dispose();
            kernel = null;
        }
    }
}
