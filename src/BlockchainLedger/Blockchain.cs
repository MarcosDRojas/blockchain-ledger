namespace BlockchainLedger;

// Ties Blocks together into an ordered, tamper-evident chain: each block
// records the hash of the one before it, so altering an earlier block
// breaks the link to everything that comes after it. Every block also has
// to be mined (proof-of-work) before it's accepted into the chain.
public class Blockchain
{
    // Exposed directly (not read-only) so callers/tests can swap a block out
    // to demonstrate what tampering does to IsChainValid.
    public List<Block> Chain { get; } = new();

    // Number of leading zero hex digits a block's Hash must have to be
    // accepted. Higher = exponentially more mining work per block.
    public int Difficulty { get; }

    public Blockchain(int difficulty = 4)
    {
        Difficulty = difficulty;
        Chain.Add(CreateGenesisBlock());
    }

    // The first block has no predecessor, so it points to a placeholder hash.
    // It's mined like any other block so IsChainValid can hold it to the
    // same proof-of-work standard.
    private Block CreateGenesisBlock()
    {
        var genesis = new Block(0, "Genesis Block", "0");
        genesis.MineBlock(Difficulty);
        return genesis;
    }

    // Builds a new block linked to the current last block, mines it to meet
    // Difficulty, then appends it.
    public void AddBlock(string data)
    {
        Block previous = Chain[^1];
        var newBlock = new Block(previous.Index + 1, data, previous.Hash);
        newBlock.MineBlock(Difficulty);
        Chain.Add(newBlock);
    }

    // A chain is valid only if every block both links to the real hash of
    // its predecessor AND shows valid proof-of-work for its own hash.
    public bool IsChainValid()
    {
        string target = new string('0', Difficulty);

        for (int i = 0; i < Chain.Count; i++)
        {
            if (!Chain[i].Hash.StartsWith(target))
            {
                return false;
            }

            if (i > 0 && Chain[i].PreviousHash != Chain[i - 1].Hash)
            {
                return false;
            }
        }

        return true;
    }
}
