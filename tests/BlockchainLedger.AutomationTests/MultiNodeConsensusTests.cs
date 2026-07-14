using System.Net.Http.Json;
using BlockchainLedger;
using Xunit;

namespace BlockchainLedger.AutomationTests;

// Exercises real broadcast and longest-valid-chain consensus across
// genuinely separate node processes (containers on an isolated Docker
// network) — the same scenario as the README's manual two-terminal demo,
// but automated. Each test gets its own fresh set of containers.
public class MultiNodeConsensusTests : IAsyncLifetime
{
    private readonly MultiNodeFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task AllNodes_StartWithTheSameDeterministicGenesisBlock()
    {
        var genesisHashes = new List<string>();
        foreach (NodeHandle node in _fixture.Nodes)
        {
            var chain = await node.HttpClient.GetFromJsonAsync<ChainResponseDto>("/chain");
            genesisHashes.Add(chain!.Chain[0].Hash);
        }

        Assert.All(genesisHashes, hash => Assert.Equal(genesisHashes[0], hash));
    }

    [Fact]
    public async Task MinedBlock_PropagatesToPeersViaBroadcast()
    {
        NodeHandle node1 = _fixture.Nodes[0];
        NodeHandle node2 = _fixture.Nodes[1];
        await RegisterPeerAsync(from: node1, to: node2);
        await RegisterPeerAsync(from: node2, to: node1);

        var mineResponse = await node1.HttpClient.PostAsJsonAsync("/blocks/mine", new { data = "Alice pays Bob 5 coins" });
        var minedBlock = await mineResponse.Content.ReadFromJsonAsync<Block>();

        ChainResponseDto node2Chain = await PollUntilChainLengthAsync(node2, expectedLength: 2);

        Assert.Equal("Alice pays Bob 5 coins", node2Chain.Chain[1].Data);
        Assert.Equal(minedBlock!.Hash, node2Chain.Chain[1].Hash);
    }

    [Fact]
    public async Task LateJoiningPeer_CatchesUpViaConsensusResolve()
    {
        NodeHandle node1 = _fixture.Nodes[0];
        NodeHandle node3 = _fixture.Nodes[2];

        // node3 isn't peered with anyone yet, so it can't receive the broadcast.
        await node1.HttpClient.PostAsJsonAsync("/blocks/mine", new { data = "Bob pays Carol 2 coins" });

        // It joins the network after the block was already mined...
        await RegisterPeerAsync(from: node3, to: node1);

        // ...so it has to actively ask around to catch up, rather than
        // having caught the broadcast.
        HttpResponseMessage resolveResponse = await node3.HttpClient.PostAsync("/consensus/resolve", content: null);
        resolveResponse.EnsureSuccessStatusCode();

        var node3Chain = await node3.HttpClient.GetFromJsonAsync<ChainResponseDto>("/chain");

        Assert.Equal(2, node3Chain!.Length);
        Assert.Equal("Bob pays Carol 2 coins", node3Chain.Chain[1].Data);
    }

    [Fact]
    public async Task ThreeNodes_AllConvergeOnTheSameChainAfterMiningOnDifferentNodes()
    {
        NodeHandle node1 = _fixture.Nodes[0];
        NodeHandle node2 = _fixture.Nodes[1];
        NodeHandle node3 = _fixture.Nodes[2];

        // Fully mesh all three nodes together.
        foreach ((NodeHandle from, NodeHandle to) in new[] { (node1, node2), (node2, node1), (node2, node3), (node3, node2), (node1, node3), (node3, node1) })
        {
            await RegisterPeerAsync(from, to);
        }

        await node1.HttpClient.PostAsJsonAsync("/blocks/mine", new { data = "Alice pays Bob 5 coins" });
        await PollUntilChainLengthAsync(node2, expectedLength: 2);
        await PollUntilChainLengthAsync(node3, expectedLength: 2);

        await node2.HttpClient.PostAsJsonAsync("/blocks/mine", new { data = "Bob pays Carol 2 coins" });
        ChainResponseDto finalChain1 = await PollUntilChainLengthAsync(node1, expectedLength: 3);
        ChainResponseDto finalChain3 = await PollUntilChainLengthAsync(node3, expectedLength: 3);

        Assert.Equal(finalChain1.Chain[^1].Hash, finalChain3.Chain[^1].Hash);
        Assert.True(finalChain1.Valid);
        Assert.True(finalChain3.Valid);
    }

    private static async Task RegisterPeerAsync(NodeHandle from, NodeHandle to)
    {
        HttpResponseMessage response = await from.HttpClient.PostAsJsonAsync("/peers/register", new { peerUrl = to.InternalUrl });
        response.EnsureSuccessStatusCode();
    }

    // Broadcast/consensus happen over HTTP between containers, asynchronously
    // from the test's point of view, so assertions poll briefly instead of
    // assuming the chain has already updated the instant the mine call returns.
    private static async Task<ChainResponseDto> PollUntilChainLengthAsync(NodeHandle node, int expectedLength, int timeoutSeconds = 15)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        ChainResponseDto? last = null;

        while (DateTime.UtcNow < deadline)
        {
            last = await node.HttpClient.GetFromJsonAsync<ChainResponseDto>("/chain");
            if (last is not null && last.Length >= expectedLength)
            {
                return last;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException(
            $"Node {node.Alias} never reached chain length {expectedLength}; last seen length was {last?.Length}.");
    }
}
