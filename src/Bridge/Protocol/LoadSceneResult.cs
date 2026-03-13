#nullable enable
using Newtonsoft.Json;

namespace DINOForge.Bridge.Protocol
{
    /// <summary>
    /// Result of a scene load request.
    /// </summary>
    public sealed class LoadSceneResult
    {
        /// <summary>Whether the scene load was dispatched successfully.</summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>The scene name that was requested.</summary>
        [JsonProperty("scene")]
        public string Scene { get; set; } = "";
    }
}
