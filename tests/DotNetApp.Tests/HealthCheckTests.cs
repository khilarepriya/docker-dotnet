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

