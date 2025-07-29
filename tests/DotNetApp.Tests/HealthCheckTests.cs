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
    Assert.Equal("Healthy", result); // Adjust expected string to your app's response

    await container.StopAsync();
}

