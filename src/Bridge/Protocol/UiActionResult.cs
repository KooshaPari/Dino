#nullable enable
using Newtonsoft.Json;

namespace DINOForge.Bridge.Protocol
{
    /// <summary>
    /// Result of a UI query or action request executed against the live Unity UI tree.
    /// </summary>
    public sealed class UiActionResult
    {
        /// <summary>Whether the request succeeded.</summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>Human-readable status message.</summary>
        [JsonProperty("message")]
        public string Message { get; set; } = "";

        /// <summary>Selector string used for the request.</summary>
        [JsonProperty("selector")]
        public string Selector { get; set; } = "";

        /// <summary>First matched node, when relevant.</summary>
        [JsonProperty("matchedNode", NullValueHandling = NullValueHandling.Ignore)]
        public UiNode? MatchedNode { get; set; }

        /// <summary>Total number of matching nodes.</summary>
        [JsonProperty("matchCount")]
        public int MatchCount { get; set; }
    }
}
