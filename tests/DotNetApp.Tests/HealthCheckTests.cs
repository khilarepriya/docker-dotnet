using Xunit;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Net.Http;
using System.Threading.Tasks;

public class HealthCheckTests
{
    [Fact]
    public async Task DotNetAppImage_ShouldRespondOnHealthEndpoint()
    {
        var container = new ContainerBuilder()
            .WithImage("dotnetapp:latest") // ✅ Use your actual image
            .WithPortBinding(6060, 6060)   // ✅ Map host:container port
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r =>
                r.ForPort(6060).ForPath("/health"))) // ✅ Wait for /health success
            .Build();

        await container.StartAsync();

        var http = new HttpClient();
        var response = await http.GetStringAsync("http://localhost:6060/health");
        Assert.Equal("Healthy", response); // ✅ Match the actual health response content

        await container.StopAsync();
    }
}

