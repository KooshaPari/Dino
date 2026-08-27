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
        // ── Keyboard navigation ────────────────────────────────────────────────────

        /// <summary>
        /// Handles keyboard input for the mod menu panel.
        ///
        /// Keymap:
        ///   - Arrow Up/Down: Navigate pack list (keyboard focus only)
        ///   - Enter/Space: Toggle enable/disable on the focused pack
        ///   - Tab: Cycle focus between sections (search → pack list)
        ///   - Esc: Close the menu
        ///   - '/': Focus the search input field
        ///   - Ctrl+R: Reload packs
        ///   - Ctrl+S: Reserved for future profile save feature
        ///
        /// Guards: Only processes input when the panel is visible.
        /// </summary>
        private void KeyboardUpdate()
        {
            if (!IsVisible)
                return;

            // Esc always closes, even if input is focused
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearCurrentSelection();
                Hide();
                return;
            }

            // Check if an InputField currently has focus (e.g., search box)
            bool inputFieldFocused = EventSystem.current?.currentSelectedGameObject?.GetComponent<InputField>() != null;

            // Arrow keys + Enter/Space only work when no input field is focused
            if (!inputFieldFocused)
            {
                // Arrow Up: Move focus up in the pack list
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (_filteredIndices.Count > 0)
                    {
                        if (_keyboardFocusedRowIndex <= 0)
                            _keyboardFocusedRowIndex = _filteredIndices.Count - 1;
                        else
                            _keyboardFocusedRowIndex--;
                        RefreshPackListFocusIndicator();
                    }
                    return;
                }

                // Arrow Down: Move focus down in the pack list
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (_filteredIndices.Count > 0)
                    {
                        _keyboardFocusedRowIndex++;
                        if (_keyboardFocusedRowIndex >= _filteredIndices.Count)
                            _keyboardFocusedRowIndex = 0;
                        RefreshPackListFocusIndicator();
                    }
                    return;
                }

                // Enter/Space: Toggle the focused pack
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    if (_keyboardFocusedRowIndex >= 0 && _keyboardFocusedRowIndex < _filteredIndices.Count)
                    {
                        int realIndex = _filteredIndices[_keyboardFocusedRowIndex];
                        SelectPack(realIndex);
                        OnToggleSelected();
                    }
                    return;
                }
            }

            // Tab: Cycle focus between UI sections (works even with input focused)
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (inputFieldFocused)
                {
                    // If search input is focused, move to pack list
                    _keyboardFocusedRowIndex = 0;
                    RefreshPackListFocusIndicator();
                }
                else if (_keyboardFocusedRowIndex >= 0)
                {
                    // If pack list is focused, move back to search input
                    if (_searchInput != null)
                    {
                        EventSystem.current?.SetSelectedGameObject(_searchInput.gameObject);
                        _keyboardFocusedRowIndex = -1;
                        RefreshPackListFocusIndicator();
                    }
                }
                return;
            }

            // '/': Focus the search input (vim-style command prefix)
            if (Input.GetKeyDown(KeyCode.Slash))
            {
                if (_searchInput != null)
                {
                    EventSystem.current?.SetSelectedGameObject(_searchInput.gameObject);
                    _keyboardFocusedRowIndex = -1;
                    RefreshPackListFocusIndicator();
                }
                return;
            }

            // Ctrl+R: Reload packs
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    ClearCurrentSelection();
                    OnReloadRequested?.Invoke();
                    return;
                }

                // Ctrl+S: Reserved for profile save (placeholder)
                if (Input.GetKeyDown(KeyCode.S))
                {
                    // Future: implement profile save
                    return;
                }
            }
        }

        /// <summary>
        /// Refreshes the visual focus indicator on the pack list rows.
        /// Applies a colored border highlight to the focused row.
        /// </summary>
        private void RefreshPackListFocusIndicator()
        {
            if (_listContent == null)
                return;

            // Clear all focus borders
            for (int i = 0; i < _listContent.childCount; i++)
            {
                Transform child = _listContent.GetChild(i);
                Outline outline = child.GetComponent<Outline>();
                if (outline != null)
                    Destroy(outline);
            }

            // Apply focus border to the focused row
            if (_keyboardFocusedRowIndex >= 0 && _keyboardFocusedRowIndex < _listContent.childCount)
            {
                Transform focusedRow = _listContent.GetChild(_keyboardFocusedRowIndex);
                Outline outline = focusedRow.gameObject.AddComponent<Outline>();
                outline.effectColor = UiBuilder.Accent;
                outline.effectDistance = new Vector2(2f, 2f);
            }
        }

        // ── UI construction ────────────────────────────────────────────────────────

        private void BuildListPane(Transform parent)
        {
            _log?.LogInfo("[ModMenuPanel.BuildListPane] Starting pack list pane construction...");

            GameObject pane = new GameObject("ListPane", typeof(RectTransform));
            pane.transform.SetParent(parent, false);

            LayoutElement paneLe = pane.AddComponent<LayoutElement>();
            paneLe.preferredWidth = ListWidth;
            paneLe.minWidth = ListWidth;
            paneLe.flexibleHeight = 1f;  // CRITICAL: Allow ListPane to expand to fill parent height!

            VerticalLayoutGroup paneLayout = pane.AddComponent<VerticalLayoutGroup>();
            paneLayout.childForceExpandWidth = true;
            paneLayout.childForceExpandHeight = false;
            paneLayout.childControlWidth = true;
            // Fix (iter-149): must control child height so the pack-list scroll's
            // LayoutElement.flexibleHeight=1 is honored. With childControlHeight=false the
            // scroll got zero allocated height → the pack rows rendered off-viewport
            // (list appeared empty even though "N of M" counter was correct).
            paneLayout.childControlHeight = true;
            paneLayout.childAlignment = TextAnchor.UpperLeft;
            paneLayout.spacing = 0f;
            paneLayout.padding = new RectOffset(0, 0, 0, 0);

            // List header
            GameObject listHeader = UiBuilder.MakePanel(pane.transform, "ListHeader",
                UiBuilder.BgSurface, new Vector2(ListWidth, 32f));
            RectTransform lhRt = listHeader.GetComponent<RectTransform>();
            lhRt.anchorMin = new Vector2(0f, 1f);
            lhRt.anchorMax = new Vector2(1f, 1f);
            lhRt.pivot = new Vector2(0.5f, 1f);
            lhRt.sizeDelta = new Vector2(0f, 32f);

            UiBuilder.AddHorizontalLayout(listHeader, 4f, new RectOffset(8, 8, 6, 6));
            Text lhTitle = UiBuilder.MakeText(listHeader.transform, "ListTitle",
                "Loaded Packs", 12, UiBuilder.TextSecondary, bold: false);
            LayoutElement lhTitleLe = lhTitle.gameObject.AddComponent<LayoutElement>();
            lhTitleLe.flexibleWidth = 1f;
            LayoutElement listHeaderLe = listHeader.AddComponent<LayoutElement>();
            listHeaderLe.preferredWidth = ListWidth;
            listHeaderLe.minWidth = ListWidth;
            listHeaderLe.preferredHeight = 32f;

            // Build filter controls
            BuildListFilters(pane.transform);

            // Scroll view for pack items
            _log?.LogInfo("[ModMenuPanel.BuildListPane] Creating scroll view...");
            (ScrollRect scrollRect, RectTransform content) = UiBuilder.MakeScrollView(
                pane.transform, "PackListScroll",
                new Vector2(ListWidth, 0f));

            // Validate the result
            if (content == null || scrollRect == null)
            {
                _log?.LogError("[ModMenuPanel.BuildListPane] CRITICAL: MakeScrollView failed! " +
                    $"scrollRect={scrollRect != null}, content={content != null}. " +
                    "Pack list will not render. Check UiBuilder.MakeScrollView for exceptions.");
                _listContent = null;
                return;
            }

            // Fix #944/B1: Do NOT override offsetMin/Max with absolute values here.
            // The ListPane uses a VerticalLayoutGroup (header 32px + FilterContainer ~240px +
            // scroll). Manually setting offsetMax to -32px collapses the scroll rect behind the
            // filter bar. Let the LayoutGroup manage positions via LayoutElement.flexibleHeight.
            RectTransform scrollRt = scrollRect.GetComponent<RectTransform>();
            LayoutElement scrollLe = scrollRect.gameObject.AddComponent<LayoutElement>();
            scrollLe.preferredWidth = ListWidth;
            scrollLe.minWidth = ListWidth;
            scrollLe.flexibleHeight = 1f;  // fills remaining space below filter container

            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ListWidth);
            content.sizeDelta = new Vector2(ListWidth, content.sizeDelta.y);

            _listContent = content;
            _listScrollRect = scrollRect;
            _log?.LogInfo($"[ModMenuPanel.BuildListPane] Scroll view initialized successfully.");
            _log?.LogInfo($"  scrollRt.rect.size={scrollRt.rect.size} (viewport visible area)");
            _log?.LogInfo($"  scrollRt.sizeDelta={scrollRt.sizeDelta}, anchorMin={scrollRt.anchorMin}, anchorMax={scrollRt.anchorMax}");
            _log?.LogInfo($"  content.name={content.name}");
            _log?.LogInfo($"  content.active={content.gameObject.activeSelf}");
            _log?.LogInfo($"  content.anchorMin={content.anchorMin}, anchorMax={content.anchorMax}");
            _log?.LogInfo($"  content.sizeDelta={content.sizeDelta}");
            _log?.LogInfo($"  content.anchoredPosition={content.anchoredPosition}");
            _log?.LogInfo($"  ScrollRect component on: {scrollRect.name}");
            _log?.LogInfo($"  ScrollRect.content set to: {scrollRect.content?.name ?? "NULL"}");
            _log?.LogInfo($"  ScrollRect.vertical={scrollRect.vertical}");
            _log?.LogInfo($"  ScrollRect.enabled={scrollRect.enabled}");
            Image viewportImage = scrollRect.GetComponent<Image>();
            _log?.LogInfo($"  Viewport Image: exists={viewportImage != null}, raycastTarget={viewportImage?.raycastTarget}");

            // Verify components on content
            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            _log?.LogInfo($"  content has ContentSizeFitter: {csf != null} (verticalFit={csf?.verticalFit})");
            _log?.LogInfo($"  content has VerticalLayoutGroup: {vlg != null} (childForceExpandHeight={vlg?.childForceExpandHeight}, spacing={vlg?.spacing})");
        }

        private void RebuildPackList()
        {
            if (_listContent == null)
            {
                _log?.LogWarning("[ModMenuPanel.RebuildPackList] _listContent is NULL — UI not initialized yet. " +
                    "Pack list will render once Build() completes. Ensure DFCanvas.Start() runs before SetPacks() is called.");
                return;
            }

            // Reset keyboard focus when rebuilding the list
            _keyboardFocusedRowIndex = -1;

            _log?.LogInfo($"[ModMenuPanel.RebuildPackList] START: presenter.Packs.Count={_presenter.Packs.Count}, _listContent={_listContent.name}, active={_listContent.gameObject.activeSelf}");
            _log?.LogInfo($"[ModMenuPanel.RebuildPackList] _listContent RectTransform: position={_listContent.anchoredPosition}, sizeDelta={_listContent.sizeDelta}");
            _log?.LogInfo($"[ModMenuPanel.RebuildPackList] Clearing {_listContent.childCount} existing items");

            _listContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ListWidth);
            _listContent.sizeDelta = new Vector2(ListWidth, _listContent.sizeDelta.y);

            // Remove existing items immediately from the layout tree to avoid
            // same-frame duplicate entries when SetPacks triggers rapid rebuilds.
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                Transform child = _listContent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            _log?.LogInfo($"[ModMenuPanel.RebuildPackList] After clear: childCount={_listContent.childCount}. Now rendering {_presenter.Packs.Count} pack(s)...");

            for (int i = 0; i < _presenter.Packs.Count; i++)
            {
                _log?.LogInfo($"[ModMenuPanel.RebuildPackList] Creating item {i}: '{_presenter.Packs[i].Name}' (ID: {_presenter.Packs[i].Id})");
                BuildPackListItem(_presenter.Packs[i], i);

                Transform item = _listContent.GetChild(i);
                RectTransform rt = item.GetComponent<RectTransform>();
                _log?.LogInfo($"[ModMenuPanel.probe] item[{i}] name={item.name} childCount={item.transform.childCount} rect=({rt.sizeDelta.x}x{rt.sizeDelta.y}) active={item.gameObject.activeSelf}");
                for (int childIndex = 0; childIndex < item.transform.childCount; childIndex++)
                {
                    Transform child = item.transform.GetChild(childIndex);
                    Text text = child.GetComponent<Text>();
                    if (text != null)
                    {
                        RectTransform textRt = text.GetComponent<RectTransform>();
                        _log?.LogInfo($"[ModMenuPanel.probe] item[{i}].child[{childIndex}] Text color={text.color} text='{text.text}' active={text.gameObject.activeSelf} rect=({textRt.sizeDelta.x}x{textRt.sizeDelta.y})");
                    }

                    Image image = child.GetComponent<Image>();
                    if (image != null)
                    {
                        _log?.LogInfo($"[ModMenuPanel.probe] item[{i}].child[{childIndex}] Image color={image.color} spriteNull={image.sprite == null} raycastTarget={image.raycastTarget}");
                    }
                }
            }

            // CRITICAL FIX: Manually set content height since ContentSizeFitter is not calculating correctly
            // Calculate: padding.top + (itemCount * itemHeight) + (itemCount-1 * spacing) + padding.bottom
            float padding_top = 4f, padding_bottom = 4f, spacing = 2f, itemHeight = 40f;
            float calculatedHeight = padding_top + (_presenter.Packs.Count * itemHeight) + (Mathf.Max(0, _presenter.Packs.Count - 1) * spacing) + padding_bottom;
            _listContent.sizeDelta = new Vector2(_listContent.sizeDelta.x, calculatedHeight);
            _log?.LogInfo($"[ModMenuPanel.RebuildPackList] MANUAL FIX APPLIED: Set content height to {calculatedHeight} (was {_listContent.sizeDelta.y})");

            _log?.LogInfo($"[ModMenuPanel.RebuildPackList] COMPLETE: childCount={_listContent.childCount}. Listing items:");
            for (int i = 0; i < _listContent.childCount; i++)
            {
                Transform child = _listContent.GetChild(i);
                RectTransform childRt = child.GetComponent<RectTransform>();
                _log?.LogInfo($"  Item {i}: name={child.name}, active={child.gameObject.activeSelf}, sizeDelta={childRt.sizeDelta}, childCount={child.childCount}");
            }

            // CRITICAL: Log content size AFTER all items are created and VerticalLayoutGroup has calculated
            _log?.LogInfo($"[ModMenuPanel.RebuildPackList] FINAL CONTENT SIZE: sizeDelta={_listContent.sizeDelta}, rect.height={_listContent.rect.height}");
            ContentSizeFitter csf = _listContent.GetComponent<ContentSizeFitter>();
            VerticalLayoutGroup vlg = _listContent.GetComponent<VerticalLayoutGroup>();
            _log?.LogInfo($"[ModMenuPanel.RebuildPackList] ContentSizeFitter: {(csf != null ? $"enabled={csf.enabled}, verticalFit={csf.verticalFit}" : "NULL")}");
            _log?.LogInfo($"[ModMenuPanel.RebuildPackList] VerticalLayoutGroup: {(vlg != null ? $"enabled={vlg.enabled}, spacing={vlg.spacing}, padding={vlg.padding}, preferredHeight={vlg.preferredHeight}" : "NULL")}");

            // Calculate expected height manually
            float expectedHeight = 0f;
            if (vlg != null)
            {
                expectedHeight = vlg.padding.top + vlg.padding.bottom;
                for (int i = 0; i < _listContent.childCount; i++)
                {
                    Transform child = _listContent.GetChild(i);
                    LayoutElement childLe = child.GetComponent<LayoutElement>();
                    if (childLe != null && childLe.preferredHeight > 0)
                    {
                        expectedHeight = expectedHeight + childLe.preferredHeight;
                        if (i > 0) expectedHeight = expectedHeight + vlg.spacing;
                    }
                }
            }
            _log?.LogInfo($"[ModMenuPanel.RebuildPackList] MANUAL CALCULATION: expected total height={expectedHeight} (padding.top={vlg?.padding.top}, padding.bottom={vlg?.padding.bottom}, spacing={vlg?.spacing}, items={_listContent.childCount})");

            // Drive layout immediately so the ScrollRect sees the correct content bounds
            // even when the panel is currently hidden.
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);
        }

        private void BuildListFilters(Transform parent)
        {
            // Section header bar with colored accent
            GameObject headerBar = new GameObject("FilterHeader", typeof(RectTransform));
            headerBar.transform.SetParent(parent, false);
            RectTransform headerBarRt = headerBar.GetComponent<RectTransform>();

            // Top colored bar (4px)
            GameObject colorBar = UiBuilder.MakePanel(headerBar.transform, "AccentBar",
                UiBuilder.Accent, new Vector2(0f, 4f));
            RectTransform colorBarRt = colorBar.GetComponent<RectTransform>();
            colorBarRt.anchorMin = new Vector2(0f, 1f);
            colorBarRt.anchorMax = Vector2.one;
            colorBarRt.pivot = new Vector2(0.5f, 1f);
            colorBarRt.offsetMin = Vector2.zero;
            colorBarRt.offsetMax = new Vector2(0f, -4f);
            colorBarRt.sizeDelta = new Vector2(0f, 4f);

            // Fix (iter-149): the AccentBar is absolutely positioned (top-stretch). The
            // parent HorizontalLayoutGroup below would otherwise treat it as a flex child
            // and stretch it into a large green bar overlapping the FILTERS label. Opt it
            // out of layout so it stays a 4px accent line.
            LayoutElement colorBarLe = colorBar.AddComponent<LayoutElement>();
            colorBarLe.ignoreLayout = true;

            LayoutElement headerLe = headerBar.AddComponent<LayoutElement>();
            headerLe.preferredHeight = 28f;

            HorizontalLayoutGroup headerLayout = headerBar.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(8, 8, 4, 4);
            headerLayout.spacing = 0f;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            Text headerTitle = UiBuilder.MakeText(headerBar.transform, "HeaderTitle", L10n.T("menu.filter.label", "FILTERS"), 12,
                UiBuilder.TextSecondary, bold: true);
            LayoutElement headerTitleLe = headerTitle.gameObject.AddComponent<LayoutElement>();
            headerTitleLe.flexibleWidth = 1f;

            // Filters container
            GameObject filterContainer = new GameObject("FilterContainer", typeof(RectTransform));
            filterContainer.transform.SetParent(parent, false);
            RectTransform fcRt = filterContainer.GetComponent<RectTransform>();

            VerticalLayoutGroup fcLayout = filterContainer.AddComponent<VerticalLayoutGroup>();
            fcLayout.childForceExpandWidth = true;
            fcLayout.childForceExpandHeight = false;
            fcLayout.spacing = 4f;
            fcLayout.padding = new RectOffset(8, 8, 8, 8);

            LayoutElement fcLe = filterContainer.AddComponent<LayoutElement>();
            fcLe.preferredHeight = 212f;  // Enough for profiles row + 3 rows of controls
            fcLe.minHeight = 160f;
            fcLe.flexibleWidth = 1f;

            // ── Profiles row (#918) ──────────────────────────────────────────
            BuildProfilesSection(filterContainer.transform);

            // Search row
            GameObject searchRow = new GameObject("SearchRow", typeof(RectTransform));
            searchRow.transform.SetParent(filterContainer.transform, false);
            HorizontalLayoutGroup searchHlg = searchRow.AddComponent<HorizontalLayoutGroup>();
            searchHlg.spacing = 4f;
            searchHlg.childForceExpandWidth = true;
            searchHlg.childForceExpandHeight = false;
            LayoutElement searchRowLe = searchRow.AddComponent<LayoutElement>();
            searchRowLe.preferredHeight = 28f;

            _searchInput = UiBuilder.MakeInputField(searchRow.transform, "SearchInput", L10n.T("menu.search.placeholder", "Search packs..."),
                OnSearchChanged);
            LayoutElement searchInputLe = _searchInput.gameObject.AddComponent<LayoutElement>();
            searchInputLe.preferredHeight = 28f;
            searchInputLe.flexibleWidth = 1f;
            searchInputLe.minWidth = 100f;

            // Filter row 1: Tier filter
            GameObject filterRow1 = new GameObject("FilterRow1", typeof(RectTransform));
            filterRow1.transform.SetParent(filterContainer.transform, false);
            HorizontalLayoutGroup filterHlg1 = filterRow1.AddComponent<HorizontalLayoutGroup>();
            filterHlg1.spacing = 4f;
            filterHlg1.childForceExpandWidth = false;
            filterHlg1.childForceExpandHeight = false;
            LayoutElement filterRow1Le = filterRow1.AddComponent<LayoutElement>();
            filterRow1Le.preferredHeight = 28f;
            filterRow1Le.flexibleWidth = 1f;

            Text tierLabel = UiBuilder.MakeText(filterRow1.transform, "TierLabel", "Tier:", 11, UiBuilder.TextSecondary);
            LayoutElement tierLabelLe = tierLabel.gameObject.AddComponent<LayoutElement>();
            tierLabelLe.preferredWidth = 40f;

            _tierDropdown = MakeDropdown(filterRow1.transform, "TierDropdown",
                new[] { L10n.T("menu.filter.tier.all", "All"), L10n.T("menu.filter.tier.engine_extension", "Engine Extension"), L10n.T("menu.filter.tier.content", "Content"), L10n.T("menu.filter.tier.total_conversion", "Total Conversion"), L10n.T("menu.filter.tier.baseline", "Baseline") },
                OnTierFilterChanged);
            LayoutElement tierDdLe = _tierDropdown.gameObject.AddComponent<LayoutElement>();
            tierDdLe.preferredWidth = 90f;
            tierDdLe.minWidth = 80f;
            tierDdLe.preferredHeight = 28f;

            // Filter row 2: State filter
            GameObject filterRow2 = new GameObject("FilterRow2", typeof(RectTransform));
            filterRow2.transform.SetParent(filterContainer.transform, false);
            HorizontalLayoutGroup filterHlg2 = filterRow2.AddComponent<HorizontalLayoutGroup>();
            filterHlg2.spacing = 4f;
            filterHlg2.childForceExpandWidth = false;
            filterHlg2.childForceExpandHeight = false;
            LayoutElement filterRow2Le = filterRow2.AddComponent<LayoutElement>();
            filterRow2Le.preferredHeight = 28f;
            filterRow2Le.flexibleWidth = 1f;

            Text stateLabel = UiBuilder.MakeText(filterRow2.transform, "StateLabel", L10n.T("menu.filter.state", "State:"), 11, UiBuilder.TextSecondary);
            LayoutElement stateLabelLe = stateLabel.gameObject.AddComponent<LayoutElement>();
            stateLabelLe.preferredWidth = 40f;

            _stateDropdown = MakeDropdown(filterRow2.transform, "StateDropdown",
                new[] { L10n.T("menu.filter.state.all", "All"), L10n.T("menu.filter.state.enabled", "Enabled"), L10n.T("menu.filter.state.disabled", "Disabled"), L10n.T("menu.filter.state.errors", "Has Errors") },
                OnStateFilterChanged);
            LayoutElement stateDdLe = _stateDropdown.gameObject.AddComponent<LayoutElement>();
            stateDdLe.preferredWidth = 90f;
            stateDdLe.minWidth = 80f;
            stateDdLe.preferredHeight = 28f;

            // Sort row
            GameObject sortRow = new GameObject("SortRow", typeof(RectTransform));
            sortRow.transform.SetParent(filterContainer.transform, false);
            HorizontalLayoutGroup sortHlg = sortRow.AddComponent<HorizontalLayoutGroup>();
            sortHlg.spacing = 4f;
            sortHlg.childForceExpandWidth = false;
            sortHlg.childForceExpandHeight = false;
            LayoutElement sortRowLe = sortRow.AddComponent<LayoutElement>();
            sortRowLe.preferredHeight = 28f;
            sortRowLe.flexibleWidth = 1f;

            Text sortLabel = UiBuilder.MakeText(sortRow.transform, "SortLabel", L10n.T("menu.sort.label", "Sort:"), 11, UiBuilder.TextSecondary);
            LayoutElement sortLabelLe = sortLabel.gameObject.AddComponent<LayoutElement>();
            sortLabelLe.preferredWidth = 40f;

            _sortDropdown = MakeDropdown(sortRow.transform, "SortDropdown",
                new[] { L10n.T("menu.sort.name", "Name"), L10n.T("menu.sort.type", "Type"), L10n.T("menu.sort.version", "Version") },
                OnSortChanged);
            LayoutElement sortDdLe = _sortDropdown.gameObject.AddComponent<LayoutElement>();
            sortDdLe.preferredWidth = 90f;
            sortDdLe.minWidth = 80f;
            sortDdLe.preferredHeight = 28f;

            // Counter text (flexible space, then counter)
            GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(sortRow.transform, false);
            LayoutElement spacerLe = spacer.AddComponent<LayoutElement>();
            spacerLe.flexibleWidth = 1f;

            _listCounterText = UiBuilder.MakeText(sortRow.transform, "CounterText", "0 of 0", 10, UiBuilder.TextSecondary);
            LayoutElement counterLe = _listCounterText.gameObject.AddComponent<LayoutElement>();
            counterLe.preferredWidth = 60f;
        }

        /// <summary>Creates a dropdown with the given options and callback.</summary>
        private Dropdown MakeDropdown(Transform parent, string name, string[] options, Action<int> onValueChanged)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
            go.transform.SetParent(parent, false);

            Image bgImg = go.GetComponent<Image>();
            bgImg.color = UiBuilder.BgDeep;

            Dropdown dropdown = go.GetComponent<Dropdown>();

            // Populate options
            dropdown.options.Clear();
            foreach (string option in options)
            {
                dropdown.options.Add(new Dropdown.OptionData(option));
            }
            dropdown.value = 0;

            // Setup label
            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(go.transform, false);
            RectTransform labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(8f, 0f);
            labelRt.offsetMax = new Vector2(-8f, 0f);

            Text labelText = label.GetComponent<Text>();
            labelText.text = options[0];
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 12;
            labelText.color = UiBuilder.TextPrimary;
            labelText.alignment = TextAnchor.MiddleLeft;

            dropdown.targetGraphic = bgImg;
            dropdown.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<int>(onValueChanged));

            return dropdown;
        }

        private void OnSearchChanged(string text)
        {
            _searchText = text;
            ApplyFilters();
        }

        private void OnTierFilterChanged(int index)
        {
            if (_tierDropdown != null)
                _tierFilter = _tierDropdown.options[index].text;
            ApplyFilters();
        }

        private void OnStateFilterChanged(int index)
        {
            if (_stateDropdown != null)
                _stateFilter = _stateDropdown.options[index].text;
            ApplyFilters();
        }

        private void OnSortChanged(int index)
        {
            if (_sortDropdown != null)
                _sortBy = _sortDropdown.options[index].text;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            _filteredIndices.Clear();

            // Fix #944/B3: use dropdown VALUE (index) to compare filters, NOT localized option text.
            // Comparing against "All"/"Engine Extension"/etc. breaks once i18n renames the labels
            // (e.g. "All Tiers" in some locales). Index 0 always means "no filter" regardless of locale.
            int tierIndex = _tierDropdown != null ? _tierDropdown.value : 0;
            int stateIndex = _stateDropdown != null ? _stateDropdown.value : 0;

            for (int i = 0; i < _presenter.Packs.Count; i++)
            {
                PackDisplayInfo pack = _presenter.Packs[i];

                // Search filter
                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    string searchLower = _searchText.ToLowerInvariant();
                    if (!pack.Name.ToLowerInvariant().Contains(searchLower) &&
                        !pack.Id.ToLowerInvariant().Contains(searchLower))
                    {
                        continue;
                    }
                }

                // Tier filter (index 0 = All; 1 = Engine Extension, 2 = Content, 3 = Total Conversion, 4 = Baseline)
                if (tierIndex != 0)
                {
                    bool matches = false;
                    if (tierIndex == 1 && pack.Classification == "engine_extension") matches = true;
                    if (tierIndex == 2 && pack.Classification == "content") matches = true;
                    if (tierIndex == 3 && pack.Classification == "total_conversion") matches = true;
                    if (tierIndex == 4 && pack.Classification == "baseline") matches = true;
                    if (!matches) continue;
                }

                // State filter (index 0 = All; 1 = Enabled, 2 = Disabled, 3 = Has Errors)
                if (stateIndex != 0)
                {
                    bool matches = false;
                    if (stateIndex == 1 && pack.IsEnabled) matches = true;
                    if (stateIndex == 2 && !pack.IsEnabled) matches = true;
                    if (stateIndex == 3 && pack.Errors.Count > 0) matches = true;
                    if (!matches) continue;
                }

                _filteredIndices.Add(i);
            }

            // Apply sorting
            ApplySorting();

            // Update counter
            if (_listCounterText != null)
            {
                _listCounterText.text = $"{_filteredIndices.Count} of {_presenter.Packs.Count}";
            }

            // Rebuild the filtered list
            RebuildFilteredPackList();
        }

        /// <summary>Sorts the filtered pack indices according to the current sort selection.</summary>
        private void ApplySorting()
        {
            if (_sortBy == "Type")
            {
                _filteredIndices.Sort((a, b) =>
                    _presenter.Packs[a].Type.CompareTo(_presenter.Packs[b].Type));
            }
            else if (_sortBy == "Version")
            {
                // Simple string version sort (not semver)
                _filteredIndices.Sort((a, b) =>
                    _presenter.Packs[a].Version.CompareTo(_presenter.Packs[b].Version));
            }
            else // Name (default)
            {
                _filteredIndices.Sort((a, b) =>
                    _presenter.Packs[a].Name.CompareTo(_presenter.Packs[b].Name));
            }
        }

        private void RebuildFilteredPackList()
        {
            if (_listContent == null) return;

            // Clear existing items
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                Transform child = _listContent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            // Render filtered items
            for (int displayIndex = 0; displayIndex < _filteredIndices.Count; displayIndex++)
            {
                int realIndex = _filteredIndices[displayIndex];
                BuildPackListItem(_presenter.Packs[realIndex], realIndex);
            }

            // Update content height
            float padding_top = 4f, padding_bottom = 4f, spacing = 2f, itemHeight = 40f;
            float calculatedHeight = padding_top + (_filteredIndices.Count * itemHeight) +
                                      (Mathf.Max(0, _filteredIndices.Count - 1) * spacing) + padding_bottom;
            _listContent.sizeDelta = new Vector2(_listContent.sizeDelta.x, calculatedHeight);

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);

            // Fix (iter-149): DINO never runs ScrollRect.LateUpdate, so pin the freshly
            // built list back to the top of the viewport (otherwise a stale clamp can leave
            // the rows scrolled out of view).
            if (_listScrollRect != null)
                _listScrollRect.verticalNormalizedPosition = 1f;
        }

        private void BuildPackListItem(PackDisplayInfo pack, int index)
        {
            if (_listContent == null)
            {
                _log?.LogWarning($"[ModMenuPanel.BuildPackListItem] _listContent is NULL for pack '{pack.Id}' — item {index} skipped.");
                return;
            }

            _log?.LogInfo($"[ModMenuPanel.BuildPackListItem] Starting item {index}: '{pack.Name}' (enabled={pack.IsEnabled}, selected={index == _presenter.SelectedIndex})");

            bool isSelected = index == _presenter.SelectedIndex;
            bool hasErrors = pack.Errors.Count > 0;
            bool hasConflicts = pack.Conflicts.Count > 0;

            // Zebra striping: alternate row colors
            // Even rows (#1A1F2E) / Odd rows (#1F2536)
            bool isEvenRow = index % 2 == 0;
            Color bgColor = isSelected ? UiBuilder.BgSurface : (isEvenRow ? HexColor("#1A1F2E") : HexColor("#1F2536"));
            Color alpha = pack.IsEnabled ? Color.white : new Color(1f, 1f, 1f, 0.6f);

            GameObject card = UiBuilder.MakePanel(_listContent, $"PackItem_{pack.Id}", bgColor, new Vector2(0f, ItemHeight));
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ListWidth - 8f);
            cardRt.sizeDelta = new Vector2(ListWidth - 8f, ItemHeight);
            _log?.LogInfo($"[ModMenuPanel.BuildPackListItem] Item {index} card created: sizeDelta={cardRt.sizeDelta}, active={card.activeSelf}");

            LayoutElement cardLe = card.AddComponent<LayoutElement>();
            cardLe.minWidth = ListWidth - 8f;
            cardLe.preferredWidth = ListWidth - 8f;
            cardLe.minHeight = ItemHeight;
            cardLe.preferredHeight = ItemHeight;
            cardLe.flexibleWidth = 1f;
            _log?.LogInfo($"[ModMenuPanel.BuildPackListItem] Item {index} LayoutElement set: minHeight={cardLe.minHeight}, preferredHeight={cardLe.preferredHeight}");

            // Content layout with improved spacing (8px padding, 4px rows)
            HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            // Status indicator dot (green=enabled, red=error, yellow=conflict)
            Color dotColor = hasErrors ? UiBuilder.Error : (hasConflicts ? UiBuilder.Warning : (pack.IsEnabled ? UiBuilder.Success : new Color(0.5f, 0.5f, 0.5f, 1f)));
            GameObject dotGo = UiBuilder.MakePanel(card.transform, "StatusDot", dotColor, new Vector2(8f, 8f));
            RectTransform dotRt = dotGo.GetComponent<RectTransform>();
            LayoutElement dotLe = dotGo.AddComponent<LayoutElement>();
            dotLe.preferredWidth = 8f;
            dotLe.preferredHeight = 8f;
            dotLe.minWidth = 8f;
            dotLe.minHeight = 8f;

            // Pack name
            Color nameColor = pack.IsEnabled ? UiBuilder.TextPrimary : UiBuilder.TextSecondary;
            Text nameText = UiBuilder.MakeText(card.transform, "PackName", pack.Name, 13,
                nameColor, bold: isSelected);
            RectTransform nameTextRt = nameText.GetComponent<RectTransform>();
            _log?.LogInfo($"[ModMenuPanel.BuildPackListItem] Item {index} nameText created: text='{pack.Name}', fontSize={nameText.fontSize}, color={nameColor}, sizeDelta={nameTextRt.sizeDelta}, font={nameText.font?.name}");

            if (!pack.IsEnabled)
            {
                nameText.color = new Color(nameColor.r, nameColor.g, nameColor.b, 0.6f);
            }
            LayoutElement nameLe = nameText.gameObject.AddComponent<LayoutElement>();
            nameLe.minWidth = 100f;
            nameLe.flexibleWidth = 1f;
            nameLe.minHeight = 16f;
            nameLe.preferredHeight = ItemHeight - 8f;
            _log?.LogInfo($"[ModMenuPanel.BuildPackListItem] Item {index} nameText LayoutElement: minWidth={nameLe.minWidth}, flexibleWidth={nameLe.flexibleWidth}, minHeight={nameLe.minHeight}");

            // Error / Conflict badge
            if (hasErrors)
            {
                GameObject badge = UiBuilder.MakePanel(card.transform, "ErrorBadge",
                    UiBuilder.Error, new Vector2(32f, 18f));
                LayoutElement badgeLe = badge.AddComponent<LayoutElement>();
                badgeLe.preferredWidth = 32f;
                badgeLe.preferredHeight = 18f;

                Text badgeText = UiBuilder.MakeText(badge.transform, "BadgeText", "ERR",
                    10, Color.white, bold: true, TextAnchor.MiddleCenter);
                UiBuilder.FillParent(badgeText.GetComponent<RectTransform>());
            }
            else if (hasConflicts)
            {
                GameObject badge = UiBuilder.MakePanel(card.transform, "ConflictBadge",
                    UiBuilder.Warning, new Vector2(40f, 18f));
                LayoutElement badgeLe = badge.AddComponent<LayoutElement>();
                badgeLe.preferredWidth = 40f;
                badgeLe.preferredHeight = 18f;

                Text badgeText = UiBuilder.MakeText(badge.transform, "BadgeText", "CONF",
                    10, Color.black, bold: true, TextAnchor.MiddleCenter);
                UiBuilder.FillParent(badgeText.GetComponent<RectTransform>());
            }

            // Version label
            Text versionText = UiBuilder.MakeText(card.transform, "Version",
                $"v{pack.Version}", 11, UiBuilder.TextSecondary);
            LayoutElement verLe = versionText.gameObject.AddComponent<LayoutElement>();
            verLe.preferredWidth = 50f;
            verLe.minWidth = 40f;
            verLe.minHeight = 16f;

            // Click to select
            int capturedIndex = index;
            Button btn = card.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            // Hover: lighten by ~10%
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.1f);
            cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.15f);
            cb.selectedColor = bgColor;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.05f;
            btn.colors = cb;
            btn.targetGraphic = card.GetComponent<Image>();
            btn.onClick.AddListener(() => SelectPack(capturedIndex));
        }

        private void SelectPack(int index)
        {
            _presenter.SelectIndex(index);
            ClearCurrentSelection();
            QueueListRefresh();
        }

        private void QueueListRefresh()
        {
            if (_listRefreshQueued) return;
            _listRefreshQueued = true;
            StartCoroutine(RefreshListNextFrame());
        }

        private IEnumerator RefreshListNextFrame()
        {
            yield return null;
            _listRefreshQueued = false;
            RebuildPackList();
            RefreshDetail();
        }

        private static void ClearCurrentSelection()
        {
            try
            {
                EventSystem current = EventSystem.current;
                if (current != null)
                {
                    current.SetSelectedGameObject(null);
                }
            }
            catch { } // safe-swallow: UI selection cleanup is best-effort
        }
    }
}
