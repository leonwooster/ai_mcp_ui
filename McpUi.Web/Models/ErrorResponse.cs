using System.Text.Json.Serialization;

namespace McpUi.Web.Models
{
    public class ErrorResponse
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
        [JsonPropertyName("details")] public object? Details { get; set; }
    }
}
