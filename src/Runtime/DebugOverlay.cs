using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime
{
    /// <summary>
    /// IMGUI debug overlay MonoBehaviour showing ECS world state,
    /// loaded packs, and system status.
    /// Toggled via F9. Lives on the persistent DINOForge_Root GameObject.
    /// </summary>
    public class DebugOverlayBehaviour : MonoBehaviour
    {
        private bool _visible;
        private Vector2 _scrollPosition;
        private Rect _windowRect = new Rect(10, 10, 420, 600);
        private bool _showSystems;
        private bool _showArchetypes;
        private bool _showErrors;
        private ModPlatform? _modPlatform;

        /// <summary>Whether the debug overlay is currently visible.</summary>
        public bool IsVisible => _visible;

        /// <summary>
        /// Provides a reference to the ModPlatform for status display.
        /// </summary>
        /// <param name="modPlatform">The mod platform orchestrator.</param>
        public void SetModPlatform(ModPlatform? modPlatform)
        {
            _modPlatform = modPlatform;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;

            _windowRect = GUI.Window(
                9999,
                _windowRect,
                DrawWindow,
                $"DINOForge Debug v{PluginInfo.VERSION}");
        }

        private void DrawWindow(int windowId)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            // Platform status
            GUILayout.Label("=== Platform Status ===");
            if (_modPlatform != null)
            {
                GUILayout.Label($"  Initialized: {_modPlatform.IsInitialized}");
                GUILayout.Label($"  World Ready: {_modPlatform.IsWorldReady}");
                GUILayout.Label($"  Packs Dir: {_modPlatform.PacksDirectory}");

                if (_modPlatform.ContentLoader != null)
                {
                    int errorCount = _modPlatform.ContentLoader.LastLoadErrorCount;
                    if (errorCount > 0)
                    {
                        Color oldColor = GUI.color;
                        GUI.color = Color.red;
                        GUILayout.Label($"  Load Errors: {errorCount}");
                        GUI.color = oldColor;
                    }
                }
            }
            else
            {
                GUILayout.Label("  ModPlatform: not available");
            }

            GUILayout.Space(10);

            // World info
            GUILayout.Label("=== ECS Worlds ===");

            if (World.All.Count > 0)
            {
                foreach (World world in World.All)
                {
                    if (!world.IsCreated) continue;

                    GUILayout.Label($"World: {world.Name}");

                    try
                    {
                        EntityManager em = world.EntityManager;
                        NativeArray<Entity> entities = em.GetAllEntities(Allocator.Temp);
                        GUILayout.Label($"  Entities: {entities.Length}");
                        entities.Dispose();

                        int systemCount = world.Systems.Count;
                        GUILayout.Label($"  Systems: {systemCount}");
                    }
                    catch (Exception ex)
                    {
                        GUILayout.Label($"  Error: {ex.Message}");
                    }

                    GUILayout.Space(5);
                }
            }
            else
            {
                GUILayout.Label("No worlds found.");
            }

            GUILayout.Space(10);

            // Toggle sections
            _showSystems = GUILayout.Toggle(_showSystems, "Show Systems");
            if (_showSystems)
            {
                DrawSystems();
            }

            _showArchetypes = GUILayout.Toggle(_showArchetypes, "Show Archetypes (top 20)");
            if (_showArchetypes)
            {
                DrawArchetypes();
            }

            // Errors section
            if (_modPlatform?.ContentLoader != null && _modPlatform.ContentLoader.LastLoadErrorCount > 0)
            {
                _showErrors = GUILayout.Toggle(_showErrors, $"Show Errors ({_modPlatform.ContentLoader.LastLoadErrorCount})");
                if (_showErrors)
                {
                    DrawErrors();
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("F8 = Dump to disk | F9 = Toggle debug | F10 = Mod menu");

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawSystems()
        {
            if (World.All.Count == 0) return;

            foreach (World world in World.All)
            {
                if (!world.IsCreated) continue;

                try
                {
                    var systems = world.Systems;
                    int limit = Math.Min(systems.Count, 30);
                    for (int i = 0; i < limit; i++)
                    {
                        ComponentSystemBase system = systems[i];
                        string status = system.Enabled ? "ON" : "OFF";
                        GUILayout.Label($"  [{status}] {system.GetType().Name}");
                    }
                }
                catch { }
            }
        }

        private void DrawArchetypes()
        {
            if (World.All.Count == 0) return;

            foreach (World world in World.All)
            {
                if (!world.IsCreated) continue;

                try
                {
                    EntityManager em = world.EntityManager;
                    NativeArray<Entity> entities = em.GetAllEntities(Allocator.Temp);

                    // Simple archetype counting
                    var archetypeCounts = new System.Collections.Generic.Dictionary<string, int>();

                    foreach (Entity entity in entities)
                    {
                        try
                        {
                            NativeArray<ComponentType> types = em.GetComponentTypes(entity, Allocator.Temp);
                            string key = string.Join(", ", types
                                .Select(t => t.GetManagedType()?.Name ?? "?")
                                .OrderBy(n => n)
                                .Take(5));
                            if (types.Length > 5) key += $" (+{types.Length - 5} more)";

                            if (!archetypeCounts.ContainsKey(key))
                                archetypeCounts[key] = 0;
                            archetypeCounts[key]++;

                            types.Dispose();
                        }
                        catch { }
                    }

                    foreach (var kvp in archetypeCounts.OrderByDescending(k => k.Value).Take(20))
                    {
                        GUILayout.Label($"  [{kvp.Value}x] {kvp.Key}");
                    }

                    entities.Dispose();
                }
                catch { }
            }
        }

        private void DrawErrors()
        {
            if (_modPlatform?.ContentLoader == null) return;

            var errors = _modPlatform.ContentLoader.LastLoadErrors;
            if (errors == null || errors.Count == 0)
            {
                GUILayout.Label("  (No errors available)");
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = Color.red;

            int maxShow = Math.Min(10, errors.Count);
            for (int i = 0; i < maxShow; i++)
            {
                string error = errors[i];
                string display = error.Length > 100 ? error.Substring(0, 97) + "..." : error;
                GUILayout.Label($"  • {display}");
            }

            if (errors.Count > 10)
            {
                GUILayout.Label($"  ... and {errors.Count - 10} more errors (see F10 mod menu)");
            }

            GUI.color = oldColor;
        }
    }
}
