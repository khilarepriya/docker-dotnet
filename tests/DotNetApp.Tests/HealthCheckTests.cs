using Xunit;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Net.Http;
using System.Threading.Tasks;

public class HealthCheckTests
{
    [Fact]
    public async Task AspNetBaseImage_ShouldStartSuccessfully()
    {
        var container = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/dotnet/aspnet:6.0") // ✅ Public official image
            .WithPortBinding(6060, 80)                        // ✅ Host:Container
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();

        await container.StartAsync();

        var http = new HttpClient();
        var response = await http.GetAsync("http://localhost:6060");
        Assert.True(response.IsSuccessStatusCode); // Just check if the container is running

        await container.StopAsync();
    }
}

