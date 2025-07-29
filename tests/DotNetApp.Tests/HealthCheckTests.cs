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
        string imageName = Environment.GetEnvironmentVariable("TEST_IMAGE_NAME") ?? "priyanka015/dotnet:latest";

        var container = new ContainerBuilder()
            .WithImage(imageName)
            .WithPortBinding(6060, 6060)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r =>
                r.ForPort(6060).ForPath("/health")))
            .Build();

        await container.StartAsync();

        var client = new HttpClient();
        var response = await client.GetStringAsync("http://localhost:6060/health");

        Assert.Equal("Healthy", response); // Change based on your app output

        await container.StopAsync();
    }
}

