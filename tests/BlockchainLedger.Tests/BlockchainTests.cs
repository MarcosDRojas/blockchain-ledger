using Xunit;

namespace BlockchainLedger.Tests;

public class BlockchainTests
{
    [Fact]
    public void NewBlockchain_StartsWithGenesisBlock()
    {
        var blockchain = new Blockchain();

        Assert.Single(blockchain.Chain);
        Assert.Equal(0, blockchain.Chain[0].Index);
        Assert.Equal("0", blockchain.Chain[0].PreviousHash);
    }

    [Fact]
    public void AddBlock_LinksToPreviousBlocksHash()
    {
        var blockchain = new Blockchain();
        blockchain.AddBlock("first transaction");

        Assert.Equal(2, blockchain.Chain.Count);
        Assert.Equal(blockchain.Chain[0].Hash, blockchain.Chain[1].PreviousHash);
    }

    [Fact]
    public void IsChainValid_TrueForUntamperedChain()
    {
        var blockchain = new Blockchain();
        blockchain.AddBlock("first transaction");
        blockchain.AddBlock("second transaction");

        Assert.True(blockchain.IsChainValid());
    }

    [Fact]
    public void IsChainValid_FalseWhenAnEarlierBlockIsSwapped()
    {
        var blockchain = new Blockchain();
        blockchain.AddBlock("first transaction");
        blockchain.AddBlock("second transaction");

        blockchain.Chain[1] = new Block(1, "tampered data", blockchain.Chain[0].Hash);

        Assert.False(blockchain.IsChainValid());
    }
}
