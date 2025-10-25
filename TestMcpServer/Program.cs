using System.Text.Json;

try
{
    // Main loop to handle requests
    string? line;
    while ((line = Console.ReadLine()) != null)
    {
        try
        {
            var request = JsonSerializer.Deserialize<JsonRpcRequest>(line);
            if (request == null) continue;
            
            object response;
            
            switch (request.method)
            {
                case "initialize":
                    response = new {
                        jsonrpc = "2.0",
                        id = request.id,
                        result = new {
                            protocolVersion = "2025-03-26",
                            capabilities = new {
                                tools = new {
                                    listChanged = true
                                }
                            },
                            serverInfo = new {
                                name = "Test MCP Server",
                                version = "1.0.0"
                            }
                        }
                    };
                    break;
                
                case "tools/list":
                    response = new {
                        jsonrpc = "2.0",
                        id = request.id,
                        result = new {
                            tools = new[] {
                                new { name = "test-tool", description = "A test tool" }
                            }
                        }
                    };
                    break;
                
                case "tools/call":
                    response = new {
                        jsonrpc = "2.0",
                        id = request.id,
                        result = new {
                            content = new[] {
                                new { type = "text", text = "Test tool called successfully" }
                            }
                        }
                    };
                    break;
                
                default:
                    response = new {
                        jsonrpc = "2.0",
                        id = request.id,
                        error = new {
                            code = -32601,
                            message = $"Method '{request.method}' not found"
                        }
                    };
                    break;
            }
            
            Console.WriteLine(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing request: {ex}");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Server error: {ex}");
}

public class JsonRpcRequest
{
    public string? jsonrpc { get; set; }
    public object? id { get; set; }  // Changed to object to handle both string and number IDs
    public string method { get; set; } = string.Empty;
    public object? @params { get; set; }
}
