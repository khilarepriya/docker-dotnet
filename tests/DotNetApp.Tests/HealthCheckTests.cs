using Xunit;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Net.Http.Json;

public class HealthCheckTests
{
    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        var container = new ContainerBuilder()
            .WithImage("dotnetapp:latest")
            .WithPortBinding(6060, 6060) // Map host:container port 6060
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(6060).ForPath("/health")))
            .Build();

        await container.StartAsync();

        var http = new HttpClient();
        var result = await http.GetStringAsync("http://localhost:6060/");
        Assert.Equal("Pipeline is working successfully", result);

        await container.StopAsync();
    }
}

