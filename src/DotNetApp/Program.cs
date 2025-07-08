var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6060); // App listens on port 6060
});

var app = builder.Build();

app.MapGet("/", () => "Pipeline is working successfully");
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

