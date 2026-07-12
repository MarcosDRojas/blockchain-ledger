using BlockchainLedger;
using Xunit;

namespace BlockchainLedger.Tests;

public class BlockTests
{
    [Fact]
    public void Constructor_SetsIndexAndData()
    {
        var block = new Block(1, "hello");

        Assert.Equal(1, block.Index);
        Assert.Equal("hello", block.Data);
    }

    [Fact]
    public void Hash_IsA64CharacterHexString()
    {
        var block = new Block(0, "Genesis Block");

        Assert.Equal(64, block.Hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", block.Hash);
    }

    [Fact]
    public void Hash_ChangesWhenDataChanges()
    {
        var block1 = new Block(0, "a");
        var block2 = new Block(0, "b");

        Assert.NotEqual(block1.Hash, block2.Hash);
    }

    [Fact]
    public void Hash_ChangesWhenIndexChanges()
    {
        var block1 = new Block(0, "same data");
        var block2 = new Block(1, "same data");

        Assert.NotEqual(block1.Hash, block2.Hash);
    }
}
