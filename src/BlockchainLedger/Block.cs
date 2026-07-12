using System.Security.Cryptography;
using System.Text;

namespace BlockchainLedger;

// A single record in the ledger. Each block fingerprints its own contents
// via SHA-256 so that any later tampering with Index/Timestamp/Data is detectable.
public class Block
{
    // Position of this block in the chain (0 = genesis block).
    public int Index { get; }

    // When this block was created, used as part of the hash input.
    public DateTime Timestamp { get; }

    // The payload this block is recording.
    public string Data { get; }

    // SHA-256 hash of Index + Timestamp + Data, computed once at construction time.
    public string Hash { get; }

    public Block(int index, string data)
    {
        Index = index;
        Timestamp = DateTime.UtcNow;
        Data = data;
        Hash = ComputeHash();
    }

    // Hashes the block's fields together so the block is self-verifying:
    // changing any field after construction would no longer match this hash.
    private string ComputeHash()
    {
        string rawData = $"{Index}{Timestamp:O}{Data}";
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
