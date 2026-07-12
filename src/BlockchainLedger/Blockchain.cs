namespace BlockchainLedger;

// Ties Blocks together into an ordered, tamper-evident chain: each block
// records the hash of the one before it, so altering an earlier block
// breaks the link to everything that comes after it.
public class Blockchain
{
    // Exposed directly (not read-only) so callers/tests can swap a block out
    // to demonstrate what tampering does to IsChainValid.
    public List<Block> Chain { get; } = new();

    public Blockchain()
    {
        Chain.Add(CreateGenesisBlock());
    }

    // The first block has no predecessor, so it points to a placeholder hash.
    private static Block CreateGenesisBlock() => new(0, "Genesis Block", "0");

    // Appends a new block that links back to the current last block.
    public void AddBlock(string data)
    {
        Block previous = Chain[^1];
        Chain.Add(new Block(previous.Index + 1, data, previous.Hash));
    }

    // Walks the chain checking that every block's PreviousHash matches the
    // actual hash of the block before it — the link tampering breaks.
    public bool IsChainValid()
    {
        for (int i = 1; i < Chain.Count; i++)
        {
            if (Chain[i].PreviousHash != Chain[i - 1].Hash)
            {
                return false;
            }
        }

        return true;
    }
}
