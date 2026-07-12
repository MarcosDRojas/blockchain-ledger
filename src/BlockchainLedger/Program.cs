using System.Diagnostics;
using BlockchainLedger;

// Entry point: mines a small chain (timing each block to show proof-of-work
// is real work), confirms it validates, then tampers with an earlier block
// to show the chain link breaking.
var blockchain = new Blockchain(difficulty: 4);
Console.WriteLine($"Genesis mined with Nonce: {blockchain.Chain[0].Nonce}");

var stopwatch = new Stopwatch();
foreach (var data in new[] { "Alice pays Bob 5 coins", "Bob pays Carol 2 coins" })
{
    stopwatch.Restart();
    blockchain.AddBlock(data);
    stopwatch.Stop();
    Console.WriteLine($"Mined \"{data}\" in {stopwatch.ElapsedMilliseconds}ms");
}

Console.WriteLine();
foreach (var block in blockchain.Chain)
{
    Console.WriteLine($"Index: {block.Index}");
    Console.WriteLine($"Data: {block.Data}");
    Console.WriteLine($"Nonce: {block.Nonce}");
    Console.WriteLine($"PreviousHash: {block.PreviousHash}");
    Console.WriteLine($"Hash: {block.Hash}");
    Console.WriteLine();
}

Console.WriteLine($"Chain valid? {blockchain.IsChainValid()}");

// Simulate tampering: replace block 1 with different data, re-mined so its
// own hash still meets the difficulty target. Its PreviousHash still matches
// block 0, but block 2's PreviousHash no longer matches this new block's
// Hash, so the chain is now invalid.
var tamperedBlock = new Block(1, "Alice pays Bob 500 coins", blockchain.Chain[0].Hash);
tamperedBlock.MineBlock(blockchain.Difficulty);
blockchain.Chain[1] = tamperedBlock;
Console.WriteLine($"Chain valid after tampering with block 1? {blockchain.IsChainValid()}");
