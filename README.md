# blockchain-ledger

A from-scratch blockchain in C#, built one concept at a time: hashing → chaining → proof-of-work → multi-node networking. The goal was to understand how each piece works before reaching for a library that hides it.

## What's in here

| Chunk | What it adds | Where |
|---|---|---|
| 1. Self-hashing block | A `Block` computes its own SHA-256 fingerprint from its contents | [Block.cs](src/BlockchainLedger/Block.cs) |
| 2. Chaining | Each block links to the previous block's hash, so editing an old block breaks everything after it | [Blockchain.cs](src/BlockchainLedger/Blockchain.cs) |
| 3. Proof-of-work | Blocks must be *mined* — a `Nonce` is searched for until the hash has N leading zeros | [Block.cs](src/BlockchainLedger/Block.cs) `MineBlock` |
| 4. Multi-node networking | Independent node processes broadcast blocks and resolve conflicts via "longest valid chain wins," rooted in a pinned, deterministic genesis block | [Node.cs](src/BlockchainLedger/Node.cs) |

## What each chunk actually taught

**Hashing (Chunk 1).** A block's `Hash` is a SHA-256 digest of its own fields (`Index`, `Timestamp`, `Data`, ...). Change any field and the hash changes — that's what makes a block *tamper-evident* on its own, before it's even part of a chain.

**Chaining (Chunk 2).** A single self-hashing block doesn't stop someone from swapping it out for a different one — nothing points *at* it. Adding `PreviousHash` (itself folded into the hash) links blocks into a sequence: replacing block *N* changes its hash, which no longer matches what block *N+1* recorded as `PreviousHash`. `IsChainValid()` walks the chain checking exactly that link.

**Proof-of-work (Chunk 3).** Anyone can construct a block with an arbitrary hash claim — what stops them is `MineBlock`: it brute-forces a `Nonce` until the hash starts with `Difficulty` leading zero hex digits. Finding such a nonce takes real, unavoidable computation (~16^difficulty tries on average); verifying one found by someone else costs a single hash. That asymmetry — expensive to produce, cheap to verify — is what makes rewriting history costly.

**Multi-node networking (Chunk 4).** A single process only ever has one copy of the truth. Multiple `Node`s (each wrapping its own `Blockchain`) need three new things:
- **Discovery** — `Peers`, a set of known peer URLs (`POST /peers/register`).
- **Propagation** — mining broadcasts the new block to every peer (`POST /blocks/receive`); a peer whose chain it doesn't cleanly extend falls back to asking around.
- **Consensus** — `ResolveConflictsAsync` asks every peer for their chain and adopts the longest one that's still fully valid ("longest valid chain wins").

**Genesis pinning (also Chunk 4).** Longest-chain consensus alone has a hole: a peer could hand a node an entirely fabricated chain — different genesis, fake history — and if it were longer, it'd win. `Blockchain.GenesisHash` is captured once and never changed; `IsValidChain` refuses any candidate chain whose first block doesn't match it. The genesis block itself is mined from **fixed** inputs (not `DateTime.UtcNow`) specifically so every node, run independently, mines the identical genesis block — the same trick real blockchains use by hard-coding theirs.

**Interactive API (Swagger).** Every endpoint below is also explorable and callable from a browser via Swagger UI at `/swagger` — useful for poking at a running node without hand-writing curl commands.

**Automated multi-node tests (Chunk 5, in progress).** Unit tests can't prove real networking works — they run in one process. `tests/BlockchainLedger.AutomationTests` uses [Testcontainers](https://testcontainers.com/) to build the real Docker image and run 2-3 actual node containers on an isolated Docker network per test, then drives them exactly like the manual demo below (register peers, mine, poll for propagation) to prove broadcast and consensus work between genuinely separate processes — not just in-memory objects.

## Project layout

