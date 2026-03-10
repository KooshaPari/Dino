using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime
{
    /// <summary>
    /// Simple IMGUI debug overlay showing ECS world state.
    /// Toggled via hotkey (default: F9).
    /// </summary>
    public class DebugOverlay
    {
        private Vector2 _scrollPosition;
        private Rect _windowRect = new Rect(10, 10, 420, 500);
        private bool _showSystems;
        private bool _showArchetypes;

        public void Draw()
        {
            _windowRect = GUI.Window(
                9999,
                _windowRect,
                DrawWindow,
                $"DINOForge v{PluginInfo.VERSION}");
        }

        private void DrawWindow(int windowId)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

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

                        int systemCount = world.Systems.Count();
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

            GUILayout.Space(10);
            GUILayout.Label("F8 = Dump to disk | F9 = Toggle overlay");

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
                    foreach (ComponentSystemBase system in world.Systems.Take(30))
                    {
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
    }
}
