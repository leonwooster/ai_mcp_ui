namespace McpUi.Web.Models
{
    public class PayloadValidationOptions
    {
        public const string SectionName = "PayloadValidation";
        
        /// <summary>
        /// Maximum allowed payload size in bytes (default: 10MB)
        /// </summary>
        public long MaxPayloadSize { get; set; } = 10 * 1024 * 1024; // 10MB
        
        /// <summary>
        /// Allowed MIME types for UI resources
        /// </summary>
        public string[] AllowedMimeTypes { get; set; } = new[]
        {
            "text/html",
            "text/plain",
            "text/uri-list",
            "application/json",
            "image/png",
            "image/jpeg",
            "image/gif",
            "application/vnd.mcp.remotedom"
        };
        
        /// <summary>
        /// Whether to reject payloads that exceed the maximum size
        /// </summary>
        public bool RejectOversizedPayloads { get; set; } = true;
        
        /// <summary>
        /// Whether to reject unsupported MIME types
        /// </summary>
        public bool RejectUnsupportedMimeTypes { get; set; } = true;
    }
}
