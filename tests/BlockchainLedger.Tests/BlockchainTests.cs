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
}
