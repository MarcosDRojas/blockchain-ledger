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

    // Hash of the genesis block this node started from. Pinned at
    // construction and never changed afterward — IsValidChain refuses any
    // candidate chain whose block 0 doesn't match it, no matter how long or
    // otherwise-valid that chain is. Without this, a peer could hand a node
    // an entirely fabricated chain (different genesis, fake history) and,
    // as long as it were longer, this node would replace its real history
    // with it. Pinning limits a peer to only ever extending a chain that
    // shares this node's actual origin.
    public string GenesisHash { get; }

    // Fixed rather than DateTime.UtcNow so every node mines the exact same
    // genesis block from the exact same inputs — deterministic proof-of-work.
    // That's what lets independently-started nodes recognize each other as
    // being on the same chain in the first place. Real blockchains do the
    // same thing by hard-coding their genesis block.
    private static readonly DateTime GenesisTimestamp = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public Blockchain(int difficulty = 4)
    {
        Difficulty = difficulty;
        Block genesis = CreateGenesisBlock();
        GenesisHash = genesis.Hash;
        Chain.Add(genesis);
    }

    // The first block has no predecessor, so it points to a placeholder hash.
    // Built via the "reconstruct as-is" constructor (not the mining one) so
    // Timestamp can be fixed instead of DateTime.UtcNow, then mined like any
    // other block so IsChainValid can hold it to the same proof-of-work standard.
    private Block CreateGenesisBlock()
    {
        var genesis = new Block(index: 0, timestamp: GenesisTimestamp, data: "Genesis Block", previousHash: "0", nonce: 0, hash: string.Empty);
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

    // A chain is valid only if it starts from this node's pinned genesis
    // block, and every block from there has an honest, unforged hash, earns
    // the required proof-of-work, and links to the real hash of its
    // predecessor. Works on any candidate list, not just this instance's own
    // Chain, so it can vet a chain received from a peer before adopting it.
    public bool IsValidChain(IReadOnlyList<Block> candidateChain)
    {
        if (candidateChain.Count == 0 || candidateChain[0].Hash != GenesisHash)
        {
            return false;
        }

        for (int i = 0; i < candidateChain.Count; i++)
        {
            Block block = candidateChain[i];

            if (!block.HasValidHash() || !block.SatisfiesDifficulty(Difficulty))
            {
                return false;
            }

            if (i > 0 && block.PreviousHash != candidateChain[i - 1].Hash)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsChainValid() => IsValidChain(Chain);

    // The consensus rule this node follows when it disagrees with a peer:
    // the longest chain that's still fully valid wins. Returns true if the
    // candidate replaced this node's chain.
    public bool ReplaceChainIfLonger(List<Block> candidateChain)
    {
        if (candidateChain.Count <= Chain.Count || !IsValidChain(candidateChain))
        {
            return false;
        }

        Chain.Clear();
        Chain.AddRange(candidateChain);
        return true;
    }
}
