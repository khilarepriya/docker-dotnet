using System;
using Xunit;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

public class HealthCheckTests
{
    [Fact]
    public async Task DotNetAppImage_ShouldRespondOnHealthEndpoint()
    {
        string imageName = Environment.GetEnvironmentVariable("TEST_IMAGE_NAME") ?? "dotnetapp:latest";

        var container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(imageName)
            .WithName("dotnetapp-test")
            .WithPortBinding(6060, 80)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();

        await container.StartAsync();

        var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:6060/health");
        response.EnsureSuccessStatusCode();

        await container.DisposeAsync();
    }
}

