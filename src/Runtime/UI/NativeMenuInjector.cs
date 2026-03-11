#nullable enable
using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DINOForge.Runtime.UI
{
    /// <summary>
    /// Monitors Unity scene changes and injects a "Mods" button into DINO's native
    /// UGUI menus (main menu / pause menu) so players can open the DINOForge mod menu
    /// without knowing the F10 hotkey.
    ///
    /// Strategy:
    ///   1. Subscribe to <see cref="SceneManager.activeSceneChanged"/> on Awake.
    ///   2. Every <see cref="RescanInterval"/> seconds (and on each scene change) call
    ///      <see cref="TryInjectMenuButton"/>.
    ///   3. Scan all active canvases for a "Settings" or "Options" button.
    ///   4. Clone that button, label it "Mods", and wire its onClick to
    ///      <see cref="ModMenuOverlay.Toggle"/>.
    ///   5. Stop scanning once injection succeeds; resume if the injected button is destroyed.
    ///
    /// Graceful failure: any exception during injection is caught and logged as a warning.
    /// The component never throws to its caller.
    /// </summary>
    public class NativeMenuInjector : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        // Well-known canvas names to check (case-insensitive prefix/substring)
        // ------------------------------------------------------------------ //
        private static readonly string[] CanvasCandidateNames =
        {
            "MainMenu",
            "PauseMenu",
            "SettingsMenu",
            "UI",
            "HUD",
            "Menu",
            "Canvas",
        };

        /// <summary>Interval in seconds between injection re-scan attempts.</summary>
        private const float RescanInterval = 2f;

        private ManualLogSource? _log;
        private ModMenuOverlay? _overlay;

        private Button? _injectedButton;
        private bool _injected;
        private float _rescanTimer;

        // ------------------------------------------------------------------ //
        // Public wiring surface
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Provides the mod menu overlay that the injected button will toggle.
        /// Called by <see cref="RuntimeDriver"/> immediately after AddComponent.
        /// </summary>
        /// <param name="overlay">The persistent <see cref="ModMenuOverlay"/> instance.</param>
        public void SetModMenuOverlay(ModMenuOverlay overlay)
        {
            _overlay = overlay;
        }

        /// <summary>
        /// Sets the BepInEx logger used for injection status messages.
        /// </summary>
        /// <param name="log">Logger instance from the RuntimeDriver.</param>
        public void SetLogger(ManualLogSource log)
        {
            _log = log;
        }

        // ------------------------------------------------------------------ //
        // MonoBehaviour lifecycle
        // ------------------------------------------------------------------ //

        private void Awake()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            LogInfo("[NativeMenuInjector] Awake — subscribed to scene changes.");
        }

        private void Start()
        {
            TryInjectMenuButton();
        }

        private void Update()
        {
            // If we have already injected and the button is still alive, nothing to do.
            if (_injected && _injectedButton != null) return;

            // Button was destroyed (e.g. scene unloaded) — reset and re-scan.
            if (_injected && _injectedButton == null)
            {
                LogInfo("[NativeMenuInjector] Injected button was destroyed; will re-inject.");
                _injected = false;
            }

            _rescanTimer += Time.deltaTime;
            if (_rescanTimer < RescanInterval) return;

            _rescanTimer = 0f;
            TryInjectMenuButton();
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        // ------------------------------------------------------------------ //
        // Scene change handler
        // ------------------------------------------------------------------ //

        private void OnActiveSceneChanged(Scene previous, Scene next)
        {
            LogInfo($"[NativeMenuInjector] Scene changed: {previous.name} → {next.name}. Re-scanning for menu.");
            _injected = false;
            _injectedButton = null;
            _rescanTimer = 0f;
            TryInjectMenuButton();
        }

        // ------------------------------------------------------------------ //
        // Injection logic
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Attempts to locate a Settings/Options button in any active canvas and injects
        /// a sibling "Mods" button next to it.  Safe to call multiple times; idempotent
        /// once <c>_injected</c> is true.
        /// </summary>
        private void TryInjectMenuButton()
        {
            try
            {
                Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
                LogInfo($"[NativeMenuInjector] Scanning {allCanvases.Length} canvases...");

                foreach (Canvas canvas in allCanvases)
                {
                    if (!IsCanvasActive(canvas)) continue;
                    if (!IsCanvasNameMatch(canvas.name)) continue;

                    Button? settingsButton = FindSettingsButton(canvas);
                    if (settingsButton == null) continue;

                    LogInfo($"[NativeMenuInjector] Found Settings button in canvas '{canvas.name}'. Injecting Mods button.");

                    InjectButton(settingsButton);

                    if (_injected) return;
                }

                LogInfo("[NativeMenuInjector] No suitable menu found this scan. Will retry.");
            }
            catch (Exception ex)
            {
                LogWarning($"[NativeMenuInjector] TryInjectMenuButton failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Searches a canvas for a button whose label contains "Settings" or "Options".
        /// </summary>
        private Button? FindSettingsButton(Canvas canvas)
        {
            try
            {
                Button? settings = NativeUiHelper.FindButtonByText(canvas.transform, "Settings");
                if (settings != null) return settings;

                Button? options = NativeUiHelper.FindButtonByText(canvas.transform, "Options");
                return options;
            }
            catch (Exception ex)
            {
                LogWarning($"[NativeMenuInjector] FindSettingsButton exception: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clones the reference button, labels it "Mods", positions it after the original,
        /// and wires its onClick event.
        /// </summary>
        private void InjectButton(Button settingsButton)
        {
            try
            {
                // Guard: don't inject twice into the same parent
                Transform parent = settingsButton.transform.parent;
                if (parent != null)
                {
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        if (parent.GetChild(i).name == "DINOForge_ModsButton")
                        {
                            Button existing = parent.GetChild(i).GetComponent<Button>();
                            if (existing != null)
                            {
                                _injectedButton = existing;
                                _injected = true;
                                LogInfo("[NativeMenuInjector] Mods button already present; skipping re-inject.");
                                return;
                            }
                        }
                    }
                }

                Button modsButton = NativeUiHelper.CloneButton(settingsButton, "Mods");

                // Position adjacent to Settings button
                RectTransform modsRect = modsButton.GetComponent<RectTransform>();
                RectTransform settingsRect = settingsButton.GetComponent<RectTransform>();
                if (modsRect != null && settingsRect != null)
                {
                    NativeUiHelper.PositionAfterSibling(modsRect, settingsRect);
                }

                // Wire onClick
                modsButton.onClick.RemoveAllListeners();
                modsButton.onClick.AddListener(OnModsButtonClicked);

                // Activate in case the source was inactive
                modsButton.gameObject.SetActive(true);

                _injectedButton = modsButton;
                _injected = true;

                LogInfo("[NativeMenuInjector] Mods button injected successfully.");
            }
            catch (Exception ex)
            {
                LogWarning($"[NativeMenuInjector] InjectButton failed: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------ //
        // Button click handler
        // ------------------------------------------------------------------ //

        private void OnModsButtonClicked()
        {
            try
            {
                if (_overlay != null)
                {
                    _overlay.Toggle();
                    LogInfo("[NativeMenuInjector] Mods menu toggled via native button.");
                }
                else
                {
                    LogWarning("[NativeMenuInjector] OnModsButtonClicked — overlay reference is null.");
                }
            }
            catch (Exception ex)
            {
                LogWarning($"[NativeMenuInjector] OnModsButtonClicked exception: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------ //
        // Helpers
        // ------------------------------------------------------------------ //

        private static bool IsCanvasActive(Canvas canvas)
        {
            return canvas != null
                && canvas.gameObject != null
                && canvas.gameObject.activeInHierarchy;
        }

        private static bool IsCanvasNameMatch(string canvasName)
        {
            if (string.IsNullOrEmpty(canvasName)) return false;

            foreach (string candidate in CanvasCandidateNames)
            {
                if (canvasName.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private void LogInfo(string message)
        {
            if (_log != null)
                _log.LogInfo(message);
        }

        private void LogWarning(string message)
        {
            if (_log != null)
                _log.LogWarning(message);
        }
    }
}
