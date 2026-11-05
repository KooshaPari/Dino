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

        private void BuildTelemetrySection(Transform parent)
        {
            UiBuilder.MakeHorizontalSeparator(parent, UiBuilder.Border);

            // Section header row
            GameObject headerRow = new GameObject("TelemetryHeader", typeof(RectTransform));
            headerRow.transform.SetParent(parent, false);
            HorizontalLayoutGroup hHlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            hHlg.spacing = 6f;
            hHlg.childForceExpandHeight = false;
            hHlg.childForceExpandWidth = false;
            hHlg.padding = new RectOffset(0, 0, 4, 2);
            headerRow.AddComponent<LayoutElement>().preferredHeight = 24f;

            Text sectionTitle = UiBuilder.MakeText(headerRow.transform, "TelemetryTitle",
                "TELEMETRY", 11, UiBuilder.Accent, bold: true);
            sectionTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // "Copy to Clipboard" button
            Button copyBtn = UiBuilder.MakeButton(
                headerRow.transform, "CopyMetricsBtn", "Copy",
                UiBuilder.BgSurface, UiBuilder.TextSecondary,
                OnCopyMetricsToClipboard);
            LayoutElement copyLe = copyBtn.gameObject.AddComponent<LayoutElement>();
            copyLe.preferredWidth = 48f;
            copyLe.preferredHeight = 20f;

            // "Refresh" button (manual trigger)
            Button refreshBtn = UiBuilder.MakeButton(
                headerRow.transform, "RefreshMetricsBtn", "↺",
                UiBuilder.BgSurface, UiBuilder.TextSecondary,
                RefreshTelemetryText);
            LayoutElement refreshLe = refreshBtn.gameObject.AddComponent<LayoutElement>();
            refreshLe.preferredWidth = 24f;
            refreshLe.preferredHeight = 20f;

            // Text area for metrics dump
            _telemetryText = UiBuilder.MakeText(parent, "TelemetryText",
                "(no metrics yet)", 10, new Color(0.65f, 0.75f, 0.65f, 1f));
            LayoutElement textLe = _telemetryText.gameObject.AddComponent<LayoutElement>();
            textLe.preferredHeight = 120f;
            textLe.flexibleWidth = 1f;
            _telemetryText.verticalOverflow = VerticalWrapMode.Overflow;
            _telemetryText.alignment = TextAnchor.UpperLeft;

            // Populate immediately
            RefreshTelemetryText();

            // Start the 2-second auto-refresh coroutine
            if (_telemetryRefreshCoroutine != null)
                StopCoroutine(_telemetryRefreshCoroutine);
            _telemetryRefreshCoroutine = StartCoroutine(TelemetryAutoRefreshCoroutine());
        }


        private void RefreshTelemetryText()
        {
            if (_telemetryText == null) return;
            try
            {
                string md = MetricsCollector.Instance.DumpMarkdown();
                _telemetryText.text = string.IsNullOrWhiteSpace(md) ? "(no metrics recorded yet)" : md;
            }
            catch (Exception ex)
            {
                // best-effort: telemetry display must never crash the UI
                if (_telemetryText != null)
                    _telemetryText.text = $"(metrics error: {ex.Message})";
            }
        }


        private void OnCopyMetricsToClipboard()
        {
            try
            {
                string md = MetricsCollector.Instance.DumpMarkdown();
                GUIUtility.systemCopyBuffer = string.IsNullOrWhiteSpace(md) ? "(no metrics)" : md;
            }
            catch (Exception ex)
            {
                // best-effort
                System.Diagnostics.Debug.WriteLine($"ModMenuPanel metrics clipboard copy failed: {ex.Message}");
            }
        }


        private IEnumerator TelemetryAutoRefreshCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(TelemetryRefreshIntervalSec);
                RefreshTelemetryText();
            }
        }

        // ─────────────────────────────────────────────────────────────────────

    }
}