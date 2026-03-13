namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Aerial flight parameters for a unit definition.
    /// Deserialized from the <c>aerial:</c> block in unit YAML files.
    ///
    /// These values are mapped to <see cref="DINOForge.Runtime.Aviation.AerialUnitComponent"/>
    /// when a unit with the <c>Aerial</c> behavior_tag is spawned.
    /// </summary>
    public class AerialProperties
    {
        /// <summary>
        /// Target altitude in world units above ground (Y axis).
        /// Example: 15.0f = low flight (balloon), 25.0f = high flight (bomber).
        /// </summary>
        public float CruiseAltitude { get; set; } = 15f;

        /// <summary>
        /// Speed at which the unit climbs toward its cruise altitude (world units/second).
        /// </summary>
        public float AscendSpeed { get; set; } = 5f;

        /// <summary>
        /// Speed at which the unit descends when attacking or landing (world units/second).
        /// </summary>
        public float DescendSpeed { get; set; } = 3f;

        /// <summary>
        /// Whether this aerial unit can engage other aerial targets (e.g. interceptor role).
        /// </summary>
        public bool AntiAir { get; set; } = false;
    }
}
