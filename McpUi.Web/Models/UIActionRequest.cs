using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpUi.Web.Models
{
    public class UIActionRequest
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [JsonPropertyName("payload")]
        public JsonElement? Payload { get; set; }
        
        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }
    }
}
