using System.Net.Http.Json;
using BlockchainLedger;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Xunit;

namespace BlockchainLedger.AutomationTests;

// Proves the Testcontainers harness itself works before any multi-node
// logic is layered on top: builds the real Dockerfile from Chunk A, runs
// one container from it, and checks the node inside responds like a real
// node should — including the exact deterministic genesis hash from Chunk 4.
public class SingleNodeContainerSmokeTests : IAsyncLifetime
{
    private const int NodePort = 8080;

    // Same value the manual Chunk A verification produced, and what every
    // node mines independently thanks to the fixed genesis inputs.
    private const string ExpectedGenesisHash = "000003e304f33300c3e7c15bea1fc4fa32bf0e2017f134a1ed803b2c276d2e2e";

    private IFutureDockerImage _image = null!;
    private IContainer _container = null!;
    private HttpClient _httpClient = null!;

    public async Task InitializeAsync()
    {
        _image = new ImageFromDockerfileBuilder()
            .WithName("blockchain-ledger-node:automation-tests")
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile("src/BlockchainLedger/Dockerfile")
            .WithDeleteIfExists(false)
            .Build();
        await _image.CreateAsync();

        _container = new ContainerBuilder(_image)
            .WithPortBinding(NodePort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPath("/chain").ForPort(NodePort)))
            .Build();
        await _container.StartAsync();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(NodePort)}"),
        };
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Container_ServesChainEndpoint_WithTheDeterministicGenesisBlock()
    {
        var response = await _httpClient.GetFromJsonAsync<ChainResponseDto>("/chain");

        Assert.NotNull(response);
        Assert.True(response!.Valid);
        Assert.Single(response.Chain);
        Assert.Equal(ExpectedGenesisHash, response.Chain[0].Hash);
    }

    [Fact]
    public async Task Container_CanMineANewBlock()
    {
        var mineResponse = await _httpClient.PostAsJsonAsync("/blocks/mine", new { data = "automation test transaction" });
        mineResponse.EnsureSuccessStatusCode();

        var chain = await _httpClient.GetFromJsonAsync<ChainResponseDto>("/chain");

        Assert.NotNull(chain);
        Assert.Equal(2, chain!.Length);
        Assert.Equal("automation test transaction", chain.Chain[1].Data);
    }
}
