#nullable enable
using System;
using DINOForge.Runtime.Diagnostics;

namespace DINOForge.Runtime
{
    /// <summary>
    /// Bridges <see cref="HotReload.IHmrPackActions"/> to the <see cref="RuntimeDriver"/>
    /// deferred-work queue so tier-1 and tier-2 actions run safely on the Unity main thread.
    /// </summary>
    internal sealed class HmrPackActionsAdapter : HotReload.IHmrPackActions
    {
        private readonly RuntimeDriver _driver;

        internal HmrPackActionsAdapter(RuntimeDriver driver)
        {
            _driver = driver;
        }

        /// <inheritdoc/>
        public void TriggerPackReload()
        {
            // Enqueue through the existing deferred-work mechanism so LoadPacks +
            // UGUI refresh + SetStatus + ShowToast all fire from the main-thread coroutine.
            _driver.RequestPackReload("HMR tier-1");
        }

        /// <inheritdoc/>
        public void TriggerSceneReload()
        {
            // Load scene 1 (MainMenu) — asset bundles are re-evaluated on re-enter.
            try
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(1);
            }
            catch (Exception ex)
            {
                DebugLog.Write("Plugin", $"[HmrPackActionsAdapter] TriggerSceneReload LoadScene(1) failed: {ex.Message}");
            }
        }
    }
}
