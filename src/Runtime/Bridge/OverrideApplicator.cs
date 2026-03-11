#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using DINOForge.SDK.Models;
using DINOForge.SDK.Registry;

namespace DINOForge.Runtime.Bridge
{
    /// <summary>
    /// Translates loaded pack content (units, buildings, etc.) into
    /// <see cref="StatModification"/> objects and enqueues them into
    /// <see cref="StatModifierSystem"/> for runtime application.
    /// </summary>
    public static class OverrideApplicator
    {
        /// <summary>
        /// Default HP value that indicates "no override" (same as UnitStats default).
        /// </summary>
        private const float DefaultHp = 1f;

        /// <summary>
        /// Default speed value that indicates "no override".
        /// </summary>
        private const float DefaultSpeed = 0f;

        /// <summary>
        /// Default fire rate value that indicates "no override".
        /// </summary>
        private const float DefaultFireRate = 1.0f;

        /// <summary>
        /// Default damage value that indicates "no override".
        /// </summary>
        private const float DefaultDamage = 0f;

        /// <summary>
        /// Default armor value that indicates "no override".
        /// </summary>
        private const float DefaultArmor = 0f;

        /// <summary>
        /// Default range value that indicates "no override".
        /// </summary>
        private const float DefaultRange = 0f;

        /// <summary>
        /// Scans all registered units in the registry and creates <see cref="StatModification"/>
        /// objects for any non-default stat values. Enqueues them into
        /// <see cref="StatModifierSystem"/> for application.
        /// </summary>
        /// <param name="registryManager">The registry manager containing loaded content.</param>
        /// <param name="log">Logging callback for diagnostics.</param>
        /// <returns>The number of stat modifications enqueued.</returns>
        public static int ApplyUnitOverrides(RegistryManager registryManager, Action<string> log)
        {
            if (registryManager == null) throw new ArgumentNullException(nameof(registryManager));
            if (log == null) throw new ArgumentNullException(nameof(log));

            List<StatModification> modifications = new List<StatModification>();

            foreach (KeyValuePair<string, RegistryEntry<UnitDefinition>> kvp in registryManager.Units.All)
            {
                RegistryEntry<UnitDefinition> entry = kvp.Value;
                UnitDefinition unit = entry.Data;

                if (unit.Stats == null) continue;

                try
                {
                    // HP override: only if non-default
                    if (Math.Abs(unit.Stats.Hp - DefaultHp) > 0.001f)
                    {
                        modifications.Add(new StatModification(
                            "unit.stats.hp",
                            unit.Stats.Hp,
                            ModifierMode.Override));
                        log($"[OverrideApplicator] Unit '{unit.Id}': HP = {unit.Stats.Hp}");
                    }

                    // Speed override: only if non-zero
                    if (Math.Abs(unit.Stats.Speed - DefaultSpeed) > 0.001f)
                    {
                        modifications.Add(new StatModification(
                            "unit.stats.speed",
                            unit.Stats.Speed,
                            ModifierMode.Override));
                        log($"[OverrideApplicator] Unit '{unit.Id}': Speed = {unit.Stats.Speed}");
                    }

                    // Attack cooldown (derived from fire rate): only if non-default
                    // Fire rate is attacks per second; attack cooldown is seconds per attack
                    if (Math.Abs(unit.Stats.FireRate - DefaultFireRate) > 0.001f && unit.Stats.FireRate > 0.001f)
                    {
                        float attackCooldown = 1.0f / unit.Stats.FireRate;
                        modifications.Add(new StatModification(
                            "unit.stats.attack_cooldown",
                            attackCooldown,
                            ModifierMode.Override));
                        log($"[OverrideApplicator] Unit '{unit.Id}': AttackCooldown = {attackCooldown:F3} (from FireRate={unit.Stats.FireRate})");
                    }

                    // Damage override
                    if (Math.Abs(unit.Stats.Damage - DefaultDamage) > 0.001f)
                    {
                        modifications.Add(new StatModification(
                            "unit.stats.damage",
                            unit.Stats.Damage,
                            ModifierMode.Override));
                        log($"[OverrideApplicator] Unit '{unit.Id}': Damage = {unit.Stats.Damage}");
                    }

                    // Armor override
                    if (Math.Abs(unit.Stats.Armor - DefaultArmor) > 0.001f)
                    {
                        modifications.Add(new StatModification(
                            "unit.stats.armor",
                            unit.Stats.Armor,
                            ModifierMode.Override));
                        log($"[OverrideApplicator] Unit '{unit.Id}': Armor = {unit.Stats.Armor}");
                    }

                    // Range override
                    if (Math.Abs(unit.Stats.Range - DefaultRange) > 0.001f)
                    {
                        modifications.Add(new StatModification(
                            "unit.stats.range",
                            unit.Stats.Range,
                            ModifierMode.Override));
                        log($"[OverrideApplicator] Unit '{unit.Id}': Range = {unit.Stats.Range}");
                    }
                }
                catch (Exception ex)
                {
                    log($"[OverrideApplicator] Error processing unit '{unit.Id}': {ex.Message}");
                }
            }

            if (modifications.Count > 0)
            {
                StatModifierSystem.EnqueueRange(modifications);
                log($"[OverrideApplicator] Enqueued {modifications.Count} stat modification(s).");
            }
            else
            {
                log("[OverrideApplicator] No unit stat overrides to apply.");
            }

            return modifications.Count;
        }

