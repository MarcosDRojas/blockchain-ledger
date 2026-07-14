using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using Xunit;

namespace BlockchainLedger.AutomationTests;

// One running node container, plus the two URLs that matter for it:
// - HttpClient's BaseAddress: how the test driver (on the host) reaches it.
// - InternalUrl: how *other containers on the same Docker network* reach
//   it. Peer registration must use InternalUrl, never the host-mapped
//   address — containers can't see each other's host port mappings, only
//   each other's network aliases.
public sealed record NodeHandle(string Alias, IContainer Container, HttpClient HttpClient, string InternalUrl);

// Spins up several node containers on a shared, isolated Docker network so
// tests can exercise real broadcast/consensus between genuinely separate
// processes — the automated equivalent of the two-terminal demo in the README.
public class MultiNodeFixture : IAsyncLifetime
{
    private const int NodePort = 8080;
    private const int NodeCount = 3;

    private IFutureDockerImage _image = null!;
    private INetwork _network = null!;
    private readonly List<NodeHandle> _nodes = new();

    // node[0]/node[1]/node[2] — none are peered with each other by default;
    // each test decides which nodes to connect and how.
    public IReadOnlyList<NodeHandle> Nodes => _nodes;

    public async Task InitializeAsync()
    {
        _image = new ImageFromDockerfileBuilder()
            .WithName("blockchain-ledger-node:automation-tests")
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile("src/BlockchainLedger/Dockerfile")
            .WithDeleteIfExists(false)
            .Build();
        await _image.CreateAsync();

        _network = new NetworkBuilder()
            .WithName($"blockchain-ledger-test-{Guid.NewGuid():N}")
            .Build();
        await _network.CreateAsync();

        for (int i = 1; i <= NodeCount; i++)
        {
            _nodes.Add(await StartNodeAsync($"node{i}"));
        }
    }

    private async Task<NodeHandle> StartNodeAsync(string alias)
    {
        var container = new ContainerBuilder(_image)
            .WithNetwork(_network)
            .WithNetworkAliases(alias)
            .WithPortBinding(NodePort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPath("/chain").ForPort(NodePort)))
            .Build();
        await container.StartAsync();

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{container.Hostname}:{container.GetMappedPublicPort(NodePort)}"),
        };

        return new NodeHandle(alias, container, httpClient, $"http://{alias}:{NodePort}");
    }

    public async Task DisposeAsync()
    {
        foreach (NodeHandle node in _nodes)
        {
            node.HttpClient.Dispose();
            await node.Container.DisposeAsync();
        }

        await _network.DeleteAsync();
    }
}
