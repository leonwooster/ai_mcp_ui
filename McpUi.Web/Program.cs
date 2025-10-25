using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/mcp-ui-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Bind MCP options from configuration
builder.Services.Configure<McpUi.Web.Services.McpOptions>(builder.Configuration.GetSection("Mcp"));

// Register HTTP client for MCP endpoints
builder.Services.AddHttpClient("Mcp", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    // The remote HTTP Streaming endpoint requires the client to accept both
    // application/json and text/event-stream.
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/event-stream");
});

// Register MCP client implementation as scoped so each request can manage its own session
builder.Services.AddScoped<McpUi.Web.Services.IMcpClient, McpUi.Web.Services.McpJsonRpcHttpClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
