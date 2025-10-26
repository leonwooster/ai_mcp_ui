using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpUi.Web.Models
{
    public class UIResource
    {
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }
        
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
        
        [JsonPropertyName("data")]
        public byte[]? Data { get; set; }
        
        [JsonPropertyName("components")]
        public RemoteDomComponent[]? Components { get; set; }
    }
    
    public class RemoteDomComponent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("props")]
        public Dictionary<string, object?>? Props { get; set; }
        
        [JsonPropertyName("children")]
        public RemoteDomComponent[]? Children { get; set; }
    }
}
