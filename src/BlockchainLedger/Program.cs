using BlockchainLedger;

// Entry point: creates the genesis block and prints it to prove Block
// self-hashes correctly end to end.
var block = new Block(0, "Genesis Block");
Console.WriteLine($"Index: {block.Index}");
Console.WriteLine($"Timestamp: {block.Timestamp:O}");
Console.WriteLine($"Data: {block.Data}");
Console.WriteLine($"Hash: {block.Hash}");
