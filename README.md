# engi-blockchain-gql

## Solution overview

The solution is split in three different projects:
1. `Engi.Substrate` a class library that contains shared types.
2. `Engi.Substrate.Tests` containing tests for said types.
3. `Engi.Substrate.Server` which is an ASP.NET Core application.

## Prerequisites

Make sure you have [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) installed.

## Running the tests

From your terminal, go to `Engi.Substrate.Tests` and run:

```
dotnet test
```

If you want to mess around with the code, you can keep the tests running on every change with:

```
dotnet watch test
```

## Running the server

From your terminal, go to `Engi.Substrate.Server` and run:

```
dotnet run
```

This will launch the server on the default port `5000`. 

You can access the GraphQL UIs on the url `http://localhost:5000/ui/{graphiql|playground|altair|voyager}`.

## Integration tests

There `docker-compose-integration-tests.yml` defines a composable set of services where a substrate node and the API will be ran before running the tests.

To run, and abort the services when the tests exit, run:

```
docker-compose -f docker-compose-integration-tests.yml up --exit-code-from tests
```

Once the tests complete, a `.trx` (XML) file with the results will be available inside the `integration-test-results` directory, which is mapped from the test container.

## Off-chain indexing

In order to facilitate queries that are not supported directly by the chain, it was decided to create an off-chain index.

When the application starts, a background process (`IndexingBackgroundService`) will subscribe to notifications of new finalized blocks produced.

When a block `b` is received, it uses batched load operations to detect the number of the last block saved in the database `bPrevious`.
For the range `(number(bPrevious), b]` a new `ExpandedBlock` object is created and stored in the database with `IndexedOn = null`.

`IndexingBackgroundService` also uses a Raven subscription to receive batches of `ExpandedBlocks` where `IndexedOn = null`.
For each batch, it will fetch and decode the block and its events from the chain and store the result in the `ExpandedBlock`, this time setting `IndexedOn` to the current time.

Since the header listener will process sequentially and not in parallel (to avoid race conditions), this design ensures that `ExpandedBlocks` that 
need to be fetched and decoded are stored as soon as possible but the actual work is deferred to another thread and new headers can be received.

Decoded (expanded) blocks will store extrinsic and event information in a hierarchy (extrinsic => events, they are fetched flat from the chain) to make it then easier to further process them to answer meaningful queries. 

The second processing step comes directly from a RavenDB static index where the expanded blocks are filtered for suitable extrinsics/events and the appropriate information is extracted from them to serve the relevant queries.

As an example, the `TransactionIndex` will:
- Scan all expanded blocks and unroll all the extrinsics in them.
- For each extrinsic, detect one of four relevant types (spend, income, exchange, transfer).
- If a relevant extrinsic is not found, there is no index entry emitted.
- The block number, hash, date time produced, transaction type, executor, success status, other participants, amount and job id (if any) are stored with the index and can be retrieved with each query.