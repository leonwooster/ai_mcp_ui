using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpUi.Web.Models
{
    public class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";
        [JsonPropertyName("id"), JsonConverter(typeof(FlexibleIdConverter))] public object Id { get; set; } = Guid.NewGuid().ToString("N");
        [JsonPropertyName("method")] public string Method { get; set; } = string.Empty;
        [JsonPropertyName("params")] public JsonElement? Params { get; set; }
    }

    public class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")] public string? JsonRpc { get; set; }
        [JsonPropertyName("id"), JsonConverter(typeof(FlexibleIdConverter))] public object? Id { get; set; }
        [JsonPropertyName("result")] public JsonElement? Result { get; set; }
        [JsonPropertyName("error")] public JsonRpcError? Error { get; set; }
    }

    public class JsonRpcError
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
        [JsonPropertyName("data")] public JsonElement? Data { get; set; }
    }
}
