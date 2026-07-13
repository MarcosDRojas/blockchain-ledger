using Xunit;

namespace BlockchainLedger.Tests;

// These cover Node's local logic without real HTTP calls, by only exercising
// paths that don't need to reach a peer (an unregistered/empty peer set).
// The broadcast/consensus HTTP calls themselves are exercised manually via
// the two-node demo, not by automated tests.
public class NodeTests
{
    private const int TestDifficulty = 2;

    [Fact]
    public void RegisterPeer_AddsNormalizedUrlToPeers()
    {
        var node = new Node(new Blockchain(TestDifficulty), new HttpClient());

        node.RegisterPeer("http://localhost:5002/");

        Assert.Contains("http://localhost:5002", node.Peers);
    }

    [Fact]
    public async Task ReceiveBlockAsync_FastPath_AppendsABlockThatExtendsTheTip()
    {
        var node = new Node(new Blockchain(TestDifficulty), new HttpClient());
        Block currentTip = node.Blockchain.Chain[^1];
        var incoming = new Block(currentTip.Index + 1, "next transaction", currentTip.Hash);
        incoming.MineBlock(TestDifficulty);

        bool accepted = await node.ReceiveBlockAsync(incoming);

        Assert.True(accepted);
        Assert.Equal(2, node.Blockchain.Chain.Count);
        Assert.Equal(incoming.Hash, node.Blockchain.Chain[^1].Hash);
    }

    [Fact]
    public async Task ReceiveBlockAsync_FallsBackToConsensus_WhenBlockDoesNotExtendTip()
    {
        // No peers registered, so consensus has nothing to resolve against
        // and correctly reports "no change" rather than accepting the block.
        var node = new Node(new Blockchain(TestDifficulty), new HttpClient());
        var incoming = new Block(5, "orphan block", "some-hash-that-is-not-our-tip");
        incoming.MineBlock(TestDifficulty);

        bool accepted = await node.ReceiveBlockAsync(incoming);

        Assert.False(accepted);
        Assert.Single(node.Blockchain.Chain);
    }

    [Fact]
    public async Task ResolveConflictsAsync_ReturnsFalse_WhenThereAreNoPeers()
    {
        var node = new Node(new Blockchain(TestDifficulty), new HttpClient());

        bool replaced = await node.ResolveConflictsAsync();

        Assert.False(replaced);
    }
}
