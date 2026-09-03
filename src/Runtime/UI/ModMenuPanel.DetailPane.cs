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

        private void BuildDetailPane(Transform parent)
        {
            // Outer container (uses full flexible width; houses a scroll view so all rich
            // content fits without clipping even on a 560-px-tall panel).
            _detailPane = new GameObject("DetailPane", typeof(RectTransform));
            _detailPane.transform.SetParent(parent, false);
            LayoutElement detailLe = _detailPane.AddComponent<LayoutElement>();
            detailLe.flexibleWidth = 1f;
            detailLe.flexibleHeight = 1f;

            // Outer layout: stacks the scroll area + the fixed action-buttons row
            VerticalLayoutGroup outerVlg = _detailPane.AddComponent<VerticalLayoutGroup>();
            outerVlg.childForceExpandWidth = true;
            outerVlg.childForceExpandHeight = false;
            outerVlg.spacing = 0f;
            outerVlg.padding = new RectOffset(0, 0, 0, 0);

            // ── Scrollable content area ───────────────────────────────────────
            (ScrollRect detailScroll, RectTransform detailContent) =
                UiBuilder.MakeScrollView(_detailPane.transform, "DetailScroll", Vector2.zero);
            LayoutElement scrollLe = detailScroll.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleWidth = 1f;
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 100f;

            // Override the content VLG to use detail-pane padding
            VerticalLayoutGroup contentVlg = detailContent.GetComponent<VerticalLayoutGroup>();
            if (contentVlg != null)
            {
                contentVlg.padding = new RectOffset(14, 14, 12, 8);
                contentVlg.spacing = 6f;
            }

            Transform c = detailContent; // shorthand

            // ── Pack name ────────────────────────────────────────────────────
            _detailName = UiBuilder.MakeText(c, "DetailName", "Select a pack", 15,
                UiBuilder.TextPrimary, bold: true);
            AddFlexRow(_detailName.gameObject, preferredHeight: 22f);

            // ── Meta row (author · type · license badge) ─────────────────────
            GameObject metaRow = new GameObject("MetaRow", typeof(RectTransform));
            metaRow.transform.SetParent(c, false);
            HorizontalLayoutGroup metaHlg = metaRow.AddComponent<HorizontalLayoutGroup>();
            metaHlg.spacing = 6f;
            metaHlg.childForceExpandHeight = false;
            metaHlg.childForceExpandWidth = false;
            metaRow.AddComponent<LayoutElement>().preferredHeight = 18f;

            _detailMeta = UiBuilder.MakeText(metaRow.transform, "DetailMeta", "", 12, UiBuilder.TextSecondary);
            _detailMeta.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            _detailLicense = UiBuilder.MakeText(metaRow.transform, "LicenseBadge", "", 11, UiBuilder.Accent, bold: true);
            LayoutElement licenseLe = _detailLicense.gameObject.AddComponent<LayoutElement>();
            licenseLe.preferredWidth = 80f;
            licenseLe.minWidth = 0f;

            // ── Tags row ─────────────────────────────────────────────────────
            GameObject tagsHost = new GameObject("TagsRow", typeof(RectTransform));
            tagsHost.transform.SetParent(c, false);
            _detailTagsRow = tagsHost.GetComponent<RectTransform>();
            HorizontalLayoutGroup tagsHlg = tagsHost.AddComponent<HorizontalLayoutGroup>();
            tagsHlg.spacing = 4f;
            tagsHlg.childForceExpandHeight = false;
            tagsHlg.childForceExpandWidth = false;
            tagsHlg.childAlignment = TextAnchor.MiddleLeft;
            LayoutElement tagsRowLe = tagsHost.AddComponent<LayoutElement>();
            tagsRowLe.preferredHeight = 22f;
            tagsRowLe.flexibleWidth = 1f;
            tagsHost.SetActive(false); // hidden until pack with tags is selected

            // ── Badges row (#928-935) ─────────────────────────────────────────
            // Rebuilt dynamically in RefreshDetail via RefreshBadgesRow(); no static
            // host needed — the row is created fresh each time the selection changes.

            UiBuilder.MakeHorizontalSeparator(c, UiBuilder.Border);

            // ── Description ──────────────────────────────────────────────────
            _detailDesc = UiBuilder.MakeText(c, "DetailDesc", "", 12, UiBuilder.TextPrimary);
            LayoutElement descLe = _detailDesc.gameObject.AddComponent<LayoutElement>();
            descLe.preferredHeight = 60f;
            descLe.flexibleWidth = 1f;
            _detailDesc.verticalOverflow = VerticalWrapMode.Overflow;

            // ── External links row ───────────────────────────────────────────
            GameObject linksHost = new GameObject("LinksRow", typeof(RectTransform));
            linksHost.transform.SetParent(c, false);
            _detailLinksRow = linksHost.GetComponent<RectTransform>();
            HorizontalLayoutGroup linksHlg = linksHost.AddComponent<HorizontalLayoutGroup>();
            linksHlg.spacing = 6f;
            linksHlg.childForceExpandHeight = false;
            linksHlg.childForceExpandWidth = false;
            LayoutElement linksRowLe = linksHost.AddComponent<LayoutElement>();
            linksRowLe.preferredHeight = 26f;
            linksRowLe.flexibleWidth = 1f;
            linksHost.SetActive(false); // hidden when no URLs present

            UiBuilder.MakeHorizontalSeparator(c, UiBuilder.Border);

            // ── Dependencies / Conflicts / Load order ────────────────────────
            _detailDeps = UiBuilder.MakeText(c, "DetailDeps", L10n.T("menu.detail.dependencies", "Dependencies: none"), 12, UiBuilder.TextSecondary);
            AddFlexRow(_detailDeps.gameObject, preferredHeight: 18f);

            _detailConflicts = UiBuilder.MakeText(c, "DetailConflicts", L10n.T("menu.detail.conflicts", "Conflicts: none"), 12, UiBuilder.TextSecondary);
            AddFlexRow(_detailConflicts.gameObject, preferredHeight: 18f);

            _detailLoadOrder = UiBuilder.MakeText(c, "DetailLoadOrder", "Load Order: —", 12, UiBuilder.TextSecondary);
            AddFlexRow(_detailLoadOrder.gameObject, preferredHeight: 18f);

            UiBuilder.MakeHorizontalSeparator(c, UiBuilder.Border);

            // ── Rich content section ─────────────────────────────────────────
            // Shows count summary + first N item names (units, buildings, factions)
            _detailRichContent = UiBuilder.MakeText(c, "DetailRichContent", L10n.T("menu.detail.content", "Content: (none declared)"), 12,
                new Color(0.6f, 0.85f, 0.6f, 1f));
            LayoutElement richContentLe = _detailRichContent.gameObject.AddComponent<LayoutElement>();
            richContentLe.preferredHeight = 60f;
            richContentLe.flexibleWidth = 1f;
            _detailRichContent.verticalOverflow = VerticalWrapMode.Overflow;

            // Keep the legacy _detailContent / _detailDetectedConflicts wired so
            // RefreshDetail doesn't crash — but hide them (rich content replaces them).
            _detailContent = _detailRichContent; // alias — same text field
            _detailDetectedConflicts = UiBuilder.MakeText(c, "DetailDetectedConflicts", "", 12,
                new Color(0.9f, 0.6f, 0.2f, 1f));
            LayoutElement dcLe = _detailDetectedConflicts.gameObject.AddComponent<LayoutElement>();
            dcLe.preferredHeight = 30f;
            dcLe.flexibleWidth = 1f;
            _detailDetectedConflicts.verticalOverflow = VerticalWrapMode.Overflow;

            // ── Screenshot gallery ───────────────────────────────────────────
            // Horizontal scroll strip of 200×150 thumbnails; hidden when no screenshots.
            UiBuilder.MakeHorizontalSeparator(c, UiBuilder.Border);

            GameObject galleryHost = new GameObject("GalleryRow", typeof(RectTransform));
            galleryHost.transform.SetParent(c, false);
            _detailGalleryRow = galleryHost.GetComponent<RectTransform>();
            LayoutElement galleryRowLe = galleryHost.AddComponent<LayoutElement>();
            galleryRowLe.preferredHeight = 160f;
            galleryRowLe.flexibleWidth = 1f;
            galleryHost.SetActive(false); // hidden until screenshots found

            // Horizontal scroll view for thumbnails
            (ScrollRect galleryScroll, RectTransform galleryContent) =
                BuildHorizontalScrollView(galleryHost.transform, "GalleryScroll");
            RectTransform galleryScrollRt = galleryScroll.GetComponent<RectTransform>();
            UiBuilder.FillParent(galleryScrollRt);

            // We lazily populate galleryContent in RefreshDetail via the _detailGalleryRow tag.
            // Store the content RT by tagging the galleryHost for later lookup.
            galleryHost.name = "GalleryRow";

            // ── Conflict resolution section (#903) ────────────────────────────
            // Populated dynamically in RefreshConflictButtons(); hidden when no conflicts.
            UiBuilder.MakeHorizontalSeparator(c, UiBuilder.Border);

            GameObject conflictHost = new GameObject("ConflictSection", typeof(RectTransform));
            conflictHost.transform.SetParent(c, false);
            _conflictSection = conflictHost.GetComponent<RectTransform>();
            VerticalLayoutGroup conflictVlg = conflictHost.AddComponent<VerticalLayoutGroup>();
            conflictVlg.spacing = 6f;
            conflictVlg.childForceExpandWidth = true;
            conflictVlg.childForceExpandHeight = false;
            conflictVlg.padding = new RectOffset(0, 0, 4, 4);
            ContentSizeFitter conflictCsf = conflictHost.AddComponent<ContentSizeFitter>();
            conflictCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LayoutElement conflictHostLe = conflictHost.AddComponent<LayoutElement>();
            conflictHostLe.flexibleWidth = 1f;
            conflictHost.SetActive(false); // hidden until conflicts present

            // ── Per-pack runtime settings section (#925) ──────────────────────
            UiBuilder.MakeHorizontalSeparator(c, UiBuilder.Border);

            GameObject settingsHost = new GameObject("SettingsSection", typeof(RectTransform));
            settingsHost.transform.SetParent(c, false);
            _settingsSection = settingsHost;
            VerticalLayoutGroup settingsVlg = settingsHost.AddComponent<VerticalLayoutGroup>();
            settingsVlg.spacing = 8f;
            settingsVlg.childForceExpandWidth = true;
            settingsVlg.childForceExpandHeight = false;
            settingsVlg.padding = new RectOffset(12, 12, 8, 8);
            ContentSizeFitter settingsCsf = settingsHost.AddComponent<ContentSizeFitter>();
            settingsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LayoutElement settingsHostLe = settingsHost.AddComponent<LayoutElement>();
            settingsHostLe.flexibleWidth = 1f;
            settingsHost.SetActive(false); // hidden until pack has settings

            // Stores the setting controls for this pack
            _settingsContent = settingsHost.GetComponent<RectTransform>();

            // ── Telemetry section (#921) ──────────────────────────────────────
            BuildTelemetrySection(c);

            // ── Fixed action-buttons row (outside scroll) ────────────────────
            GameObject btnRow = new GameObject("ActionButtons", typeof(RectTransform));
            btnRow.transform.SetParent(_detailPane.transform, false);
            HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 8f;
            btnHlg.childForceExpandHeight = false;
            btnHlg.childForceExpandWidth = false;
            btnHlg.padding = new RectOffset(14, 14, 6, 6);
            LayoutElement btnRowLe = btnRow.AddComponent<LayoutElement>();
            btnRowLe.preferredHeight = 40f;
            btnRowLe.minHeight = 40f;

            Button toggleBtn = UiBuilder.MakeButton(
                btnRow.transform, "ToggleBtn", L10n.T("menu.button.disable", "Disable"),
                UiBuilder.BgSurface, UiBuilder.TextPrimary,
                OnToggleSelected);
            LayoutElement toggleBtnLe = toggleBtn.gameObject.AddComponent<LayoutElement>();
            toggleBtnLe.preferredWidth = 90f;
            toggleBtnLe.minWidth = 90f;
            toggleBtnLe.preferredHeight = 30f;

            // ── Screenshot modal (full-size overlay, initially hidden) ────────
            BuildScreenshotModal();

            // ── Diff modal (#903) ─────────────────────────────────────────────
            BuildDiffModal();
        }


        private static void AddFlexRow(GameObject go, float preferredHeight)
        {
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.flexibleWidth = 1f;
        }


        private static (ScrollRect scrollRect, RectTransform content) BuildHorizontalScrollView(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
            go.transform.SetParent(parent, false);

            Image bgImg = go.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0f);
            bgImg.raycastTarget = true;

            Mask mask = go.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(go.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            HorizontalLayoutGroup hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(4, 4, 4, 4);

            ScrollRect scrollRect = go.GetComponent<ScrollRect>();
            scrollRect.content = contentRt;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport = go.GetComponent<RectTransform>();

            return (scrollRect, contentRt);
        }


        private void BuildScreenshotModal()
        {
            if (_panelRt == null) return;
            _screenshotModal = UiBuilder.MakePanel(_panelRt, "ScreenshotModal",
                new Color(0f, 0f, 0f, 0.88f), Vector2.zero);
            UiBuilder.FillParent(_screenshotModal.GetComponent<RectTransform>());

            // The modal image (fills modal with aspect-ratio letterboxing via AspectRatioFitter)
            GameObject imgGo = new GameObject("ModalImage", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(_screenshotModal.transform, false);
            RectTransform imgRt = imgGo.GetComponent<RectTransform>();
            imgRt.anchorMin = new Vector2(0.05f, 0.1f);
            imgRt.anchorMax = new Vector2(0.95f, 0.95f);
            imgRt.offsetMin = Vector2.zero;
            imgRt.offsetMax = Vector2.zero;
            _modalImage = imgGo.GetComponent<Image>();
            _modalImage.preserveAspect = true;

            // Click anywhere on the modal to close
            Button closeOverlay = _screenshotModal.AddComponent<Button>();
            closeOverlay.targetGraphic = _screenshotModal.GetComponent<Image>();
            closeOverlay.onClick.AddListener(() => _screenshotModal.SetActive(false));

            _screenshotModal.SetActive(false);
        }

        private void RefreshDetail()
        {
            PackDisplayInfo? selected = _presenter.SelectedPack;
            if (selected == null)
            {
                if (_detailName != null) _detailName.text = "Select a pack";
                if (_detailMeta != null) _detailMeta.text = "";
                if (_detailLicense != null) _detailLicense.text = "";
                if (_detailDesc != null) _detailDesc.text = "";
                if (_detailDeps != null) _detailDeps.text = "Dependencies: none";
                if (_detailConflicts != null) _detailConflicts.text = "Conflicts: none";
                if (_detailLoadOrder != null) _detailLoadOrder.text = "Load Order: —";
                if (_detailContent != null) _detailContent.text = "Content: (none)";
                if (_detailDetectedConflicts != null) _detailDetectedConflicts.text = "";
                if (_detailTagsRow != null) _detailTagsRow.gameObject.SetActive(false);
                if (_detailLinksRow != null) _detailLinksRow.gameObject.SetActive(false);
                if (_detailGalleryRow != null) _detailGalleryRow.gameObject.SetActive(false);
                if (_detailBadgesRow != null) { UnityEngine.Object.Destroy(_detailBadgesRow); _detailBadgesRow = null; }
                return;
            }

            PackDisplayInfo p = selected;

            // ── Name ──────────────────────────────────────────────────────────
            if (_detailName != null) _detailName.text = p.Name;

            // ── Badges row (#928-935) — rendered above meta for visibility ─────
            RefreshBadgesRow(p);

            // ── Meta + license badge ──────────────────────────────────────────
            if (_detailMeta != null) _detailMeta.text = $"by {p.Author}  ·  {p.Type}  ·  v{p.Version}";
            if (_detailLicense != null)
            {
                if (!string.IsNullOrEmpty(p.License))
                {
                    _detailLicense.text = $"[{p.License}]";
                    _detailLicense.gameObject.SetActive(true);
                }
                else
                {
                    _detailLicense.text = "";
                    _detailLicense.gameObject.SetActive(false);
                }
            }

            // ── Tags row ──────────────────────────────────────────────────────
            RefreshTagsRow(p);

            // ── Description ───────────────────────────────────────────────────
            if (_detailDesc != null)
            {
                string descText = string.IsNullOrEmpty(p.Description)
                    ? "(no description)"
                    : p.Description!;

                if (p.Errors.Count > 0)
                {
                    descText = descText + "\n\n<color=#e05252>Errors:</color>\n"
                        + string.Join("\n", p.Errors);
                }

                _detailDesc.text = descText;
            }

            // ── External links row ────────────────────────────────────────────
            RefreshLinksRow(p);

            // ── Deps / Conflicts / Load order ─────────────────────────────────
            if (_detailDeps != null)
            {
                _detailDeps.text = p.Dependencies.Count == 0
                    ? "Dependencies: none"
                    : "Dependencies: " + string.Join(", ", p.Dependencies);
            }

            if (_detailConflicts != null)
            {
                _detailConflicts.text = p.Conflicts.Count == 0
                    ? "Conflicts: none"
                    : "<color=#e8a020>Conflicts: " + string.Join(", ", p.Conflicts) + "</color>";
            }

            if (_detailLoadOrder != null)
            {
                _detailLoadOrder.text = $"Load Order: {p.LoadOrder}";
            }

            // ── Rich content section ──────────────────────────────────────────
            if (_detailRichContent != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(512);
                if (p.ContentSummary.Count == 0)
                {
                    sb.Append(L10n.T("menu.detail.content", "Content: (none declared)"));
                }
                else
                {
                    sb.Append("<color=#88dd88>Content:</color>\n");
                    foreach (System.Collections.Generic.KeyValuePair<string, int> kv in p.ContentSummary)
                    {
                        string label = kv.Key.Replace('_', ' ');
                        sb.Append($"  {kv.Value} {label}\n");
                    }

                    // Show first N unit / building / faction names as previews
                    if (p.UnitNames.Count > 0)
                    {
                        int shown = System.Math.Min(5, p.UnitNames.Count);
                        string preview = string.Join(", ", SubList(p.UnitNames, shown));
                        string more = p.UnitNames.Count > shown ? $" and {p.UnitNames.Count - shown} more" : "";
                        sb.Append($"\n<color=#aaddaa>Units: </color>{preview}{more}\n");
                    }
                    if (p.BuildingNames.Count > 0)
                    {
                        int shown = System.Math.Min(3, p.BuildingNames.Count);
                        string preview = string.Join(", ", SubList(p.BuildingNames, shown));
                        string more = p.BuildingNames.Count > shown ? $" and {p.BuildingNames.Count - shown} more" : "";
                        sb.Append($"<color=#aaddaa>Buildings: </color>{preview}{more}\n");
                    }
                    if (p.FactionNames.Count > 0)
                    {
                        string factionList = string.Join(", ", p.FactionNames);
                        sb.Append($"<color=#aaddaa>Factions: </color>{factionList}\n");
                    }
                }

                _detailRichContent.text = sb.ToString().TrimEnd('\n');
            }

            // ── Detected conflicts ────────────────────────────────────────────
            if (_detailDetectedConflicts != null)
            {
                if (p.DetectedConflicts.Count == 0)
                {
                    _detailDetectedConflicts.text = "";
                }
                else
                {
                    _detailDetectedConflicts.text = "<color=#e8a020>Content Overlaps:</color>\n"
                        + string.Join("\n", p.DetectedConflicts);
                }
            }

            // ── Screenshot gallery (lazy-load textures via coroutine) ─────────
            RefreshGallery(p);

            // ── Conflict resolution buttons (#903) ────────────────────────────
            RefreshConflictButtons(p);

            // ── Per-pack runtime settings (#925) ───────────────────────────────
            RefreshSettings(p);

            // ── Toggle button label ───────────────────────────────────────────
            if (_detailPane != null)
            {
                Transform btnRow = _detailPane.transform.Find("ActionButtons");
                if (btnRow != null)
                {
                    Transform toggleBtnT = btnRow.Find("ToggleBtn");
                    if (toggleBtnT != null)
                    {
                        Text? btnLabel = toggleBtnT.Find("Label")?.GetComponent<Text>();
                        if (btnLabel != null)
                            btnLabel.text = p.IsEnabled ? "Disable" : "Enable";
                    }
                }
            }
        }

        /// <summary>
        /// Rebuilds the badge row for the selected pack (#928-935).
        /// Destroys any previous row, then delegates to <see cref="DINOForge.Runtime.UI.Badges.BadgeRenderer"/>
        /// to create the new one.  The row is inserted as the first child of the detail scroll content
        /// so badges appear at the very top of the detail pane.

        private void RefreshBadgesRow(PackDisplayInfo p)
        {
            // Destroy previous dynamic row
            if (_detailBadgesRow != null)
            {
                Destroy(_detailBadgesRow);
                _detailBadgesRow = null;
            }

            if (p.Badges.Count == 0) return;

            // Find the detail scroll content (parent of _detailName)
            Transform? contentParent = _detailName?.transform.parent;
            if (contentParent == null) return;

            _detailBadgesRow = DINOForge.Runtime.UI.Badges.BadgeRenderer.BuildBadgeRow(contentParent, p.Badges);

            // Move to just before the pack name (index 0) so badges appear at the top
            _detailBadgesRow.transform.SetSiblingIndex(0);
        }


        private void RefreshTagsRow(PackDisplayInfo p)
        {
            if (_detailTagsRow == null) return;
            // Clear existing chips
            for (int i = _detailTagsRow.childCount - 1; i >= 0; i--)
            {
                Transform child = _detailTagsRow.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            if (p.Tags.Count == 0)
            {
                _detailTagsRow.gameObject.SetActive(false);
                return;
            }

            _detailTagsRow.gameObject.SetActive(true);
            // Cycle through a small set of muted accent colours for variety
            Color[] chipColors = new Color[]
            {
                UiBuilder.HexColor("#2a4a38", 1f),
                UiBuilder.HexColor("#3a3420", 1f),
                UiBuilder.HexColor("#2a3a4a", 1f),
                UiBuilder.HexColor("#4a2a38", 1f),
            };
            int colorIdx = 0;
            foreach (string tag in p.Tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                Color chipBg = chipColors[colorIdx % chipColors.Length];
                colorIdx++;

                GameObject chip = UiBuilder.MakePanel(_detailTagsRow, $"Tag_{tag}",
                    chipBg, new Vector2(0f, 20f));
                HorizontalLayoutGroup chipHlg = chip.AddComponent<HorizontalLayoutGroup>();
                chipHlg.padding = new RectOffset(6, 6, 2, 2);
                chipHlg.childForceExpandWidth = false;
                LayoutElement chipLe = chip.AddComponent<LayoutElement>();
                chipLe.preferredHeight = 20f;

                Text chipText = UiBuilder.MakeText(chip.transform, "TagLabel", tag, 10,
                    UiBuilder.TextPrimary, bold: false, TextAnchor.MiddleCenter);
                LayoutElement textLe = chipText.gameObject.AddComponent<LayoutElement>();
                textLe.preferredWidth = tag.Length * 6.5f + 12f; // threshold-ok: character-width estimate
                textLe.minWidth = 20f;
            }
        }

        /// <summary>
        /// Returns true only for absolute http/https URLs.
        /// Prevents javascript:, file:, and other dangerous schemes from reaching
        /// <see cref="Application.OpenURL"/>.

        private static bool IsSafeWebUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }


        private void RefreshLinksRow(PackDisplayInfo p)
        {
            if (_detailLinksRow == null) return;
            // Clear existing buttons
            for (int i = _detailLinksRow.childCount - 1; i >= 0; i--)
            {
                Transform child = _detailLinksRow.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            bool hasLinks = !string.IsNullOrEmpty(p.HomepageUrl)
                || !string.IsNullOrEmpty(p.GithubUrl)
                || !string.IsNullOrEmpty(p.DiscordUrl);

            if (!hasLinks)
            {
                _detailLinksRow.gameObject.SetActive(false);
                return;
            }

            _detailLinksRow.gameObject.SetActive(true);

            void MakeLinkBtn(string label, string? url, Color accent)
            {
                if (string.IsNullOrEmpty(url)) return;
                string capturedUrl = url!;
                Button btn = UiBuilder.MakeButton(_detailLinksRow, $"Btn_{label}",
                    label, UiBuilder.BgSurface, accent,
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
                LayoutElement le = btn.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = label.Length * 7f + 20f; // threshold-ok: button width estimate
                le.minWidth = 60f;
                le.preferredHeight = 24f;
            }

            MakeLinkBtn("🌐 Homepage", p.HomepageUrl, UiBuilder.Accent);
            MakeLinkBtn("⌥ GitHub", p.GithubUrl, UiBuilder.TextPrimary);
            MakeLinkBtn("💬 Discord", p.DiscordUrl, UiBuilder.HexColor("#7289da", 1f));
        }


        private void RefreshGallery(PackDisplayInfo p)
        {
            if (_detailGalleryRow == null) return;

            // Clear old thumbnails
            _galleryThumbs.Clear();
            Transform galleryScroll = _detailGalleryRow.Find("GalleryScroll");
            if (galleryScroll != null)
            {
                Transform content = galleryScroll.Find("Content");
                if (content != null)
                {
                    for (int i = content.childCount - 1; i >= 0; i--)
                    {
                        Transform child = content.GetChild(i);
                        child.SetParent(null, false);
                        Destroy(child.gameObject);
                    }
                }
            }

            if (p.ScreenshotPaths.Count == 0)
            {
                _detailGalleryRow.gameObject.SetActive(false);
                return;
            }

            _detailGalleryRow.gameObject.SetActive(true);

            if (galleryScroll == null) return;
            Transform galleryContent = galleryScroll.Find("Content");
            if (galleryContent == null) return;

            // Spawn placeholder cards and kick off the texture load coroutine
            for (int i = 0; i < p.ScreenshotPaths.Count; i++)
            {
                int capturedIdx = i;
                string imgPath = p.ScreenshotPaths[i];

                // Thumbnail card (200 × 150)
                GameObject thumbCard = UiBuilder.MakePanel(galleryContent, $"Thumb_{i}",
                    UiBuilder.BgSurface, new Vector2(200f, 150f));
                LayoutElement thumbLe = thumbCard.AddComponent<LayoutElement>();
                thumbLe.preferredWidth = 200f;
                thumbLe.minWidth = 200f;
                thumbLe.preferredHeight = 150f;
                thumbLe.minHeight = 150f;

                // The image placeholder (starts grey, filled by coroutine)
                Image thumbImg = thumbCard.GetComponent<Image>();
                thumbImg.preserveAspect = true;
                _galleryThumbs.Add(thumbImg);

                // "Loading…" label
                Text loadingTxt = UiBuilder.MakeText(thumbCard.transform, "Loading",
                    "…", 11, UiBuilder.TextSecondary, bold: false, TextAnchor.MiddleCenter);
                UiBuilder.FillParent(loadingTxt.GetComponent<RectTransform>());

                // Click to open full-size in modal
                Button thumbBtn = thumbCard.AddComponent<Button>();
                thumbBtn.targetGraphic = thumbImg;
                int idx = capturedIdx;
                thumbBtn.onClick.AddListener(() => OpenScreenshotModal(idx));

                // Start lazy texture load
                StartCoroutine(LoadTextureAsync(imgPath, thumbImg, loadingTxt));
            }
        }


        private System.Collections.IEnumerator LoadTextureAsync(string path, Image target, Text loadingLabel)
        {
            yield return null; // yield once so we don't block the frame

            Texture2D? tex = null;
            try
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes))
                {
                    Destroy(tex);
                    tex = null;
                }
            }
            catch (Exception ex)
            {
                // safe-swallow: screenshot load is best-effort UI decoration
                System.Diagnostics.Debug.WriteLine($"ModMenuPanel screenshot load failed: {ex.Message}");
            }

            if (target == null) yield break; // UI was destroyed while loading

            if (tex != null)
            {
                target.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
                target.color = Color.white;
                if (loadingLabel != null) Destroy(loadingLabel.gameObject);
            }
            else
            {
                if (loadingLabel != null) loadingLabel.text = "⚠";
            }
        }


        private void OpenScreenshotModal(int thumbIndex)
        {
            if (_screenshotModal == null || _modalImage == null) return;
            if (thumbIndex < 0 || thumbIndex >= _galleryThumbs.Count) return;

            Image thumbImg = _galleryThumbs[thumbIndex];
            if (thumbImg == null || thumbImg.sprite == null) return;

            _modalImage.sprite = thumbImg.sprite;
            _screenshotModal.SetActive(true);
        }


        private static IEnumerable<string> SubList(IReadOnlyList<string> list, int count)
        {
            for (int i = 0; i < count && i < list.Count; i++)
                yield return list[i];
        }

        // ── Conflict resolution UI (#903) ──────────────────────────────────────────

        /// <summary>
        /// Builds the full-screen diff modal (hidden by default).
        /// Called once from <see cref="BuildDetailPane"/>.
    }
}