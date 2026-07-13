using System.Net.Http.Json;
using System.Text.Json;

namespace BlockchainLedger;

// One participant in the network. Wraps this process's Blockchain plus the
// set of peers it knows about, and knows how to broadcast new blocks and
// resolve disagreements with them.
public class Node
{
    public Blockchain Blockchain { get; }

    // Base URLs of peers this node knows about, e.g. "http://localhost:5002".
    // Registration is one-directional per call — call /peers/register on
    // both nodes to form a two-way link.
    public HashSet<string> Peers { get; } = new();

    private readonly HttpClient _httpClient;

    public Node(Blockchain blockchain, HttpClient httpClient)
    {
        Blockchain = blockchain;
        _httpClient = httpClient;
    }

    public void RegisterPeer(string peerUrl) => Peers.Add(peerUrl.TrimEnd('/'));

    // Mines a block locally, appends it to this node's chain, then tells
    // every known peer about it.
    public async Task<Block> MineAndBroadcastAsync(string data)
    {
        Blockchain.AddBlock(data);
        Block minedBlock = Blockchain.Chain[^1];
        await BroadcastBlockAsync(minedBlock);
        return minedBlock;
    }

    private async Task BroadcastBlockAsync(Block block)
    {
        foreach (string peer in Peers)
        {
            try
            {
                await _httpClient.PostAsJsonAsync($"{peer}/blocks/receive", block);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
            {
                // Peer unreachable/misbehaving right now; it'll catch up
                // later via /consensus/resolve instead of blocking this
                // broadcast to the peers that did receive it.
            }
        }
    }

    // Called when a block arrives from a peer (via POST /blocks/receive).
    // Fast path: if it extends our current tip and is honestly mined, just
    // append it. Otherwise we're out of sync (missed a block, or there's a
    // fork) — fall back to full consensus against all peers.
    public async Task<bool> ReceiveBlockAsync(Block incoming)
    {
        Block currentTip = Blockchain.Chain[^1];

        if (incoming.PreviousHash == currentTip.Hash
            && incoming.HasValidHash()
            && incoming.SatisfiesDifficulty(Blockchain.Difficulty))
        {
            Blockchain.Chain.Add(incoming);
            return true;
        }

        return await ResolveConflictsAsync();
    }

    // Longest-valid-chain consensus: ask every known peer for their full
    // chain and adopt the longest one that's still valid, if it's longer
    // than what we already have. Returns true if our chain changed.
    public async Task<bool> ResolveConflictsAsync()
    {
        bool replaced = false;

        foreach (string peer in Peers)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ChainResponseDto>($"{peer}/chain");
                if (response is not null && Blockchain.ReplaceChainIfLonger(response.Chain))
                {
                    replaced = true;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
            {
                // Peer unreachable/misbehaving; skip it and try the rest.
            }
        }

        return replaced;
    }
}
