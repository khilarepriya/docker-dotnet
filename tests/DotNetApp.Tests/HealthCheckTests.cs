using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Net.Http.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit; // ✅ Don't forget this for `[Fact]` attribute

public class HealthCheckTests
{
    [Fact]
    public async Task DotNetAppImage_ShouldRespondOnHealthEndpoint()
    {
        var container = new ContainerBuilder()
            .WithImage("dotnetapp:latest")
            .WithPortBinding(6060, 6060)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r =>
                r.ForPort(6060).ForPath("/health")))
            .Build();

        await container.StartAsync();

        var http = new HttpClient();
        var result = await http.GetStringAsync("http://localhost:6060/health");

        Assert.Equal("Healthy", result); // Change "Healthy" based on your app response

        await container.StopAsync();
    }
}

