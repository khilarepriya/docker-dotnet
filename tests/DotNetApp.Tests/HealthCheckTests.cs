using System.Threading.Tasks;
using Xunit;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

public class HealthCheckTests
{
    [Fact]
    public async Task DotNetAppImage_ShouldRespondOnHealthEndpoint()
    {
        var imageName = Environment.GetEnvironmentVariable("TEST_IMAGE_NAME") ?? "dotnetapp:latest";

        var container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(imageName)
            .WithPortBinding(6060, 6060)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r
                .ForPort(6060)
                .ForPath("/health")))
            .Build();

        await container.StartAsync();
        Assert.True(container.State == TestcontainersStates.Running);
        await container.StopAsync();
    }
}

