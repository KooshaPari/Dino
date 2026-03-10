using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DINOForge.Runtime.UI
{
    /// <summary>
    /// IMGUI-based mod menu overlay. Toggle with F10.
    /// Attached to a DontDestroyOnLoad GameObject so it persists across scenes.
    /// Shows loaded packs, enable/disable toggles, version info, and a reload button.
    /// </summary>
    public class ModMenuOverlay : MonoBehaviour
    {
        private bool _visible;
        private Rect _windowRect = new Rect(20, 20, 500, 600);
        private Vector2 _packListScroll;
        private int _selectedPackIndex = -1;
        private string _statusMessage = "";
        private int _errorCount;

        private readonly List<PackDisplayInfo> _packs = new List<PackDisplayInfo>();

        /// <summary>Callback invoked when the user clicks the Reload Packs button.</summary>
        public Action? OnReloadRequested;

        /// <summary>Whether the overlay is currently visible.</summary>
        public bool IsVisible => _visible;

        /// <summary>The currently selected pack index, or -1 if none.</summary>
        public int SelectedPackIndex => _selectedPackIndex;

        /// <summary>
        /// Updates the list of packs displayed in the overlay.
        /// </summary>
        /// <param name="packs">Pack display info objects to show.</param>
        public void SetPacks(IEnumerable<PackDisplayInfo> packs)
        {
            _packs.Clear();
            _packs.AddRange(packs);
            _selectedPackIndex = _packs.Count > 0 ? 0 : -1;
        }

        /// <summary>
        /// Updates the status bar message.
        /// </summary>
        /// <param name="message">Status text to display.</param>
        /// <param name="errorCount">Number of errors to show in the status bar.</param>
        public void SetStatus(string message, int errorCount = 0)
        {
            _statusMessage = message;
            _errorCount = errorCount;
        }

        /// <summary>
        /// Toggles the overlay visibility.
        /// </summary>
        public void Toggle()
        {
            _visible = !_visible;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Toggle();
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;

            _windowRect = GUI.Window(
                9001,
                _windowRect,
                DrawWindow,
                "DINOForge Mod Menu");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Status bar
            DrawStatusBar();

            GUILayout.Space(8);

            // Main content: pack list + details side by side
            GUILayout.BeginHorizontal();

            // Pack list (left panel)
            DrawPackList();

            // Details panel (right panel)
            if (_selectedPackIndex >= 0 && _selectedPackIndex < _packs.Count)
            {
                DrawPackDetails(_packs[_selectedPackIndex]);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Reload button
            if (GUILayout.Button("Reload Packs", GUILayout.Height(30)))
            {
                OnReloadRequested?.Invoke();
            }

            GUILayout.EndVertical();

            // Make the window draggable
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 20));
        }

        private void DrawStatusBar()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label($"Packs: {_packs.Count}");
            GUILayout.FlexibleSpace();

            if (_errorCount > 0)
            {
                Color oldColor = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label($"Errors: {_errorCount}");
                GUI.color = oldColor;
            }
            else
            {
                GUILayout.Label("No errors");
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(_statusMessage);
            GUILayout.EndHorizontal();
        }

        private void DrawPackList()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(200));
            GUILayout.Label("Loaded Packs", GUI.skin.GetStyle("label"));

            _packListScroll = GUILayout.BeginScrollView(_packListScroll, GUILayout.Height(400));

            for (int i = 0; i < _packs.Count; i++)
            {
                PackDisplayInfo pack = _packs[i];

                GUILayout.BeginHorizontal();

                // Enable/disable toggle
                bool newEnabled = GUILayout.Toggle(pack.IsEnabled, "", GUILayout.Width(20));
                if (newEnabled != pack.IsEnabled)
                {
                    _packs[i] = pack.WithEnabled(newEnabled);
                }

                // Pack name (highlight selected)
                if (i == _selectedPackIndex)
                {
                    Color oldColor = GUI.color;
                    GUI.color = Color.cyan;
                    if (GUILayout.Button(pack.Name, GUI.skin.label))
                    {
                        _selectedPackIndex = i;
                    }
                    GUI.color = oldColor;
                }
                else
                {
                    if (GUILayout.Button(pack.Name, GUI.skin.label))
                    {
                        _selectedPackIndex = i;
                    }
                }

                GUILayout.Label($"v{pack.Version}", GUILayout.Width(50));

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawPackDetails(PackDisplayInfo pack)
        {
            GUILayout.BeginVertical("box", GUILayout.MinWidth(280));
            GUILayout.Label("Pack Details", GUI.skin.GetStyle("label"));

            GUILayout.Label($"ID: {pack.Id}");
            GUILayout.Label($"Name: {pack.Name}");
            GUILayout.Label($"Version: {pack.Version}");
            GUILayout.Label($"Author: {pack.Author}");
            GUILayout.Label($"Type: {pack.Type}");
            GUILayout.Label($"Load Order: {pack.LoadOrder}");

            if (!string.IsNullOrEmpty(pack.Description))
            {
                GUILayout.Space(4);
                GUILayout.Label($"Description: {pack.Description}");
            }

            if (pack.Dependencies.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("Dependencies:");
                foreach (string dep in pack.Dependencies)
                {
                    GUILayout.Label($"  - {dep}");
                }
            }

            if (pack.Conflicts.Count > 0)
            {
                GUILayout.Space(4);
                Color oldColor = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label("Conflicts:");
                foreach (string conflict in pack.Conflicts)
                {
                    GUILayout.Label($"  - {conflict}");
                }
                GUI.color = oldColor;
            }

            GUILayout.EndVertical();
        }
    }

    /// <summary>
    /// Immutable display data for a pack in the mod menu.
    /// </summary>
    public sealed class PackDisplayInfo
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public string Author { get; }
        public string Type { get; }
        public string? Description { get; }
        public int LoadOrder { get; }
        public bool IsEnabled { get; }
        public IReadOnlyList<string> Dependencies { get; }
        public IReadOnlyList<string> Conflicts { get; }

        public PackDisplayInfo(
            string id,
            string name,
            string version,
            string author,
            string type,
            string? description,
            int loadOrder,
            bool isEnabled,
            IReadOnlyList<string> dependencies,
            IReadOnlyList<string> conflicts)
        {
            Id = id;
            Name = name;
            Version = version;
            Author = author;
            Type = type;
            Description = description;
            LoadOrder = loadOrder;
            IsEnabled = isEnabled;
            Dependencies = dependencies;
            Conflicts = conflicts;
        }

        /// <summary>Returns a copy with the enabled state changed.</summary>
        public PackDisplayInfo WithEnabled(bool enabled)
            => new PackDisplayInfo(Id, Name, Version, Author, Type, Description, LoadOrder, enabled, Dependencies, Conflicts);
    }
}
