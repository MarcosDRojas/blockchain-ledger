using System.Security.Cryptography;
using System.Text;

namespace BlockchainLedger;

// A single record in the ledger. Each block fingerprints its own contents
// via SHA-256, and links to the block before it via PreviousHash so that
// tampering with any earlier block breaks the chain from that point forward.
public class Block
{
    // Position of this block in the chain (0 = genesis block).
    public int Index { get; }

    // When this block was created, used as part of the hash input.
    public DateTime Timestamp { get; }

    // The payload this block is recording.
    public string Data { get; }

    // Hash of the block that comes before this one. The genesis block has
    // no predecessor, so it uses "0" as a placeholder.
    public string PreviousHash { get; }

    // The value MineBlock searches over to make Hash satisfy the difficulty
    // target. Starts at 0 and only ever changes during mining.
    public int Nonce { get; private set; }

    // SHA-256 hash of Index + Timestamp + Data + PreviousHash + Nonce.
    // Mutable via mining: MineBlock keeps recomputing this as Nonce changes.
    public string Hash { get; private set; }

    public Block(int index, string data, string previousHash)
    {
        Index = index;
        Timestamp = DateTime.UtcNow;
        Data = data;
        PreviousHash = previousHash;
        Nonce = 0;
        Hash = ComputeHash();
    }

    // Hashes the block's fields together, including the link to the previous
    // block and the current Nonce, so that swapping the block or its proof
    // of work is detectable.
    private string ComputeHash()
    {
        string rawData = $"{Index}{Timestamp:O}{Data}{PreviousHash}{Nonce}";
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // Proof-of-work: repeatedly bump Nonce and rehash until Hash starts with
    // `difficulty` leading zero hex digits. This is deliberately expensive —
    // finding such a Nonce takes real, unavoidable computation (~16^difficulty
    // tries on average), while anyone else can verify the result in one hash.
    public void MineBlock(int difficulty)
    {
        string target = new string('0', difficulty);
        while (!Hash.StartsWith(target))
        {
            Nonce++;
            Hash = ComputeHash();
        }
    }
}