        /// <summary>
        /// Applies YAML-loaded stat overrides to entities via StatModifierSystem.
        /// Converts each override entry to a StatModification and enqueues for application.
        /// </summary>
        /// <param name="overrides">The stat override definitions loaded from YAML.</param>
        /// <param name="log">Logging callback for diagnostics.</param>
        /// <returns>The number of stat modifications enqueued.</returns>
        public static int ApplyStatOverrides(IReadOnlyList<SDK.Models.StatOverrideDefinition> overrides, Action<string> log)
        {
            if (overrides == null) throw new ArgumentNullException(nameof(overrides));
            if (log == null) throw new ArgumentNullException(nameof(log));

            List<StatModification> modifications = new List<StatModification>();

            foreach (SDK.Models.StatOverrideDefinition definition in overrides)
            {
                if (definition.Overrides == null || definition.Overrides.Count == 0)
                    continue;

                try
                {
                    foreach (SDK.Models.StatOverrideEntry entry in definition.Overrides)
                    {
                        // Parse the mode string to ModifierMode enum
                        ModifierMode mode = ModifierMode.Override;
                        if (!string.IsNullOrEmpty(entry.Mode))
                        {
                            if (string.Equals(entry.Mode, "override", StringComparison.OrdinalIgnoreCase))
                                mode = ModifierMode.Override;
                            else if (string.Equals(entry.Mode, "add", StringComparison.OrdinalIgnoreCase))
                                mode = ModifierMode.Add;
                            else if (string.Equals(entry.Mode, "multiply", StringComparison.OrdinalIgnoreCase))
                                mode = ModifierMode.Multiply;
                        }

                        // Create stat modification
                        StatModification mod = new StatModification(
                            entry.Target,
                            entry.Value,
                            mode,
                            entry.Filter);

                        modifications.Add(mod);
                        log($"[OverrideApplicator] YAML override: {entry.Target} = {entry.Value} ({mode})");
                    }
                }
                catch (Exception ex)
                {
                    log($"[OverrideApplicator] Error processing stat override definition: {ex.Message}");
                }
            }

            if (modifications.Count > 0)
            {
                StatModifierSystem.EnqueueRange(modifications);
                log($"[OverrideApplicator] Enqueued {modifications.Count} YAML stat override(s).");
            }

            return modifications.Count;
        }

        /// <summary>
        /// Writes a debug message to the DINOForge debug log file.
        /// </summary>
        private static void WriteDebug(string msg)
        {
            try
            {
                string debugLog = Path.Combine(
                    BepInEx.Paths.BepInExRootPath, "dinoforge_debug.log");
                File.AppendAllText(debugLog, $"[{DateTime.Now}] {msg}\n");
            }
            catch { }
        }
    }
}
