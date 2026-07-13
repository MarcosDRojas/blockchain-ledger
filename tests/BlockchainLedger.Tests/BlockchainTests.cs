using Xunit;

namespace BlockchainLedger.Tests;

public class BlockchainTests
{
    // Low difficulty keeps these tests fast — the mining algorithm itself
    // is exercised directly by the MineBlock tests in BlockTests.
    private const int TestDifficulty = 2;

    [Fact]
    public void NewBlockchain_StartsWithMinedGenesisBlock()
    {
        var blockchain = new Blockchain(TestDifficulty);

        Assert.Single(blockchain.Chain);
        Assert.Equal(0, blockchain.Chain[0].Index);
        Assert.Equal("0", blockchain.Chain[0].PreviousHash);
        Assert.StartsWith("00", blockchain.Chain[0].Hash);
    }

    [Fact]
    public void AddBlock_LinksToPreviousBlocksHash()
    {
        var blockchain = new Blockchain(TestDifficulty);
        blockchain.AddBlock("first transaction");

        Assert.Equal(2, blockchain.Chain.Count);
        Assert.Equal(blockchain.Chain[0].Hash, blockchain.Chain[1].PreviousHash);
    }

    [Fact]
    public void AddBlock_ProducesAMinedBlock()
    {
        var blockchain = new Blockchain(TestDifficulty);
        blockchain.AddBlock("first transaction");

        Assert.StartsWith("00", blockchain.Chain[1].Hash);
    }

    [Fact]
    public void IsChainValid_TrueForUntamperedChain()
    {
        var blockchain = new Blockchain(TestDifficulty);
        blockchain.AddBlock("first transaction");
        blockchain.AddBlock("second transaction");

        Assert.True(blockchain.IsChainValid());
    }

    [Fact]
    public void IsChainValid_FalseWhenAnEarlierBlockIsSwappedForAMinedOne()
    {
        var blockchain = new Blockchain(TestDifficulty);
        blockchain.AddBlock("first transaction");
        blockchain.AddBlock("second transaction");

        var tamperedBlock = new Block(1, "tampered data", blockchain.Chain[0].Hash);
        tamperedBlock.MineBlock(TestDifficulty);
        blockchain.Chain[1] = tamperedBlock;

        Assert.False(blockchain.IsChainValid());
    }

    [Fact]
    public void IsChainValid_FalseWhenABlockIsNotMined()
    {
        var blockchain = new Blockchain(TestDifficulty);
        blockchain.AddBlock("first transaction");

        // Swap in a block that links correctly but was never mined, so its
        // hash almost certainly doesn't meet the difficulty target.
        blockchain.Chain[1] = new Block(1, "unmined block", blockchain.Chain[0].Hash);

        Assert.False(blockchain.IsChainValid());
    }

    [Fact]
    public void ReplaceChainIfLonger_AdoptsALongerValidChain()
    {
        var shortChain = new Blockchain(TestDifficulty);

        var longerChain = new Blockchain(TestDifficulty);
        longerChain.AddBlock("first transaction");
        longerChain.AddBlock("second transaction");

        bool replaced = shortChain.ReplaceChainIfLonger(longerChain.Chain);

        Assert.True(replaced);
        Assert.Equal(longerChain.Chain.Count, shortChain.Chain.Count);
        Assert.Equal(longerChain.Chain[^1].Hash, shortChain.Chain[^1].Hash);
    }

    [Fact]
    public void ReplaceChainIfLonger_RejectsAChainThatIsNotLonger()
    {
        var node = new Blockchain(TestDifficulty);
        node.AddBlock("first transaction");
        var sameLengthChain = new List<Block> { node.Chain[0], node.Chain[1] };

        bool replaced = node.ReplaceChainIfLonger(sameLengthChain);

        Assert.False(replaced);
        Assert.Equal(2, node.Chain.Count);
    }

    [Fact]
    public void ReplaceChainIfLonger_RejectsALongerButInvalidChain()
    {
        var node = new Blockchain(TestDifficulty);

        var invalidLongerChain = new List<Block>
        {
            node.Chain[0],
            new Block(1, "unmined block", node.Chain[0].Hash),
            new Block(2, "another unmined block", "wrong-previous-hash"),
        };

        bool replaced = node.ReplaceChainIfLonger(invalidLongerChain);

        Assert.False(replaced);
        Assert.Single(node.Chain);
    }

    [Fact]
    public void TwoBlockchains_ShareTheSameDeterministicGenesisBlock()
    {
        var nodeA = new Blockchain(TestDifficulty);
        var nodeB = new Blockchain(TestDifficulty);

        Assert.Equal(nodeA.GenesisHash, nodeB.GenesisHash);
        Assert.Equal(nodeA.Chain[0].Hash, nodeB.Chain[0].Hash);
    }

    [Fact]
    public void ReplaceChainIfLonger_RejectsALongerChainWithADifferentGenesisBlock()
    {
        var node = new Blockchain(TestDifficulty);

        // A fully self-consistent chain (honest hashes, proper mining,
        // correct linking) that's longer than ours, but rooted in a
        // completely different, fabricated genesis block.
        var foreignGenesis = new Block(0, "A fabricated chain", "0");
        foreignGenesis.MineBlock(TestDifficulty);
        var foreignBlock1 = new Block(1, "fabricated transaction", foreignGenesis.Hash);
        foreignBlock1.MineBlock(TestDifficulty);
        var foreignChain = new List<Block> { foreignGenesis, foreignBlock1 };

        bool replaced = node.ReplaceChainIfLonger(foreignChain);

        Assert.False(replaced);
        Assert.Single(node.Chain);
    }
}
