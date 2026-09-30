#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DINOForge.Bridge.Protocol
{
    /// <summary>
    /// Result of reloading content packs from disk.
    /// </summary>
    public sealed class ReloadResult
    {
        /// <summary>Whether the reload completed without fatal errors.</summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>List of pack IDs that were loaded.</summary>
        [JsonProperty("loadedPacks")]
        public List<string> LoadedPacks { get; set; } = new List<string>(); // public-mutable-ok: JSON deserializer requires mutable List

        /// <summary>Any errors encountered during the reload.</summary>
        [JsonProperty("errors")]
        public List<string> Errors { get; set; } = new List<string>(); // public-mutable-ok: JSON deserializer requires mutable List

        /// <summary>Requested pack root, when supplied by the caller.</summary>
        [JsonProperty("requestedPath")]
        public string? RequestedPath { get; set; }

        /// <summary>Actual pack root used by the runtime.</summary>
        [JsonProperty("resolvedPath")]
        public string? ResolvedPath { get; set; }

        /// <summary>Generation the caller requested/constructed.</summary>
        [JsonProperty("desiredGeneration")]
        public string? DesiredGeneration { get; set; }

        /// <summary>Generation accepted as active after this operation.</summary>
        [JsonProperty("activeGeneration")]
        public string? ActiveGeneration { get; set; }

        /// <summary>Whether all required runtime consumers applied the desired generation.</summary>
        [JsonProperty("fullyObserved")]
        public bool? FullyObserved { get; set; }

        /// <summary>Machine-readable activation disposition (legacy/committed/rejected/partial/restart-required).</summary>
        [JsonProperty("activationDisposition")]
        public string? ActivationDisposition { get; set; }
    }
}
