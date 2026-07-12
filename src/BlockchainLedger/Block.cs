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

    // SHA-256 hash of Index + Timestamp + Data + PreviousHash, computed once at construction time.
    public string Hash { get; }

    public Block(int index, string data, string previousHash)
    {
        Index = index;
        Timestamp = DateTime.UtcNow;
        Data = data;
        PreviousHash = previousHash;
        Hash = ComputeHash();
    }

    // Hashes the block's fields together, including the link to the previous
    // block, so that swapping this block out for a different one is detectable.
    private string ComputeHash()
    {
        string rawData = $"{Index}{Timestamp:O}{Data}{PreviousHash}";
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
