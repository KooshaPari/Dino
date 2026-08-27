#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using DINOForge.Runtime.Conflicts;
using DINOForge.Runtime.Localization;
using DINOForge.Runtime.Profiles;
using DINOForge.Runtime.Settings;
using DINOForge.Runtime.Telemetry;
using DINOForge.Runtime.Updates;
using DINOForge.SDK;
using DINOForge.SDK.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace DINOForge.Runtime.UI
{
    public partial class ModMenuPanel : MonoBehaviour, IModMenuHost
    {

    /// <summary>
    /// UGUI mod menu panel. Replaces the legacy IMGUI ModMenuOverlay.
    /// Layout: header bar | split (pack list / detail pane) | footer.
    /// Exposes the same public API as <see cref="ModMenuOverlay"/> so ModPlatform
    /// does not need changes.
    ///
    /// Entry points: Opened by F10 hotkey OR by clicking the injected MODS button on the
    /// native menu (main menu / pause menu). Both paths call <see cref="Toggle"/> on this
    /// panel's <see cref="IModMenuHost"/> implementation.
    /// </summary>
    public class ModMenuPanel : MonoBehaviour, IModMenuHost
    {
        // ── Public API surface (mirrors ModMenuOverlay) ──────────────────────────
        /// <summary>Callback invoked when the user clicks Reload Packs.</summary>
        public Action? OnReloadRequested { get; set; }

        /// <summary>Callback invoked when a pack is toggled (packId, isEnabled).</summary>
        public Action<string, bool>? OnPackToggled { get; set; }

        /// <summary>Whether this panel is currently visible or transitioning visible.</summary>
        public bool IsVisible => _targetVisible;

        /// <summary>The currently selected pack index in the current presenter list, or -1 if none.</summary>
        public int SelectedPackIndex => _presenter.SelectedIndex;

        // ── Panel layout constants ────────────────────────────────────────────────
        private const float PanelWidth = 680f;
        private const float PanelHeight = 560f;
        private const float HeaderHeight = 44f;
        private const float FooterHeight = 44f;
        private const float ListWidth = 220f;
        private const float ItemHeight = 40f;
        private const float AnimDuration = 0.15f;

        // ── State ────────────────────────────────────────────────────────────────
        private readonly ModMenuPresenter _presenter = new ModMenuPresenter();
        private ManualLogSource? _log;

        // ── Filter state ─────────────────────────────────────────────────────────
        private string _searchText = "";
        private string _tierFilter = "All";  // All, Engine Extension, Content, Total Conversion, Baseline
        private string _stateFilter = "All"; // All, Enabled, Disabled, Has Errors
        private string _sortBy = "Name";     // Name, Type, Version, Recently Updated
        private readonly List<int> _filteredIndices = new List<int>();

        // ── Keyboard navigation state ────────────────────────────────────────────
        private int _keyboardFocusedRowIndex = -1; // -1 = no focus, 0+ = index in filtered pack list

        // ── Animation ────────────────────────────────────────────────────────────
        private CanvasGroup? _canvasGroup;
        private RectTransform? _panelRt;
        private bool _targetVisible;

        // ── UI references ────────────────────────────────────────────────────────
        private Text? _headerStatusText;
        private RectTransform? _listContent;
        private Text? _listCounterText; // "N of M packs"
        private ScrollRect? _listScrollRect;
        private InputField? _searchInput;
        private Dropdown? _tierDropdown;
        private Dropdown? _stateDropdown;
        private Dropdown? _sortDropdown;
        private GameObject? _detailPane;
        private Text? _detailName;
        private Text? _detailMeta;
        private Text? _detailDesc;
        private Text? _detailDeps;
        private Text? _detailConflicts;
        private Text? _detailLoadOrder;
        private Text? _detailContent;
        private Text? _detailDetectedConflicts;
        private bool _listRefreshQueued;

        // ── Rich detail pane refs (#897) ──────────────────────────────────────
        private Text? _detailLicense;
        private Text? _detailRichContent;
        private RectTransform? _detailTagsRow;
        private RectTransform? _detailLinksRow;
        private RectTransform? _detailGalleryRow;

        // ── Badge row (#928-935) ──────────────────────────────────────────────
        /// <summary>Root GameObject of the horizontal badge row; rebuilt on each selection change.</summary>
        private GameObject? _detailBadgesRow;
        private readonly List<Image> _galleryThumbs = new List<Image>();
        private GameObject? _screenshotModal;
        private Image? _modalImage;

        // ── Conflict resolution (#903) ────────────────────────────────────────
        private RectTransform? _conflictSection;
        private GameObject? _diffModal;
        private Text? _diffModalTitle;
        private Text? _diffLeftText;
        private Text? _diffRightText;
        private ConflictResolutionStore? _conflictStore;
        private string _packsDirectory = string.Empty;

        // ── Update banner (#899) ──────────────────────────────────────────────
        /// <summary>Container row for the updates-available banner (hidden until updates found).</summary>
        private GameObject? _updateBannerRoot;
        /// <summary>Vertical content inside the banner — one row per pending update.</summary>
        private RectTransform? _updateBannerContent;
        private const float UpdateBannerRowHeight = 24f;

        // ── Profile manager (#918) ────────────────────────────────────────────
        private ProfileManager? _profileManager;
        private Dropdown? _profileDropdown;
        private InputField? _profileNameInput;
        private GameObject? _profileSaveModal;

        /// <summary>Callback invoked when the user loads a profile. Arg = list of pack IDs to enable.</summary>
        public Action<System.Collections.Generic.IReadOnlyList<string>>? OnProfileLoaded { get; set; }

        // ── Pack settings panel (#925) ────────────────────────────────────────
        /// <summary>Container for the per-pack settings section in the detail pane.</summary>
        private GameObject? _settingsSection;
        /// <summary>Content area inside the settings section for dynamically added setting controls.</summary>
        private RectTransform? _settingsContent;

        /// <summary>Canvas root used to anchor the dependency prompt dialog.</summary>
        private Transform? _canvasRoot;

        // ── Telemetry tab (#921) ──────────────────────────────────────────────
        /// <summary>Text element that displays the MetricsCollector DumpMarkdown output.</summary>
        private Text? _telemetryText;
        /// <summary>Running coroutine handle for the 2-second auto-refresh loop; null when not running.</summary>
        private Coroutine? _telemetryRefreshCoroutine;
        private const float TelemetryRefreshIntervalSec = 2f;

        // ── Help panel (#937) ──────────────────────────────────────────────────
        /// <summary>FAQ/help panel component; built on-demand when first shown.</summary>
        private HelpPanel? _helpPanel;

        // ── Bootstrap ────────────────────────────────────────────────────────────

        /// <summary>
        /// Initializes the logger. Must be called before Build().
        /// </summary>
        /// <param name="log">BepInEx logger for diagnostics.</param>
        public void Initialize(ManualLogSource log)
        {
            _log = log;
            _log?.LogInfo("[ModMenuPanel] Initialized with logger.");
        }

        /// <summary>
        /// Provides the packs directory path so the diff modal can read definition YAML files.
        /// Call before or after Build(); safe to call at any time.
        /// </summary>
        public void SetPacksDirectory(string packsDirectory)
        {
            _packsDirectory = packsDirectory ?? string.Empty;
        }

        /// <summary>
        /// Provides the conflict-resolution persistence store.
        /// Call before or after Build(); safe to call at any time.
        /// </summary>
        public void SetConflictResolutionStore(ConflictResolutionStore store)
        {
            _conflictStore = store;
        }

        /// <summary>
        /// Provides the profile manager that backs the Profiles section (#918).
        /// Call before or after Build(); safe to call at any time.
        /// When set before Build() the profile dropdown is populated immediately;
        /// when set after Build() the dropdown is refreshed on the next call to
        /// <see cref="RefreshProfileDropdown"/>.
        /// </summary>
        internal void SetProfileManager(ProfileManager profileManager)
        {
            _profileManager = profileManager;
            RefreshProfileDropdown();
        }

        public void Build(Transform canvasRoot)
        {
            _canvasRoot = canvasRoot;
            _log?.LogInfo("[ModMenuPanel.Build] Starting UGUI hierarchy construction...");

            // Root panel — centered
            GameObject rootGo = UiBuilder.MakePanel(canvasRoot, "ModMenuPanel",
                UiBuilder.BgDeep, new Vector2(PanelWidth, PanelHeight));
            RectTransform rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(300f, 0f); // slide-in offset start

            _panelRt = rootRt;
            _canvasGroup = UiBuilder.EnsureCanvasGroup(rootGo);
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            BuildHeader(rootGo.transform);
            BuildBody(rootGo.transform);
            BuildFooter(rootGo.transform);

            // If packs were already loaded before the UI finished building, render
            // them immediately so the list does not stay blank until the next refresh.
            RebuildPackList();
            RefreshDetail();

            _log?.LogInfo($"[ModMenuPanel.Build] UGUI hierarchy complete. _listContent={(_listContent != null ? _listContent.name : "NULL")}");
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Replaces the pack list and refreshes the UI.</summary>
        public void SetPacks(IEnumerable<PackDisplayInfo> packs)
        {
            int beforeCount = _presenter.Packs.Count;
            _presenter.SetPacks(packs);

            _log?.LogInfo($"");
            _log?.LogInfo($"╔════════════════════════════════════════════════════════════════════════════════════╗");
            _log?.LogInfo($"║ [ModMenuPanel.SetPacks] ENTRY                                                       ║");
            _log?.LogInfo($"╚════════════════════════════════════════════════════════════════════════════════════╝");
            _log?.LogInfo($"  Before: {beforeCount} packs, After: {_presenter.Packs.Count} packs");
            _log?.LogInfo($"  _listContent: {(_listContent != null ? $"READY (name={_listContent.name}, active={_listContent.gameObject.activeSelf})" : "NULL")}");
            _log?.LogInfo($"  SelectedIndex: {_presenter.SelectedIndex}");

            if (_presenter.Packs.Count > 0)
            {
                _log?.LogInfo($"  Pack list:");
                foreach (PackDisplayInfo p in _presenter.Packs)
                {
                    _log?.LogInfo($"    • {p.Name} (ID: {p.Id}, enabled: {p.IsEnabled})");
                }
            }

            // Safety check: if _listContent is null, it means Build() hasn't been called yet
            // or failed. This can happen if SetPacks is called before the UI hierarchy is complete.
            if (_listContent == null)
            {
                _log?.LogWarning("[ModMenuPanel.SetPacks] _listContent is NULL! UI hierarchy not initialized. " +
                    "Packs will queue and render when UI is ready. Check DFCanvas.Start() completion.");
            }

            // Fix #944/B2: ApplyFilters initialises _filteredIndices from the new pack set.
            // Without this, _filteredIndices stays empty after SetPacks → counter shows "0 of N"
            // and filtering is broken on first display.
            _log?.LogInfo($"[ModMenuPanel.SetPacks] Calling ApplyFilters() to populate _filteredIndices...");
            ApplyFilters();
            _log?.LogInfo($"[ModMenuPanel.SetPacks] ApplyFilters() complete ({_filteredIndices.Count} of {_presenter.Packs.Count} visible). Calling RefreshDetail()...");
            RefreshDetail();
            _log?.LogInfo($"[ModMenuPanel.SetPacks] RefreshDetail() complete. EXIT.");
            _log?.LogInfo($"");
        }

        public void SetStatus(string message, int errorCount = 0)
        {
            _presenter.SetStatus(message, errorCount);
            if (_headerStatusText != null)
            {
                _headerStatusText.text = BuildStatusLine();
                _headerStatusText.color = _presenter.ErrorCount > 0 ? UiBuilder.Error : UiBuilder.TextSecondary;
            }
        }

        public void Show()
        {
            // Immediate visibility - no animation (Update() never fires in DINO)
            _targetVisible = true;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            // Force panel to be fully visible
            if (_panelRt != null)
            {
                _panelRt.gameObject.SetActive(true);
                _panelRt.anchoredPosition = Vector2.zero; // Ensure no slide offset
            }

            // Force all children to be visible
            if (_listContent != null)
            {
                _listContent.gameObject.SetActive(true);
                for (int i = 0; i < _listContent.childCount; i++)
                {
                    _listContent.GetChild(i).gameObject.SetActive(true);
                }
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);
            }

            // Also force the entire panel hierarchy to rebuild
            if (_panelRt != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRt);
            }
        }

        /// <summary>Hides the panel immediately (no animation, Update() never fires).</summary>
        public void Hide()
        {
            _targetVisible = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_panelRt != null)
            {
                _panelRt.gameObject.SetActive(false);
            }
        }

        /// <inheritdoc />
        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }

        private void ShowHelpPanel()
        {
            if (_helpPanel == null && _canvasRoot != null)
            {
                // Build the help panel on first use
                _helpPanel = new GameObject("HelpPanel").AddComponent<HelpPanel>();
                _helpPanel.Build(_canvasRoot, _log);
            }

            if (_helpPanel != null)
            {
                _helpPanel.Show();
            }
        }

        // ── MonoBehaviour ─────────────────────────────────────────────────────────

        private void Update()
        {
            AnimatePanel();
            KeyboardUpdate();
        }

        // ── Animation ─────────────────────────────────────────────────────────────

        private void AnimatePanel()
        {
            // No-op: Update() never fires in DINO (MonoBehaviour.Update is not called).
            // Show()/Hide() set state immediately instead.
        }

        private void BuildHeader(Transform parent)
        {
            // Gradient background: top dark gray #1A1F2E → bottom slightly lighter #232938
            GameObject headerGradient = UiBuilder.MakePanel(parent, "HeaderGradient",
                HexColor("#1A1F2E"), new Vector2(0f, HeaderHeight));
            RectTransform hGradRt = headerGradient.GetComponent<RectTransform>();
            hGradRt.anchorMin = new Vector2(0f, 1f);
            hGradRt.anchorMax = Vector2.one;
            hGradRt.pivot = new Vector2(0.5f, 1f);
            hGradRt.offsetMin = Vector2.zero;
            hGradRt.offsetMax = Vector2.zero;
            hGradRt.sizeDelta = new Vector2(0f, HeaderHeight);

            // Overlay gradient layer (lighter at bottom)
            GameObject headerGradient2 = UiBuilder.MakePanel(headerGradient.transform, "GradientOverlay",
                HexColor("#232938"), new Vector2(0f, HeaderHeight));
            RectTransform hGrad2Rt = headerGradient2.GetComponent<RectTransform>();
            hGrad2Rt.anchorMin = Vector2.zero;
            hGrad2Rt.anchorMax = Vector2.one;
            hGrad2Rt.offsetMin = Vector2.zero;
            hGrad2Rt.offsetMax = Vector2.zero;
            Image grad2Img = headerGradient2.GetComponent<Image>();
            grad2Img.color = new Color(grad2Img.color.r, grad2Img.color.g, grad2Img.color.b, 0.4f);

            UiBuilder.AddHorizontalLayout(headerGradient, 8f, new RectOffset(12, 8, 6, 6));

            // Title
            Text title = UiBuilder.MakeText(headerGradient.transform, "Title", L10n.T("menu.title", "DINOForge"), 16,
                UiBuilder.Accent, bold: true);
            LayoutElement titleLe = title.gameObject.AddComponent<LayoutElement>();
            titleLe.preferredWidth = 120f;
            titleLe.minWidth = 80f;

            // Status text (flexible)
            _headerStatusText = UiBuilder.MakeText(headerGradient.transform, "Status",
                BuildStatusLine(), 12, UiBuilder.TextSecondary);
            LayoutElement statusLe = _headerStatusText.gameObject.AddComponent<LayoutElement>();
            statusLe.preferredWidth = 300f;
            statusLe.flexibleWidth = 1f;

            // Help button
            Button helpBtn = UiBuilder.MakeButton(
                headerGradient.transform, "HelpBtn", "?",
                UiBuilder.BgDeep, UiBuilder.TextSecondary,
                () =>
                {
                    ShowHelpPanel();
                });
            RectTransform helpBtnRt = helpBtn.GetComponent<RectTransform>();
            LayoutElement helpLe = helpBtn.gameObject.AddComponent<LayoutElement>();
            helpLe.preferredWidth = 28f;
            helpLe.preferredHeight = 28f;

            // Close button
            Button closeBtn = UiBuilder.MakeButton(
                headerGradient.transform, "CloseBtn", "×",
                UiBuilder.BgDeep, UiBuilder.TextSecondary,
                () =>
                {
                    ClearCurrentSelection();
                    Hide();
                });
            RectTransform closeBtnRt = closeBtn.GetComponent<RectTransform>();
            LayoutElement closeLe = closeBtn.gameObject.AddComponent<LayoutElement>();
            closeLe.preferredWidth = 28f;
            closeLe.preferredHeight = 28f;

            // Bottom separator
            GameObject sep = UiBuilder.MakeHorizontalSeparator(parent, UiBuilder.Border);
            RectTransform sepRt = sep.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 1f);
            sepRt.anchorMax = Vector2.one;
            sepRt.pivot = new Vector2(0.5f, 1f);
            sepRt.anchoredPosition = new Vector2(0f, -HeaderHeight);
            sepRt.sizeDelta = new Vector2(0f, 1f);
        }


        private static Color HexColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c))
                return c;
            return new Color(1f, 0f, 1f, 1f); // magenta fallback
        }

        private void BuildBody(Transform parent)
        {
            // Body container between header and footer
            GameObject body = new GameObject("Body", typeof(RectTransform));
            body.transform.SetParent(parent, false);
            RectTransform bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(0f, FooterHeight + 1f);
            bodyRt.offsetMax = new Vector2(0f, -(HeaderHeight + 1f));

            HorizontalLayoutGroup hlg = body.AddComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 0f;

            BuildListPane(body.transform);

            // Vertical divider
            GameObject divider = UiBuilder.MakePanel(body.transform, "Divider",
                UiBuilder.Border, new Vector2(1f, 0f));
            LayoutElement divLe = divider.AddComponent<LayoutElement>();
            divLe.preferredWidth = 1f;
            divLe.minWidth = 1f;

            BuildDetailPane(body.transform);
        }

        private void BuildFooter(Transform parent)
        {
            // Separator above footer
            GameObject sep = UiBuilder.MakeHorizontalSeparator(parent, UiBuilder.Border);
            RectTransform sepRt = sep.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 0f);
            sepRt.anchorMax = new Vector2(1f, 0f);
            sepRt.pivot = new Vector2(0.5f, 0f);
            sepRt.anchoredPosition = new Vector2(0f, FooterHeight);
            sepRt.sizeDelta = new Vector2(0f, 1f);

            GameObject footer = UiBuilder.MakePanel(parent, "Footer",
                UiBuilder.BgSurface, new Vector2(0f, FooterHeight));
            RectTransform fRt = footer.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = new Vector2(1f, 0f);
            fRt.pivot = new Vector2(0.5f, 0f);
            fRt.offsetMin = Vector2.zero;
            fRt.offsetMax = Vector2.zero;
            fRt.sizeDelta = new Vector2(0f, FooterHeight);

            UiBuilder.AddHorizontalLayout(footer, 8f, new RectOffset(12, 12, 7, 7));

            // Reload button
            Button reloadBtn = UiBuilder.MakeButton(
                footer.transform, "ReloadBtn", L10n.T("menu.button.reload", "↺  Reload Packs"),
                UiBuilder.BgDeep, UiBuilder.Accent,
                () =>
                {
                    ClearCurrentSelection();
                    OnReloadRequested?.Invoke();
                });
            LayoutElement reloadLe = reloadBtn.gameObject.AddComponent<LayoutElement>();
            reloadLe.preferredWidth = 140f;
            reloadLe.preferredHeight = 30f;

            // Spacer
            GameObject spacer = new GameObject("FooterSpacer", typeof(RectTransform));
            spacer.transform.SetParent(footer.transform, false);
            LayoutElement spacerLe = spacer.AddComponent<LayoutElement>();
            spacerLe.flexibleWidth = 1f;
        }

        private string BuildStatusLine()
        {
            string errPart = _presenter.ErrorCount > 0 ? $"  {_presenter.ErrorCount} errors" : "";
            int totalContent = 0;
            int enabledCount = 0;
            foreach (PackDisplayInfo pack in _presenter.Packs)
            {
                if (!pack.IsEnabled) continue;
                enabledCount++;
                foreach (System.Collections.Generic.KeyValuePair<string, int> kv in pack.ContentSummary)
                    totalContent += kv.Value;
            }
            string contentPart = totalContent > 0 ? $"  ({totalContent} content files)" : "";
            return $"{enabledCount}/{_presenter.Packs.Count} packs active{errPart}{contentPart}";
        }

        // ── Update banner (#899) ─────────────────────────────────────────────────

        /// <summary>
        /// Displays an "Updates Available" banner showing each pending update with a
        /// "View on GitHub" button. Hides the banner when the list is empty or null.
        /// Must be called on the Unity main thread.
    }
}