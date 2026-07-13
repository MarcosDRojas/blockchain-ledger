using BlockchainLedger;
using Xunit;

namespace BlockchainLedger.Tests;

public class BlockTests
{
    [Fact]
    public void Constructor_SetsIndexDataAndPreviousHash()
    {
        var block = new Block(1, "hello", "abc123");

        Assert.Equal(1, block.Index);
        Assert.Equal("hello", block.Data);
        Assert.Equal("abc123", block.PreviousHash);
    }

    [Fact]
    public void Hash_IsA64CharacterHexString()
    {
        var block = new Block(0, "Genesis Block", "0");

        Assert.Equal(64, block.Hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", block.Hash);
    }

    [Fact]
    public void Hash_ChangesWhenDataChanges()
    {
        var block1 = new Block(0, "a", "0");
        var block2 = new Block(0, "b", "0");

        Assert.NotEqual(block1.Hash, block2.Hash);
    }

    [Fact]
    public void Hash_ChangesWhenIndexChanges()
    {
        var block1 = new Block(0, "same data", "0");
        var block2 = new Block(1, "same data", "0");

        Assert.NotEqual(block1.Hash, block2.Hash);
    }

    [Fact]
    public void Hash_ChangesWhenPreviousHashChanges()
    {
        var block1 = new Block(1, "same data", "aaa");
        var block2 = new Block(1, "same data", "bbb");

        Assert.NotEqual(block1.Hash, block2.Hash);
    }

    [Fact]
    public void Constructor_StartsWithNonceZero()
    {
        var block = new Block(0, "Genesis Block", "0");

        Assert.Equal(0, block.Nonce);
    }

    [Fact]
    public void MineBlock_ProducesHashWithRequiredLeadingZeros()
    {
        var block = new Block(0, "Genesis Block", "0");

        block.MineBlock(difficulty: 2);

        Assert.StartsWith("00", block.Hash);
    }

    [Fact]
    public void MineBlock_ChangesNonceFromInitialHash()
    {
        var block = new Block(0, "Genesis Block", "0");
        string hashBeforeMining = block.Hash;

        block.MineBlock(difficulty: 2);

        Assert.NotEqual(hashBeforeMining, block.Hash);
        Assert.NotEqual(0, block.Nonce);
    }

    [Fact]
    public void HasValidHash_TrueForAnHonestlyMinedBlock()
    {
        var block = new Block(0, "Genesis Block", "0");
        block.MineBlock(difficulty: 2);

        Assert.True(block.HasValidHash());
    }

    [Fact]
    public void HasValidHash_FalseWhenReconstructedWithAForgedHash()
    {
        // Simulates a block arriving over the network claiming a hash that
        // doesn't actually match its own fields.
        var forged = new Block(0, DateTime.UtcNow, "Genesis Block", "0", nonce: 0, hash: "not-a-real-hash");

        Assert.False(forged.HasValidHash());
    }

    [Fact]
    public void SatisfiesDifficulty_FalseWhenHashLacksLeadingZeros()
    {
        var block = new Block(0, "Genesis Block", "0");

        Assert.False(block.SatisfiesDifficulty(difficulty: 2));
    }

    [Fact]
    public void SatisfiesDifficulty_TrueAfterMiningToThatDifficulty()
    {
        var block = new Block(0, "Genesis Block", "0");
        block.MineBlock(difficulty: 2);

        Assert.True(block.SatisfiesDifficulty(difficulty: 2));
    }
}
