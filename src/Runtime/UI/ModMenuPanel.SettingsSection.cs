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

        private void RefreshSettings(PackDisplayInfo p)
        {
            if (_settingsSection == null || _settingsContent == null) return;

            // Clear existing controls
            for (int i = _settingsContent.childCount - 1; i >= 0; i--)
            {
                Transform child = _settingsContent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            // Get settings from the pack manifest
            if (p.Settings == null || p.Settings.Count == 0)
            {
                _settingsSection.SetActive(false);
                return;
            }

            _settingsSection.SetActive(true);

            // Add a header
            Text settingsHeader = UiBuilder.MakeText(_settingsContent, "SettingsHeader",
                "Pack Settings", 12, UiBuilder.Accent, bold: true);
            settingsHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            UiBuilder.MakeHorizontalSeparator(_settingsContent, UiBuilder.Border);

            // Get the settings store
            var settingsStore = Settings.PackSettingsStore.Instance;

            // Render each setting
            foreach (SDK.Models.PackSetting setting in p.Settings)
            {
                BuildSettingControl(_settingsContent, p.Id, setting, settingsStore);
            }
        }


        private void BuildSettingControl(RectTransform parent, string packId,
            SDK.Models.PackSetting setting, Settings.PackSettingsStore settingsStore)
        {
            // Container for this setting
            GameObject row = new GameObject($"Setting_{setting.Key}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            VerticalLayoutGroup rowVlg = row.AddComponent<VerticalLayoutGroup>();
            rowVlg.spacing = 4f;
            rowVlg.childForceExpandWidth = true;
            rowVlg.childForceExpandHeight = false;
            LayoutElement rowLe = row.AddComponent<LayoutElement>();
            rowLe.flexibleWidth = 1f;

            // Label
            Text label = UiBuilder.MakeText(row.transform, "Label", setting.DisplayName, 11,
                UiBuilder.TextPrimary);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            // Control based on type
            object currentValue = setting.DefaultValue ?? GetDefaultForType(setting.Type);
            currentValue = settingsStore.Get(packId, setting.Key, currentValue);

            if (setting.Type == "bool")
            {
                BuildBoolControl(row.transform, packId, setting, currentValue, settingsStore);
            }
            else if (setting.Type == "int" || setting.Type == "float")
            {
                BuildSliderControl(row.transform, packId, setting, currentValue, settingsStore);
            }
            else if (setting.Type == "enum")
            {
                BuildEnumControl(row.transform, packId, setting, currentValue, settingsStore);
            }
            else if (setting.Type == "string")
            {
                BuildStringControl(row.transform, packId, setting, currentValue, settingsStore);
            }

            // Optional description
            if (!string.IsNullOrEmpty(setting.Description))
            {
                Text desc = UiBuilder.MakeText(row.transform, "Description",
                    setting.Description, 9, UiBuilder.TextSecondary);
                desc.verticalOverflow = VerticalWrapMode.Overflow;
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                LayoutElement descLe = desc.gameObject.AddComponent<LayoutElement>();
                descLe.flexibleWidth = 1f;
                descLe.preferredHeight = 20f;
            }
        }


        private void BuildBoolControl(Transform parent, string packId,
            SDK.Models.PackSetting setting, object currentValue, Settings.PackSettingsStore settingsStore)
        {
            GameObject toggleGo = new GameObject("BoolControl", typeof(RectTransform));
            toggleGo.transform.SetParent(parent, false);

            Toggle toggle = toggleGo.AddComponent<Toggle>();
            toggle.isOn = currentValue is bool b ? b : false;

            Image toggleBg = UiBuilder.MakePanel(toggleGo.transform, "Background",
                UiBuilder.BgSurface, new Vector2(30f, 20f)).GetComponent<Image>();

            Image checkmark = UiBuilder.MakePanel(toggleGo.transform, "Checkmark",
                UiBuilder.Accent, new Vector2(14f, 14f)).GetComponent<Image>();
            toggle.graphic = checkmark;

            string capturedKey = setting.Key;
            toggle.onValueChanged.AddListener(newValue =>
            {
                settingsStore.Set(packId, capturedKey, newValue);
            });

            LayoutElement toggleLe = toggleGo.AddComponent<LayoutElement>();
            toggleLe.preferredWidth = 30f;
            toggleLe.preferredHeight = 20f;
        }


        private void BuildSliderControl(Transform parent, string packId,
            SDK.Models.PackSetting setting, object currentValue, Settings.PackSettingsStore settingsStore)
        {
            GameObject container = new GameObject("SliderControl", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            Text valueLabel = UiBuilder.MakeText(container.transform, "ValueLabel",
                FormatSettingValue(currentValue), 10, UiBuilder.TextSecondary);
            LayoutElement valueLe = valueLabel.gameObject.AddComponent<LayoutElement>();
            valueLe.preferredWidth = 50f;

            GameObject sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(container.transform, false);
            Slider slider = sliderGo.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;

            Image sliderBg = UiBuilder.MakePanel(sliderGo.transform, "Background",
                UiBuilder.BgSurface, new Vector2(150f, 10f)).GetComponent<Image>();
            slider.targetGraphic = sliderBg;

            GameObject handleGo = UiBuilder.MakePanel(sliderGo.transform, "Handle",
                UiBuilder.Accent, new Vector2(12f, 20f));
            slider.handleRect = handleGo.GetComponent<RectTransform>();

            double minVal = setting.Min ?? 0.0;
            double maxVal = setting.Max ?? 100.0;
            slider.minValue = (float)minVal;
            slider.maxValue = (float)maxVal;
            slider.value = ConvertToFloat(currentValue);

            string capturedKey = setting.Key;
            slider.onValueChanged.AddListener(newValue =>
            {
                object valueToStore = setting.Type == "int" ? (object)(int)newValue : (object)newValue;
                settingsStore.Set(packId, capturedKey, valueToStore);
                if (valueLabel != null)
                    valueLabel.text = FormatSettingValue(valueToStore);
            });

            LayoutElement sliderLe = sliderGo.AddComponent<LayoutElement>();
            sliderLe.flexibleWidth = 1f;
            sliderLe.preferredHeight = 20f;
        }


        private void BuildEnumControl(Transform parent, string packId,
            SDK.Models.PackSetting setting, object currentValue, Settings.PackSettingsStore settingsStore)
        {
            GameObject dropdownGo = new GameObject("EnumControl", typeof(RectTransform));
            dropdownGo.transform.SetParent(parent, false);

            Dropdown dropdown = dropdownGo.AddComponent<Dropdown>();

            if (setting.EnumOptions != null)
            {
                foreach (string option in setting.EnumOptions)
                {
                    dropdown.options.Add(new Dropdown.OptionData(option));
                }
            }

            string currentStr = currentValue?.ToString() ?? "";
            for (int i = 0; i < dropdown.options.Count; i++)
            {
                if (dropdown.options[i].text == currentStr)
                {
                    dropdown.value = i;
                    break;
                }
            }

            string capturedKey = setting.Key;
            dropdown.onValueChanged.AddListener(index =>
            {
                if (index >= 0 && index < dropdown.options.Count)
                {
                    string selectedValue = dropdown.options[index].text;
                    settingsStore.Set(packId, capturedKey, selectedValue);
                }
            });

            LayoutElement dropdownLe = dropdownGo.AddComponent<LayoutElement>();
            dropdownLe.flexibleWidth = 1f;
            dropdownLe.preferredHeight = 28f;
        }


        private void BuildStringControl(Transform parent, string packId,
            SDK.Models.PackSetting setting, object currentValue, Settings.PackSettingsStore settingsStore)
        {
            GameObject inputGo = new GameObject("StringControl", typeof(RectTransform));
            inputGo.transform.SetParent(parent, false);

            InputField inputField = inputGo.AddComponent<InputField>();
            inputField.text = currentValue?.ToString() ?? "";
            inputField.lineType = InputField.LineType.SingleLine;

            Image inputBg = UiBuilder.MakePanel(inputGo.transform, "Background",
                UiBuilder.BgSurface, new Vector2(200f, 28f)).GetComponent<Image>();
            inputField.targetGraphic = inputBg;

            Text placeholder = UiBuilder.MakeText(inputGo.transform, "Placeholder",
                "Enter value...", 11, UiBuilder.TextSecondary);
            inputField.placeholder = placeholder;

            string capturedKey = setting.Key;
            inputField.onEndEdit.AddListener(newValue =>
            {
                settingsStore.Set(packId, capturedKey, newValue);
            });

            LayoutElement inputLe = inputGo.AddComponent<LayoutElement>();
            inputLe.flexibleWidth = 1f;
            inputLe.preferredHeight = 28f;
        }

        private object GetDefaultForType(string type) => type switch
        {
            "bool" => false,
            "int" => 0,
            "float" => 0.0f,
            "string" => "",
            "enum" => "",
            _ => ""
        };

        private float ConvertToFloat(object value) => value switch
        {
            float f => f,
            int i => i,
            double d => (float)d,
            long l => l,
            _ => 0f
        };

        private string FormatSettingValue(object value) => value switch
        {
            float f => f.ToString("F2"),
            double d => d.ToString("F2"),
            _ => value?.ToString() ?? ""
        };
    }
}