```
blockchain-ledger/
├── BlockchainLedger.sln
├── src/BlockchainLedger/                  # the node: Block, Blockchain, Node, HTTP API (Program.cs), Dockerfile
├── tests/BlockchainLedger.Tests/          # unit tests (xUnit)
└── tests/BlockchainLedger.AutomationTests/  # multi-node integration tests (xUnit + Testcontainers + Docker)
```

## Running a single node

```
dotnet run --project src/BlockchainLedger --urls http://localhost:5001
```

Open `http://localhost:5001/swagger` for an interactive API explorer, or use curl:

```
GET  /chain              # this node's chain: { length, valid, chain }
GET  /peers               # known peer URLs
POST /peers/register       # { "peerUrl": "http://localhost:5002" }
POST /blocks/mine          # { "data": "Alice pays Bob 5 coins" } — mines + broadcasts
POST /blocks/receive       # a peer pushing a newly mined block
POST /consensus/resolve    # manually trigger longest-valid-chain sync against all peers
```

## Running a two-node demo (locally)

```
dotnet run --project src/BlockchainLedger --urls http://localhost:5001
dotnet run --project src/BlockchainLedger --urls http://localhost:5002   # separate terminal
```

```
curl -X POST http://localhost:5001/peers/register -H "Content-Type: application/json" -d "{\"peerUrl\":\"http://localhost:5002\"}"
curl -X POST http://localhost:5002/peers/register -H "Content-Type: application/json" -d "{\"peerUrl\":\"http://localhost:5001\"}"
curl -X POST http://localhost:5001/blocks/mine -H "Content-Type: application/json" -d "{\"data\":\"Alice pays Bob 5 coins\"}"
curl http://localhost:5002/chain   # node 2 picked up node 1's block
```

## Running a two-node demo (Docker)

This is the more realistic version: two separate containers on their own Docker network, exactly what the automation tests in Chunk 5 do programmatically.

```
docker build -f src/BlockchainLedger/Dockerfile -t blockchain-ledger-node .
docker network create blockchain-demo

docker run -d --name node1 --network blockchain-demo --network-alias node1 -p 5001:8080 blockchain-ledger-node
docker run -d --name node2 --network blockchain-demo --network-alias node2 -p 5002:8080 blockchain-ledger-node
```

Each `-p hostPort:8080` is only for *you* to reach that container from the host — including Swagger UI, in a browser:

- node1: `http://localhost:5001/swagger`
- node2: `http://localhost:5002/swagger`

Containers reach each other by their `--network-alias`, not by that host port — so peer registration uses `node1`/`node2`, not `localhost`:

```
curl -X POST http://localhost:5001/peers/register -H "Content-Type: application/json" -d "{\"peerUrl\":\"http://node2:8080\"}"
curl -X POST http://localhost:5002/peers/register -H "Content-Type: application/json" -d "{\"peerUrl\":\"http://node1:8080\"}"
curl -X POST http://localhost:5001/blocks/mine -H "Content-Type: application/json" -d "{\"data\":\"Alice pays Bob 5 coins\"}"
curl http://localhost:5002/chain   # node 2 picked up node 1's block via the Docker network
```

Clean up when done:

```
docker rm -f node1 node2
docker network rm blockchain-demo
```

## Tests

```
dotnet test tests/BlockchainLedger.Tests              # fast unit tests, no Docker required
dotnet test tests/BlockchainLedger.AutomationTests     # multi-node integration tests — requires Docker running
```

## Progress

- [x] Chunk 1 — `Block` class that computes its own SHA-256 hash
- [x] Chunk 2 — link blocks into a chain (`PreviousHash`, `AddBlock`, `IsChainValid`)
- [x] Chunk 3 — proof-of-work (mining difficulty, nonce)
- [x] Chunk 4 — multi-node networking and consensus (peer registration, broadcast, longest-valid-chain, genesis pinning)
- [x] Swagger UI for interactively exploring/calling the API
- [ ] Chunk 5 — automated multi-node integration tests (Testcontainers + Docker) — containerization, single-container smoke test, and multi-node consensus scenarios done; keeping this suite isolated from the fast unit-test run is still open
