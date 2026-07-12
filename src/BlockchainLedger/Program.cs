using BlockchainLedger;

// Entry point: builds a small chain, proves it validates, then tampers with
// an earlier block to show the chain link breaking.
var blockchain = new Blockchain();
blockchain.AddBlock("Alice pays Bob 5 coins");
blockchain.AddBlock("Bob pays Carol 2 coins");

foreach (var block in blockchain.Chain)
{
    Console.WriteLine($"Index: {block.Index}");
    Console.WriteLine($"Data: {block.Data}");
    Console.WriteLine($"PreviousHash: {block.PreviousHash}");
    Console.WriteLine($"Hash: {block.Hash}");
    Console.WriteLine();
}

Console.WriteLine($"Chain valid? {blockchain.IsChainValid()}");

// Simulate tampering: replace block 1 with one that has different data.
// Its PreviousHash still matches block 0, but block 2's PreviousHash no
// longer matches this new block's Hash, so the chain is now invalid.
blockchain.Chain[1] = new Block(1, "Alice pays Bob 500 coins", blockchain.Chain[0].Hash);
Console.WriteLine($"Chain valid after tampering with block 1? {blockchain.IsChainValid()}");
