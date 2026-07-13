using BlockchainLedger;

// Entry point: stands up this process as one network node. Run several
// copies on different ports (`dotnet run --project src/BlockchainLedger --
// --urls http://localhost:5001`) to see multiple nodes mine, broadcast, and
// reach consensus with each other.
var builder = WebApplication.CreateBuilder(args);

// One Blockchain and one Node per process — this is a single node's state.
builder.Services.AddSingleton(new Blockchain(difficulty: 4));
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<Node>();

var app = builder.Build();

// Read-only view of this node's chain, plus enough info for a peer (or a
// human with curl) to judge it: how long it is and whether it's valid.
// Peers parse this same shape when resolving consensus (see Node.ResolveConflictsAsync).
app.MapGet("/chain", (Node node) => Results.Ok(new ChainResponseDto(
    node.Blockchain.Chain.Count,
    node.Blockchain.IsChainValid(),
    node.Blockchain.Chain)));

app.MapGet("/peers", (Node node) => Results.Ok(node.Peers));

// One-directional: adds peerUrl to *this* node's peer list. Call it on both
// nodes (each pointing at the other) to form a two-way link.
app.MapPost("/peers/register", (Node node, RegisterPeerRequest request) =>
{
    node.RegisterPeer(request.PeerUrl);
    return Results.Ok(node.Peers);
});

// Mines a new block with the given data and broadcasts it to every peer.
app.MapPost("/blocks/mine", async (Node node, MineBlockRequest request) =>
{
    Block minedBlock = await node.MineAndBroadcastAsync(request.Data);
    return Results.Ok(minedBlock);
});

// A peer pushing a newly mined block at us. Accepted directly if it extends
// our tip; otherwise triggers consensus against all known peers.
app.MapPost("/blocks/receive", async (Node node, Block incoming) =>
{
    bool accepted = await node.ReceiveBlockAsync(incoming);
    return Results.Ok(new { accepted, chainLength = node.Blockchain.Chain.Count });
});

// Manually trigger longest-valid-chain consensus against all known peers.
app.MapPost("/consensus/resolve", async (Node node) =>
{
    bool replaced = await node.ResolveConflictsAsync();
    return Results.Ok(new { replaced, chainLength = node.Blockchain.Chain.Count });
});

app.Run();

record RegisterPeerRequest(string PeerUrl);
record MineBlockRequest(string Data);
