#nullable enable
using Newtonsoft.Json;

namespace DINOForge.Bridge.Protocol
{
    /// <summary>Result of a start-game (BeginGameWorldLoadingSingleton) request.</summary>
    public sealed class StartGameResult
    {
        [JsonProperty("success")] public bool Success { get; set; }
        [JsonProperty("message")] public string Message { get; set; } = "";
    }
}
