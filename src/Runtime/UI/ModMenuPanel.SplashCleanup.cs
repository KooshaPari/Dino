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
    public partial class ModMenuPanel
    {

        private void OnToggleSelected()
        {
            int index = _presenter.SelectedIndex;
            if (!_presenter.IsValidIndex(index)) return;

            PackDisplayInfo current = _presenter.Packs[index];

            // Only prompt when the user is enabling (not disabling) a pack.
            if (!current.IsEnabled)
            {
                List<string> missingDeps = CollectMissingDeps(current);
                if (missingDeps.Count > 0 && _canvasRoot != null)
                {
                    // Show the dependency dialog; defer the actual enable until the user confirms.
                    DepEnableDialog.Show(
                        _canvasRoot,
                        current.Name,
                        missingDeps,
                        onEnableAll: () =>
                        {
                            // Enable each missing dependency first, then the target pack.
                            foreach (string depId in missingDeps)
                            {
                                int depIdx = FindPackIndexById(depId);
                                if (depIdx < 0) continue;
                                if (!_presenter.TryToggleEnabled(depIdx, out PackDisplayInfo depUpdated)) continue;
                                OnPackToggled?.Invoke(depUpdated.Id, depUpdated.IsEnabled);
                            }

                            // Now enable the originally-selected pack.
                            if (_presenter.TryToggleEnabled(index, out PackDisplayInfo updated))
                            {
                                ClearCurrentSelection();
                                OnPackToggled?.Invoke(updated.Id, updated.IsEnabled);
                                QueueListRefresh();
                            }
                        },
                        onCancel: () =>
                        {
                            // Nothing to do — toggle was not applied.
                            _log?.LogInfo($"[ModMenuPanel] Dependency prompt cancelled for pack '{current.Id}'.");
                        });
                    return; // Dialog is showing; do not proceed with the toggle now.
                }
            }

            // No missing deps (or disabling) — apply the toggle immediately.
            if (!_presenter.TryToggleEnabled(index, out PackDisplayInfo immediateUpdated)) return;

            ClearCurrentSelection();
            OnPackToggled?.Invoke(immediateUpdated.Id, immediateUpdated.IsEnabled);
            QueueListRefresh();
        }

        /// <summary>
        /// Returns the IDs of dependencies that are declared by <paramref name="pack"/>
        /// but are currently disabled (or not present) in the presenter pack list.

        private List<string> CollectMissingDeps(PackDisplayInfo pack)
        {
            List<string> missing = new List<string>();
            foreach (string depId in pack.Dependencies)
            {
                bool depEnabled = false;
                foreach (PackDisplayInfo p in _presenter.Packs)
                {
                    if (string.Equals(p.Id, depId, StringComparison.Ordinal) && p.IsEnabled)
                    {
                        depEnabled = true;
                        break;
                    }
                }
                if (!depEnabled) missing.Add(depId);
            }
            return missing;
        }


        private int FindPackIndexById(string packId)
        {
            for (int i = 0; i < _presenter.Packs.Count; i++)
            {
                if (string.Equals(_presenter.Packs[i].Id, packId, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        public void ShowToast(string message, HotReload.HmrToastKind kind)
        {
            // Map HmrToastKind to the existing DINOForge ToastType (defined in HudStrip.cs).
            ToastType toastType = kind switch
            {
                HotReload.HmrToastKind.Warning => ToastType.Warning,
                HotReload.HmrToastKind.Error => ToastType.Error,
                _ => ToastType.Info,
            };

            // Best-effort: set status so the header always reflects the last toast.
            SetStatus(message, kind == HotReload.HmrToastKind.Error ? 1 : 0);

            // Propagate to the DFCanvas HudStrip toast if we can reach it.
            try
            {
                DFCanvas? canvas = GetComponentInParent<DFCanvas>();
                canvas?.ShowToast(message, toastType);
            }
            catch { } // safe-swallow: toast fallback is best-effort UI only
        }

        /// <summary>
        /// Shows a blocking-style confirmation dialog overlay inside the mod menu panel.
        /// Presents <paramref name="message"/> with "Yes" and "No" buttons.
        /// Callbacks are invoked on the Unity main thread (inside the dialog button click handlers).
        /// </summary>
        /// <param name="message">Body text of the confirmation prompt.</param>
        /// <param name="onConfirm">Called when the user presses "Yes".</param>

        public void ShowConfirmDialog(string message, Action onConfirm, Action onCancel)
        {
            if (_panelRt == null)
            {
                // Panel not yet built — fall back to executing cancel immediately.
                onCancel?.Invoke();
                return;
            }

            // Build a dimmed overlay that blocks interaction with the pack list beneath it.
            GameObject overlay = new GameObject("HmrConfirmOverlay", typeof(RectTransform));
            overlay.transform.SetParent(_panelRt, false);
            RectTransform overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            // Dimmed backdrop
            Image backdrop = overlay.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.72f);
            backdrop.raycastTarget = true;

            // Dialog box
            GameObject dialog = UiBuilder.MakePanel(overlay.transform, "DialogBox",
                UiBuilder.BgSurface, new Vector2(380f, 200f));
            RectTransform dialogRt = dialog.GetComponent<RectTransform>();
            dialogRt.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRt.pivot = new Vector2(0.5f, 0.5f);
            dialogRt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup vlg = dialog.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(16, 16, 14, 14);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title
            Text titleText = UiBuilder.MakeText(dialog.transform, "DialogTitle",
                "DINOForge — Mod Reload", 14, UiBuilder.Accent, bold: true);
            LayoutElement titleLe = titleText.gameObject.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 20f;

            // Body
            Text bodyText = UiBuilder.MakeText(dialog.transform, "DialogBody", message,
                12, UiBuilder.TextPrimary);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            LayoutElement bodyLe = bodyText.gameObject.AddComponent<LayoutElement>();
            bodyLe.preferredHeight = 90f;
            bodyLe.flexibleHeight = 1f;

            // Button row
            GameObject btnRow = new GameObject("BtnRow", typeof(RectTransform));
            btnRow.transform.SetParent(dialog.transform, false);
            HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 10f;
            btnHlg.childForceExpandHeight = false;
            btnHlg.childForceExpandWidth = false;
            btnHlg.childAlignment = TextAnchor.MiddleRight;
            LayoutElement btnRowLe = btnRow.AddComponent<LayoutElement>();
            btnRowLe.preferredHeight = 34f;
            btnRowLe.flexibleWidth = 1f;

            // Spacer pushes buttons to the right
            GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(btnRow.transform, false);
            LayoutElement spacerLe = spacer.AddComponent<LayoutElement>();
            spacerLe.flexibleWidth = 1f;

            Button cancelBtn = UiBuilder.MakeButton(
                btnRow.transform, "CancelBtn", "No — keep playing",
                UiBuilder.BgDeep, UiBuilder.TextSecondary,
                () =>
                {
                    Destroy(overlay);
                    onCancel?.Invoke();
                });
            LayoutElement cancelLe = cancelBtn.gameObject.AddComponent<LayoutElement>();
            cancelLe.preferredWidth = 130f;
            cancelLe.preferredHeight = 30f;

            Button confirmBtn = UiBuilder.MakeButton(
                btnRow.transform, "ConfirmBtn", "Yes — reload",
                UiBuilder.Accent, Color.black,
                () =>
                {
                    Destroy(overlay);
                    onConfirm?.Invoke();
                });
            LayoutElement confirmLe = confirmBtn.gameObject.AddComponent<LayoutElement>();
            confirmLe.preferredWidth = 110f;
            confirmLe.preferredHeight = 30f;

            // Ensure panel is visible so user sees the dialog.
            if (!IsVisible) Show();
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        public void SetUpdatesAvailable(IReadOnlyList<Updates.UpdateInfo> updates)
        {
            // Lazily build the banner the first time updates arrive.
            if (_updateBannerRoot == null && updates != null && updates.Count > 0)
            {
                BuildUpdateBanner();
            }

            if (_updateBannerRoot == null) return;

            bool hasUpdates = updates != null && updates.Count > 0;
            _updateBannerRoot.SetActive(hasUpdates);

            if (!hasUpdates || _updateBannerContent == null) return;

            // Clear existing rows.
            for (int i = _updateBannerContent.childCount - 1; i >= 0; i--)
            {
                Transform child = _updateBannerContent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            foreach (Updates.UpdateInfo info in updates!)
            {
                string rowLabel = $"{info.DisplayName}: {info.CurrentVersion} → {info.NewVersion}";
                string capturedUrl = info.ReleaseUrl;

                GameObject row = new GameObject($"UpdateRow_{info.ComponentId}", typeof(RectTransform));
                row.transform.SetParent(_updateBannerContent, false);

                HorizontalLayoutGroup rowHlg = row.AddComponent<HorizontalLayoutGroup>();
                rowHlg.spacing = 6f;
                rowHlg.childForceExpandHeight = false;
                rowHlg.childForceExpandWidth = false;
                rowHlg.padding = new RectOffset(8, 8, 2, 2);

                LayoutElement rowLe = row.AddComponent<LayoutElement>();
                rowLe.preferredHeight = UpdateBannerRowHeight;
                rowLe.flexibleWidth = 1f;

                Text labelText = UiBuilder.MakeText(row.transform, "UpdateLabel", rowLabel, 11, UiBuilder.Accent);
                LayoutElement labelLe = labelText.gameObject.AddComponent<LayoutElement>();
                labelLe.flexibleWidth = 1f;
                labelLe.minWidth = 60f;

                Button viewBtn = UiBuilder.MakeButton(
                    row.transform, "ViewBtn", "View on GitHub",
                    UiBuilder.BgSurface, UiBuilder.TextPrimary,
                    () =>
                    {
                        if (IsSafeWebUrl(capturedUrl))
                        {
                            try { Application.OpenURL(capturedUrl); }
                            catch { } // safe-swallow: OpenURL is best-effort
                        }
                        else
                        {
                            _log?.LogWarning($"[ModMenuPanel] Refusing unsafe URL: {capturedUrl}");
                        }
                    });
                LayoutElement viewLe = viewBtn.gameObject.AddComponent<LayoutElement>();
                viewLe.preferredWidth = 110f;
                viewLe.minWidth = 90f;
                viewLe.preferredHeight = UpdateBannerRowHeight - 4f;
            }

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_updateBannerContent);
        }

        // ── Profiles section (#918) ────────────────────────────────────────────

        /// <summary>
        /// Builds the Profiles row: dropdown of saved profiles + Load / Save As / Export /
        /// Import / Delete buttons.  Inserted above the search / filter controls.

        private void BuildUpdateBanner()
        {
            if (_panelRt == null) return;

            GameObject bannerRoot = UiBuilder.MakePanel(_panelRt, "UpdateBanner",
                UiBuilder.HexColor("#3a2a00", 1f), Vector2.zero);
            RectTransform bannerRt = bannerRoot.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0f, 1f);
            bannerRt.anchorMax = new Vector2(1f, 1f);
            bannerRt.pivot = new Vector2(0.5f, 1f);
            bannerRt.anchoredPosition = new Vector2(0f, -(HeaderHeight + 1f));
            bannerRt.sizeDelta = Vector2.zero;

            ContentSizeFitter bannerCsf = bannerRoot.AddComponent<ContentSizeFitter>();
            bannerCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup bannerVlg = bannerRoot.AddComponent<VerticalLayoutGroup>();
            bannerVlg.childForceExpandWidth = true;
            bannerVlg.childForceExpandHeight = false;
            bannerVlg.spacing = 0f;
            bannerVlg.padding = new RectOffset(0, 0, 4, 4);

            GameObject titleRow = new GameObject("BannerTitle", typeof(RectTransform));
            titleRow.transform.SetParent(bannerRoot.transform, false);
            titleRow.AddComponent<LayoutElement>().preferredHeight = 18f;
            UiBuilder.AddHorizontalLayout(titleRow, 4f, new RectOffset(8, 8, 2, 2));
            Text titleTxt = UiBuilder.MakeText(titleRow.transform, "BannerTitleText",
                "Updates Available", 11, UiBuilder.Accent, bold: true);
            titleTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject content = new GameObject("UpdateBannerContent", typeof(RectTransform));
            content.transform.SetParent(bannerRoot.transform, false);
            _updateBannerContent = content.GetComponent<RectTransform>();
            VerticalLayoutGroup contentVlg = content.AddComponent<VerticalLayoutGroup>();
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.spacing = 2f;
            ContentSizeFitter contentCsf = content.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            content.AddComponent<LayoutElement>().flexibleWidth = 1f;

            _updateBannerRoot = bannerRoot;
            bannerRoot.SetActive(false);
        }

        private void BuildDiffModal()
        {
            if (_panelRt == null) return;

            _diffModal = new GameObject("DiffModal", typeof(RectTransform));
            _diffModal.transform.SetParent(_panelRt, false);
            UiBuilder.FillParent(_diffModal.GetComponent<RectTransform>());

            Image dimmerImg = _diffModal.AddComponent<Image>();
            dimmerImg.color = new Color(0f, 0f, 0f, 0.88f);
            dimmerImg.raycastTarget = true;

            GameObject card = UiBuilder.MakePanel(_diffModal.transform, "DiffCard",
                UiBuilder.BgSurface, new Vector2(640f, 440f));
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup cardVlg = card.AddComponent<VerticalLayoutGroup>();
            cardVlg.spacing = 6f;
            cardVlg.padding = new RectOffset(12, 12, 10, 10);
            cardVlg.childForceExpandWidth = true;
            cardVlg.childForceExpandHeight = false;

            // Title row: heading + close button
            GameObject titleRow = new GameObject("TitleRow", typeof(RectTransform));
            titleRow.transform.SetParent(card.transform, false);
            HorizontalLayoutGroup titleHlg = titleRow.AddComponent<HorizontalLayoutGroup>();
            titleHlg.spacing = 8f;
            titleHlg.childForceExpandWidth = false;
            titleHlg.childForceExpandHeight = false;
            titleRow.AddComponent<LayoutElement>().preferredHeight = 24f;

            _diffModalTitle = UiBuilder.MakeText(titleRow.transform, "DiffTitle",
                "Conflict Diff", 14, UiBuilder.Accent, bold: true);
            _diffModalTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject diffModalRef = _diffModal; // capture for closure
            Button diffCloseBtn = UiBuilder.MakeButton(titleRow.transform, "DiffCloseBtn", "X",
                UiBuilder.BgDeep, UiBuilder.TextSecondary,
                () => diffModalRef.SetActive(false));
            LayoutElement diffCloseLe = diffCloseBtn.gameObject.AddComponent<LayoutElement>();
            diffCloseLe.preferredWidth = 24f;
            diffCloseLe.preferredHeight = 24f;

            UiBuilder.MakeHorizontalSeparator(card.transform, UiBuilder.Border);

            // Two-column scroll area
            GameObject cols = new GameObject("Columns", typeof(RectTransform));
            cols.transform.SetParent(card.transform, false);
            HorizontalLayoutGroup colsHlg = cols.AddComponent<HorizontalLayoutGroup>();
            colsHlg.spacing = 6f;
            colsHlg.childForceExpandHeight = true;
            colsHlg.childForceExpandWidth = false;
            LayoutElement colsLe = cols.AddComponent<LayoutElement>();
            colsLe.flexibleWidth = 1f;
            colsLe.preferredHeight = 360f;
            colsLe.minHeight = 200f; // threshold-ok: minimum readable diff panel height

            _diffLeftText = BuildDiffColumn(cols.transform, "LeftColumn", "Pack A");
            _diffRightText = BuildDiffColumn(cols.transform, "RightColumn", "Pack B");

            _diffModal.SetActive(false);
        }


        private static Text BuildDiffColumn(Transform parent, string name, string headerLabel)
        {
            GameObject col = new GameObject(name, typeof(RectTransform));
            col.transform.SetParent(parent, false);
            VerticalLayoutGroup colVlg = col.AddComponent<VerticalLayoutGroup>();
            colVlg.spacing = 4f;
            colVlg.childForceExpandWidth = true;
            colVlg.childForceExpandHeight = false;
            LayoutElement colLe = col.AddComponent<LayoutElement>();
            colLe.flexibleWidth = 1f;
            colLe.flexibleHeight = 1f;

            Text colHeader = UiBuilder.MakeText(col.transform, "Header", headerLabel, 12,
                UiBuilder.Accent, bold: true);
            colHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            (ScrollRect colScroll, RectTransform colContent) =
                UiBuilder.MakeScrollView(col.transform, name + "Scroll", Vector2.zero);
            LayoutElement colScrollLe = colScroll.gameObject.AddComponent<LayoutElement>();
            colScrollLe.flexibleWidth = 1f;
            colScrollLe.flexibleHeight = 1f;
            colScrollLe.minHeight = 140f; // threshold-ok: minimum readable diff scroll height

            VerticalLayoutGroup colContentVlg = colContent.GetComponent<VerticalLayoutGroup>();
            if (colContentVlg != null) colContentVlg.padding = new RectOffset(6, 6, 4, 4);

            Text bodyText = UiBuilder.MakeText(colContent, "Body", "", 10, UiBuilder.TextPrimary);
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            return bodyText;
        }


        private void OpenDiffModal(string selfPackId, string otherPackId, string overlapSummary)
        {
            if (_diffModal == null || _diffModalTitle == null ||
                _diffLeftText == null || _diffRightText == null) return;

            _diffModalTitle.text = "Diff: " + selfPackId + "  vs  " + otherPackId;
            _diffLeftText.text = LoadPackYamlPreview(selfPackId, overlapSummary);
            _diffRightText.text = LoadPackYamlPreview(otherPackId, overlapSummary);
            _diffModal.SetActive(true);
        }

        /// <summary>
        /// Reads pack.yaml for the given pack ID and returns a preview string (first 120 lines).
        /// Falls back to an error notice if the file cannot be read.

        private string LoadPackYamlPreview(string packId, string overlapSummary)
        {
            if (string.IsNullOrEmpty(_packsDirectory)) return "(packs directory not set)";
            string yamlPath = Path.Combine(_packsDirectory, packId, "pack.yaml");
            try
            {
                if (!File.Exists(yamlPath)) return "(pack.yaml not found for " + packId + ")";
                string yaml = File.ReadAllText(yamlPath, System.Text.Encoding.UTF8);
                string[] lines = yaml.Split('\n');
                int lineLimit = System.Math.Min(lines.Length, 120); // threshold-ok: modal line display limit
                System.Text.StringBuilder sb = new System.Text.StringBuilder(2048);
                sb.Append("=== ").Append(packId).Append(" / pack.yaml ===\n");
                sb.Append("(overlap: ").Append(overlapSummary).Append(")\n\n");
                for (int i = 0; i < lineLimit; i++)
                {
                    sb.Append(lines[i]);
                    sb.Append('\n');
                }
                if (lines.Length > lineLimit)
                    sb.Append("\n... (").Append(lines.Length - lineLimit).Append(" more lines)");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                // safe-swallow: diff preview is best-effort UI decoration
                System.Diagnostics.Debug.WriteLine($"ModMenuPanel pack.yaml preview failed for {packId}: {ex.Message}");
                return "(error reading pack.yaml for " + packId + ")";
            }
        }

        /// <summary>
        /// Rebuilds the interactive conflict-resolution button rows for the selected pack.
        /// Called from <see cref="RefreshDetail"/> after the gallery section.
        /// Hides the conflict section entirely when there are no detected conflicts.

        private void RefreshConflictButtons(PackDisplayInfo p)
        {
            if (_conflictSection == null) return;

            for (int i = _conflictSection.childCount - 1; i >= 0; i--)
            {
                Transform child = _conflictSection.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            if (p.DetectedConflicts.Count == 0)
            {
                _conflictSection.gameObject.SetActive(false);
                return;
            }

            _conflictSection.gameObject.SetActive(true);

            Text conflictHeader = UiBuilder.MakeText(_conflictSection, "ConflictHeader",
                "Content Overlaps  --  Resolution", 12, UiBuilder.Warning, bold: true);
            conflictHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            UiBuilder.MakeHorizontalSeparator(_conflictSection, UiBuilder.Border);

            foreach (string conflict in p.DetectedConflicts)
                BuildConflictRow(p.Id, conflict);
        }

        /// <summary>
        /// Builds one conflict row: description + status label + 4 action buttons.
        /// DetectedConflicts format: "warfare-starwars also loads: factions, units"

        private void BuildConflictRow(string selfPackId, string conflictEntry)
        {
            if (_conflictSection == null) return;

            string otherPackId = conflictEntry;
            string overlapSummary = conflictEntry;

            int alsoIdx = conflictEntry.IndexOf(" also loads:", StringComparison.Ordinal);
            if (alsoIdx > 0)
            {
                otherPackId = conflictEntry.Substring(0, alsoIdx).Trim();
                overlapSummary = conflictEntry.Substring(alsoIdx + " also loads:".Length).Trim();
            }

            string capturedSelf = selfPackId;
            string capturedOther = otherPackId;
            string capturedOverlap = overlapSummary;

            GameObject row = new GameObject("ConflictRow_" + otherPackId, typeof(RectTransform));
            row.transform.SetParent(_conflictSection, false);
            VerticalLayoutGroup rowVlg = row.AddComponent<VerticalLayoutGroup>();
            rowVlg.spacing = 4f;
            rowVlg.childForceExpandWidth = true;
            rowVlg.childForceExpandHeight = false;
            rowVlg.padding = new RectOffset(0, 0, 2, 6);
            ContentSizeFitter rowCsf = row.AddComponent<ContentSizeFitter>();
            rowCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            row.AddComponent<LayoutElement>().flexibleWidth = 1f;

            string descText = "Pack \"" + otherPackId + "\" overlaps on: " + overlapSummary;
            Text descLabel = UiBuilder.MakeText(row.transform, "ConflictDesc",
                descText, 11, UiBuilder.Warning);
            descLabel.verticalOverflow = VerticalWrapMode.Overflow;
            descLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            LayoutElement descLe = descLabel.gameObject.AddComponent<LayoutElement>();
            descLe.flexibleWidth = 1f;
            descLe.preferredHeight = 28f;

            Text statusLabel = UiBuilder.MakeText(row.transform, "ResolutionStatus",
                GetResolutionStatusText(capturedSelf, capturedOther), 10, UiBuilder.TextSecondary);
            statusLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

            GameObject btnRow = new GameObject("ResolutionBtns", typeof(RectTransform));
            btnRow.transform.SetParent(row.transform, false);
            HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 4f;
            btnHlg.childForceExpandWidth = false;
            btnHlg.childForceExpandHeight = false;
            btnHlg.padding = new RectOffset(0, 0, 2, 0);
            btnRow.AddComponent<LayoutElement>().preferredHeight = 26f;

            Text capturedStatus = statusLabel;

            Button useSelfBtn = UiBuilder.MakeButton(btnRow.transform, "Btn_prefer_self",
                "Use This Pack", UiBuilder.BgDeep, UiBuilder.Success,
                () =>
                {
                    _conflictStore?.SetResolution(capturedSelf, capturedOther, "prefer_self");
                    _conflictStore?.Save();
                    if (capturedStatus != null)
                        capturedStatus.text = GetResolutionStatusText(capturedSelf, capturedOther);
                });
            LayoutElement useSelfLe = useSelfBtn.gameObject.AddComponent<LayoutElement>();
            useSelfLe.preferredWidth = 95f;
            useSelfLe.minWidth = 80f;
            useSelfLe.preferredHeight = 24f;

            Button useOtherBtn = UiBuilder.MakeButton(btnRow.transform, "Btn_prefer_other",
                "Use Other Pack", UiBuilder.BgDeep, UiBuilder.Warning,
                () =>
                {
                    _conflictStore?.SetResolution(capturedSelf, capturedOther, "prefer_other");
                    _conflictStore?.Save();
                    if (capturedStatus != null)
                        capturedStatus.text = GetResolutionStatusText(capturedSelf, capturedOther);
                });
            LayoutElement useOtherLe = useOtherBtn.gameObject.AddComponent<LayoutElement>();
            useOtherLe.preferredWidth = 100f;
            useOtherLe.minWidth = 85f;
            useOtherLe.preferredHeight = 24f;

            Button mergeBtn = UiBuilder.MakeButton(btnRow.transform, "Btn_merge",
                "Keep Both", UiBuilder.BgDeep, UiBuilder.TextSecondary,
                () =>
                {
                    _conflictStore?.SetResolution(capturedSelf, capturedOther, "merge");
                    _conflictStore?.Save();
                    if (capturedStatus != null)
                        capturedStatus.text = GetResolutionStatusText(capturedSelf, capturedOther);
                });
            LayoutElement mergeLe = mergeBtn.gameObject.AddComponent<LayoutElement>();
            mergeLe.preferredWidth = 80f;
            mergeLe.minWidth = 70f;
            mergeLe.preferredHeight = 24f;

            Button showDiffBtn = UiBuilder.MakeButton(btnRow.transform, "Btn_ShowDiff",
                "Show Diff", UiBuilder.BgSurface, UiBuilder.Accent,
                () => OpenDiffModal(capturedSelf, capturedOther, capturedOverlap));
            LayoutElement showDiffLe = showDiffBtn.gameObject.AddComponent<LayoutElement>();
            showDiffLe.preferredWidth = 80f;
            showDiffLe.minWidth = 70f;
            showDiffLe.preferredHeight = 24f;
        }


        private string GetResolutionStatusText(string selfId, string otherId)
        {
            string? res = _conflictStore?.GetResolution(selfId, otherId);
            if (string.IsNullOrEmpty(res) || string.Equals(res, "merge", StringComparison.Ordinal))
                return "Resolution: Keep Both (default)";
            if (string.Equals(res, "prefer_self", StringComparison.Ordinal))
                return "Resolution: Prefer \"" + selfId + "\"";
            if (string.Equals(res, "prefer_other", StringComparison.Ordinal))
                return "Resolution: Prefer \"" + otherId + "\"";
            return "Resolution: " + res;
        }

    }
}