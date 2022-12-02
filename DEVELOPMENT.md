# Development with engi-blockchain-gql

## Architecture overview

The API is built on [.NET6](https://dotnet.microsoft.com/) and uses:
- [RavenDB](https://ravendb.net/) for storage.
- [AWS SQS](https://aws.amazon.com/sqs/) and [AWS SNS](https://aws.amazon.com/sns/) to communicate with the Engine service.

## Solution overview

The solution is split in three different projects:
1. `Engi.Substrate` a class library that contains shared types.
2. `Engi.Substrate.Tests` containing tests for said types.
3. `Engi.Substrate.Server` which is an ASP.NET Core application.

## Running the environment

To make it easier to run the environment locally when you only intend to use it but not modify it,
a relevant `docker-compose` file is provided.

By default it will run:
- A RavenDB instance, mapped to port `8088` so that it doesn't interfere with a default installation on  port `8080`.
- [Localstack](https://localstack.cloud/), in order to provide the relevant SQS/SNS instances.
- `engi-node` with ports `9933` and `9944` exposed to the host so you can connect with Polkadot UI.
- The API itself, exposed on port `5000`.

To run:
```
docker-compose -f docker-compose-dev.yml up --exit-code-from api
```

To develop against another `engi-node`:
1. Comment out the `substrate` section of `docker-compose-dev.yml`.
2. Change the `SUBSTRATE__*` environment variables in `docker-compose-services.yml` and remove `substrate:*` from `WAIT_HOSTS`.

To develop against another RavenDB instance:
1. Comment out the `ravendb` section of `docker-compose-dev.yml`.
2. Add `RAVEN__URLS` environment variable with `https://<node a>:port;https://<node b>:port;https://<node c>`.

## Running the environment, with the ability to make modifications

Make sure you have installed:
- [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [RavenDB 5.4 or later](https://ravendb.net/download). Hint: use unsecured mode for local development.
- [Localstack](https://docs.localstack.cloud/get-started/#installation)
- [Rust toolchain](https://www.rust-lang.org/tools/install) if you're going to build `engi-node` from source.
- [WSL2](https://learn.microsoft.com/en-us/windows/wsl/install) if you are on Windows and want to build `engi-node`.

Then, create the AWS resources using `localstack`.

First set the relevant env vars:

```
# Linux/MacOS
export AWS_ACCESS_KEY_ID="test"
export AWS_SECRET_ACCESS_KEY="test"
export AWS_DEFAULT_REGION="us-west-2"

# Windows Powershell
$env:AWS_ACCESS_KEY_ID="test"
$env:AWS_SECRET_ACCESS_KEY="test"
$env:AWS_DEFAULT_REGION="us-west-2"
```

Start localstack:
```
localstack start
```

In another terminal run the scripts that creates the resources:
```
sh ./localstack-up.sh
```

Start RavenDB if not running as a service:

```
# Linux
sh /path/to/RavenDB/run.sh

# MacOS
sudo spctl --master-disable
sh /path/to/RavenDB/run.sh

# Windows Powershell
C:/path/to/RavenDB/run.ps1 
# or install as a service
C:/path/to/RavenDB/setup-as-service.ps1
```

Build and start `engi-node` (use WSL on Windows)

```
cd /path/to/engi-node
cargo build --release
./target/release/engi-node --dev --base-path storage --pruning archive --rpc-external --ws-external --charlie
```

Build and run the API, `watch`ing for changes:
```
cd /path/to/engi-blockchain-gql/engi-server
dotnet watch run
```

The last step will launch the server on the default port `5000`. 

You can access two graphical GraphQL clients to help with development:
- Altair: `http://localhost:5000/ui/altair` to execute queries/mutations.
- Voyager: `http://localhost:5000/ui/voyager` to explore a diagram of the GraphQL types.

Hints:
- If you don't need to do node-related work and/or don't need to be indexing the node from the API, you can
disable indexing by setting `Engi:DisableChainObserver = true` in `appsettings.json`.

## Running the tests

Tests include both unit and integrations tests.

The `docker-compose-integration-tests.yml` file can be used to run all tests by raising the set of required services.

To run, and abort the services when the tests exit, run:

```
docker-compose -f docker-compose-integration-tests.yml up --exit-code-from tests
```

Once the tests complete, a `.trx` (XML) file with the results will be available inside the `integration-test-results` directory, which is mapped from the test container.

### Executing unit tests only

Executing only the unit tests is possible with the `dotnet test --filter` [command](https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=mstest).

It is also possible to re-run the tests on every change with `dotnet watch test`, ran from the `engi-tests` directory.

## Debugging the API

The `.vscode/launch.json` configuration allows you to attach a debugger to the API process easily.

In vscode:

- Press `F5`; alternatively go to the `Run and Debug` and press the "play" button.
- A list of processed will come up - type "engi" to find the API process and press `Enter`.

Important notes: 
- If you going to make modifications make sure you're running `dotnet watch run`, not `dotnet run`.
- Since .NET 6.0 the watch process supports hot reload but the debugger will detach if you make changes since the code running doesn't match the source code any more. You won't be able to re-attached for the same reason. To fix, on the terminal where the API is running, press Ctrl/Cmd+R to reload the process and then re-attach the debugger.
- Some edits (rude edits) cannot be hot reloaded but the .NET SDK will prompt you on what to do - if you're not looking at the console you can miss it. To avoid this, set the environment variable `DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true`.

## Generating the GraphQL docs locally

To review documentation on the GraphQL schema, you can run the API as above, and visit Altair at `http://localhost:5000/ui/altair`.

Altair has a `Docs` right sidebar that contains information on the schema root types and its descendants.

Alternatively, you can generate documentation with SpectaQL by doing:

```
docker-compose -f docker-compose-generate-docs.yml up --exit-code-from docs
```

The docs end up in `engi-server/wwwroot/docs`. One way to view them:

```
npm install -g http-server
cd engi-server/wwwroot/docs/
http-server
```
