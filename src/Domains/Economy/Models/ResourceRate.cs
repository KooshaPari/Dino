namespace DINOForge.Domains.Economy.Models
{
    /// <summary>
    /// Defines a resource production or consumption rate for an economy entity.
    /// Each rate represents a single resource type with a base rate and multiplier
    /// that together determine the effective output per tick.
    /// </summary>
    public class ResourceRate
    {
        /// <summary>
        /// The type of resource this rate applies to.
        /// Valid values: food, wood, stone, iron, gold.
        /// </summary>
        public string ResourceType { get; set; } = "";

        /// <summary>
        /// Base production or consumption rate per tick before multipliers.
        /// Positive values indicate production; negative values indicate consumption.
        /// </summary>
        public float BaseRate { get; set; }

        /// <summary>
        /// Multiplier applied to the base rate. Defaults to 1.0 (no modification).
        /// Values greater than 1.0 increase output; values between 0 and 1.0 reduce it.
        /// </summary>
        public float Multiplier { get; set; } = 1.0f;

        /// <summary>
        /// The final computed rate after applying the multiplier to the base rate.
        /// </summary>
        public float EffectiveRate => BaseRate * Multiplier;

        /// <summary>
        /// All valid resource type identifiers in DINO's economy.
        /// </summary>
        public static readonly string[] ValidResourceTypes = { "food", "wood", "stone", "iron", "gold" };
    }
}
