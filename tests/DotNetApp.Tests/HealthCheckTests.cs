using System;
using System.Net.Http;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

public class HealthCheckTests
{
    [Fact]
    public async Task DotNetAppImage_ShouldRespondOnHealthEndpoint()
    {
        var imageName = Environment.GetEnvironmentVariable("TEST_IMAGE_NAME") ?? "dotnetapp:latest";

        var container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(imageName)
            .WithPortBinding(6060, 6060)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request =>
                request.ForPort(6060).ForPath("/health")))
            .Build();

        await container.StartAsync();

        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:6060/health");
        response.EnsureSuccessStatusCode();

        await container.StopAsync();
    }
}

