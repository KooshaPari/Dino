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

        private void BuildProfilesSection(Transform parent)
        {
            // Section label row
            GameObject labelRow = new GameObject("ProfileLabelRow", typeof(RectTransform));
            labelRow.transform.SetParent(parent, false);
            HorizontalLayoutGroup labelHlg = labelRow.AddComponent<HorizontalLayoutGroup>();
            labelHlg.spacing = 4f;
            labelHlg.childForceExpandWidth = true;
            labelHlg.childForceExpandHeight = false;
            LayoutElement labelRowLe = labelRow.AddComponent<LayoutElement>();
            labelRowLe.preferredHeight = 18f;
            labelRowLe.flexibleWidth = 1f;

            Text sectionLabel = UiBuilder.MakeText(labelRow.transform, "ProfilesLabel", "Profiles", 11,
                UiBuilder.Accent, bold: true);
            sectionLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // Dropdown row
            GameObject ddRow = new GameObject("ProfileDropdownRow", typeof(RectTransform));
            ddRow.transform.SetParent(parent, false);
            HorizontalLayoutGroup ddHlg = ddRow.AddComponent<HorizontalLayoutGroup>();
            ddHlg.spacing = 4f;
            ddHlg.childForceExpandWidth = false;
            ddHlg.childForceExpandHeight = false;
            LayoutElement ddRowLe = ddRow.AddComponent<LayoutElement>();
            ddRowLe.preferredHeight = 26f;
            ddRowLe.flexibleWidth = 1f;

            _profileDropdown = MakeDropdown(ddRow.transform, "ProfileDropdown",
                new[] { "(no profiles)" }, _ => { });
            LayoutElement profileDdLe = _profileDropdown.gameObject.AddComponent<LayoutElement>();
            profileDdLe.flexibleWidth = 1f;
            profileDdLe.preferredHeight = 26f;

            // Action buttons row
            GameObject btnRow = new GameObject("ProfileButtonsRow", typeof(RectTransform));
            btnRow.transform.SetParent(parent, false);
            HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 3f;
            btnHlg.childForceExpandWidth = false;
            btnHlg.childForceExpandHeight = false;
            LayoutElement btnRowLe = btnRow.AddComponent<LayoutElement>();
            btnRowLe.preferredHeight = 24f;
            btnRowLe.flexibleWidth = 1f;

            // Load
            Button loadBtn = UiBuilder.MakeButton(btnRow.transform, "ProfileLoadBtn", "Load",
                UiBuilder.BgSurface, UiBuilder.TextPrimary, OnProfileLoad);
            loadBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;
            loadBtn.gameObject.GetComponent<LayoutElement>().preferredHeight = 22f;

            // Save As
            Button saveBtn = UiBuilder.MakeButton(btnRow.transform, "ProfileSaveBtn", "Save…",
                UiBuilder.BgSurface, UiBuilder.Accent, OnProfileSaveAs);
            saveBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 46f;
            saveBtn.gameObject.GetComponent<LayoutElement>().preferredHeight = 22f;

            // Export to clipboard
            Button exportBtn = UiBuilder.MakeButton(btnRow.transform, "ProfileExportBtn", "Export",
                UiBuilder.BgSurface, UiBuilder.TextSecondary, OnProfileExport);
            exportBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
            exportBtn.gameObject.GetComponent<LayoutElement>().preferredHeight = 22f;

            // Import from clipboard
            Button importBtn = UiBuilder.MakeButton(btnRow.transform, "ProfileImportBtn", "Import",
                UiBuilder.BgSurface, UiBuilder.TextSecondary, OnProfileImport);
            importBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
            importBtn.gameObject.GetComponent<LayoutElement>().preferredHeight = 22f;

            // Delete
            Button deleteBtn = UiBuilder.MakeButton(btnRow.transform, "ProfileDeleteBtn", "Del",
                UiBuilder.BgDeep, UiBuilder.Error, OnProfileDelete);
            deleteBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 30f;
            deleteBtn.gameObject.GetComponent<LayoutElement>().preferredHeight = 22f;

            // Spacer
            GameObject spacer = new GameObject("ProfileBtnSpacer", typeof(RectTransform));
            spacer.transform.SetParent(btnRow.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // Separator below profiles section
            UiBuilder.MakeHorizontalSeparator(parent, UiBuilder.Border);

            RefreshProfileDropdown();
        }


        private void RefreshProfileDropdown()
        {
            if (_profileDropdown == null) return;

            _profileDropdown.options.Clear();

            IReadOnlyList<string> names = _profileManager?.ListProfiles() ?? new List<string>();
            if (names.Count == 0)
            {
                _profileDropdown.options.Add(new Dropdown.OptionData("(no profiles)"));
            }
            else
            {
                foreach (string name in names)
                    _profileDropdown.options.Add(new Dropdown.OptionData(name));
            }

            _profileDropdown.value = 0;
            _profileDropdown.RefreshShownValue();
        }


        private string? SelectedProfileName()
        {
            if (_profileDropdown == null) return null;
            if (_profileDropdown.options.Count == 0) return null;
            string name = _profileDropdown.options[_profileDropdown.value].text;
            if (name == "(no profiles)") return null;
            return name;
        }

        private void OnProfileLoad()
        {
            if (_profileManager == null)
            {
                _log?.LogWarning("[ModMenuPanel] OnProfileLoad: no ProfileManager.");
                return;
            }

            string? name = SelectedProfileName();
            if (name == null)
            {
                _log?.LogInfo("[ModMenuPanel] OnProfileLoad: no profile selected.");
                return;
            }

            ModProfile? profile = _profileManager.Load(name);
            if (profile == null)
            {
                _log?.LogWarning($"[ModMenuPanel] OnProfileLoad: failed to load profile '{name}'.");
                return;
            }

            _log?.LogInfo($"[ModMenuPanel] OnProfileLoad: applying profile '{name}' ({profile.EnabledPacks.Count} packs).");
            OnProfileLoaded?.Invoke(profile.EnabledPacks);
        }

        private void OnProfileSaveAs()
        {
            if (_profileManager == null)
            {
                _log?.LogWarning("[ModMenuPanel] OnProfileSaveAs: no ProfileManager.");
                return;
            }

            if (_panelRt == null) return;

            // Build inline save-as modal
            ShowProfileSaveModal();
        }

        private void ShowProfileSaveModal()
        {
            if (_panelRt == null) return;

            // Destroy any existing modal
            if (_profileSaveModal != null)
            {
                Destroy(_profileSaveModal);
                _profileSaveModal = null;
            }

            GameObject overlay = new GameObject("ProfileSaveModal", typeof(RectTransform));
            overlay.transform.SetParent(_panelRt, false);
            RectTransform overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            Image backdrop = overlay.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.72f);
            backdrop.raycastTarget = true;

            GameObject dialog = UiBuilder.MakePanel(overlay.transform, "SaveAsDialog",
                UiBuilder.BgSurface, new Vector2(320f, 160f));
            RectTransform dialogRt = dialog.GetComponent<RectTransform>();
            dialogRt.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRt.pivot = new Vector2(0.5f, 0.5f);
            dialogRt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup vlg = dialog.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(14, 14, 12, 12);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            Text titleTxt = UiBuilder.MakeText(dialog.transform, "ModalTitle",
                "Save Profile As…", 13, UiBuilder.Accent, bold: true);
            titleTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            _profileNameInput = UiBuilder.MakeInputField(dialog.transform, "ProfileNameInput",
                "Profile name…", _ => { });
            LayoutElement inputLe = _profileNameInput.gameObject.AddComponent<LayoutElement>();
            inputLe.preferredHeight = 28f;
            inputLe.flexibleWidth = 1f;

            // Pre-fill with currently selected profile name if any
            string? currentName = SelectedProfileName();
            if (!string.IsNullOrEmpty(currentName))
                _profileNameInput.text = currentName;

            GameObject btnRow = new GameObject("BtnRow", typeof(RectTransform));
            btnRow.transform.SetParent(dialog.transform, false);
            HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 8f;
            btnHlg.childForceExpandWidth = false;
            btnHlg.childForceExpandHeight = false;
            btnHlg.childAlignment = TextAnchor.MiddleRight;
            btnRow.AddComponent<LayoutElement>().preferredHeight = 30f;

            GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(btnRow.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

            InputField capturedInput = _profileNameInput;
            GameObject capturedOverlay = overlay;

            Button cancelBtn = UiBuilder.MakeButton(btnRow.transform, "CancelBtn", "Cancel",
                UiBuilder.BgDeep, UiBuilder.TextSecondary,
                () => { Destroy(capturedOverlay); });
            cancelBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;

            Button saveBtn = UiBuilder.MakeButton(btnRow.transform, "SaveBtn", "Save",
                UiBuilder.Accent, Color.black,
                () =>
                {
                    string profileName = capturedInput != null ? capturedInput.text.Trim() : string.Empty;
                    if (string.IsNullOrWhiteSpace(profileName))
                    {
                        _log?.LogWarning("[ModMenuPanel] Save profile: name is empty.");
                        return;
                    }

                    // Collect currently enabled packs
                    List<string> enabledIds = new List<string>();
                    foreach (PackDisplayInfo pack in _presenter.Packs)
                    {
                        if (pack.IsEnabled)
                            enabledIds.Add(pack.Id);
                    }

                    _profileManager?.SaveCurrent(profileName, enabledIds);
                    Destroy(capturedOverlay);
                    RefreshProfileDropdown();

                    // Select the newly saved profile in the dropdown
                    if (_profileDropdown != null)
                    {
                        for (int i = 0; i < _profileDropdown.options.Count; i++)
                        {
                            if (_profileDropdown.options[i].text == profileName)
                            {
                                _profileDropdown.value = i;
                                break;
                            }
                        }
                    }

                    _log?.LogInfo($"[ModMenuPanel] Profile '{profileName}' saved.");
                });
            saveBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 56f;

            _profileSaveModal = overlay;
        }

        private void OnProfileExport()
        {
            if (_profileManager == null) return;

            string? name = SelectedProfileName();
            if (name == null)
            {
                _log?.LogInfo("[ModMenuPanel] OnProfileExport: no profile selected.");
                return;
            }

            string json = _profileManager.ExportJson(name);
            if (string.IsNullOrEmpty(json))
            {
                _log?.LogWarning($"[ModMenuPanel] OnProfileExport: ExportJson returned empty for '{name}'.");
                return;
            }

            try
            {
                GUIUtility.systemCopyBuffer = json;
                _log?.LogInfo($"[ModMenuPanel] Profile '{name}' exported to clipboard.");
                SetStatus($"Profile '{name}' copied to clipboard");
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[ModMenuPanel] OnProfileExport clipboard write failed: {ex.Message}");
            }
        }

        private void OnProfileImport()
        {
            if (_profileManager == null)
            {
                _log?.LogWarning("[ModMenuPanel] OnProfileImport: no ProfileManager.");
                return;
            }

            string json;
            try
            {
                json = GUIUtility.systemCopyBuffer ?? string.Empty;
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[ModMenuPanel] OnProfileImport clipboard read failed: {ex.Message}");
                return;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                _log?.LogInfo("[ModMenuPanel] OnProfileImport: clipboard is empty.");
                SetStatus("Clipboard is empty — nothing to import");
                return;
            }

            try
            {
                _profileManager.ImportJson(json);
                RefreshProfileDropdown();
                SetStatus("Profile imported from clipboard");
                _log?.LogInfo("[ModMenuPanel] Profile imported from clipboard.");
            }
            catch (InvalidOperationException ex)
            {
                _log?.LogWarning($"[ModMenuPanel] OnProfileImport validation failed: {ex.Message}");
                SetStatus($"Import failed: {ex.Message}", 1);
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[ModMenuPanel] OnProfileImport unexpected error: {ex.Message}");
                SetStatus("Import failed — see log for details", 1);
            }
        }

        private void OnProfileDelete()
        {
            if (_profileManager == null) return;

            string? name = SelectedProfileName();
            if (name == null)
            {
                _log?.LogInfo("[ModMenuPanel] OnProfileDelete: no profile selected.");
                return;
            }

            // Show confirm dialog before deleting
            ShowConfirmDialog(
                $"Delete profile \"{name}\"?\nThis cannot be undone.",
                onConfirm: () =>
                {
                    bool deleted = _profileManager.Delete(name);
                    if (deleted)
                    {
                        RefreshProfileDropdown();
                        SetStatus($"Profile '{name}' deleted");
                        _log?.LogInfo($"[ModMenuPanel] Profile '{name}' deleted.");
                    }
                    else
                    {
                        _log?.LogWarning($"[ModMenuPanel] OnProfileDelete: profile '{name}' not found.");
                    }
                },
                onCancel: () => { /* cancelled, nothing to do */ });
        }

        // ── Telemetry section helpers (#921) ──────────────────────────────────

        /// <summary>
        /// Builds the telemetry section appended at the bottom of the detail pane's scroll content.
        /// Displays <see cref="MetricsCollector.Instance.DumpMarkdown()"/> as monospace plain text;
        /// auto-refreshes every 2 seconds via coroutine; "Copy to Clipboard" button for sharing.
    }
}