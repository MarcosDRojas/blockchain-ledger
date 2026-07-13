namespace BlockchainLedger;

// Shape of GET /chain: enough for a human (Length, Valid) and a peer (the
// raw Chain) to both make sense of a node's state from the same response.
public record ChainResponseDto(int Length, bool Valid, List<Block> Chain);
