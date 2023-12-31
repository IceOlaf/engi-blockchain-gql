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

First, [authenticate Docker to access our ECR images](https://docs.aws.amazon.com/cli/latest/reference/ecr/get-login-password.html).

```bash
aws ecr get-login-password \
--region us-west-2 \
| docker login \
--username AWS \
--password-stdin 163803973373.dkr.ecr.us-west-2.amazonaws.com
```

You may have to add as a second flag, `--profile [PROFILE]`, to the `get-login-password` command.

To run:
```
docker-compose -f docker-compose-dev.yml up --exit-code-from api
```

For mac M1 machines, you will need to use the `ubuntu-arm64v8-latest` version of ravendb image.

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

# Windows Powershell
$env:AWS_ACCESS_KEY_ID="test"
$env:AWS_SECRET_ACCESS_KEY="test"
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

## Local development with creating bounties
1. Create a GitHub app with the following settings:
  ```
  Name: <YOUR_APP_NAME> (e.g. my-engi-app)
  Homepage: https://engi.network
  Callback URL: http://localhost:3000/oauth/github/callback
  [x] Request user authorization (OAuth) during installation

  Read/Write permissions:
  Contents
  Discussions
  Issues
  Pull Requests
  Webhooks
  ```
  
2. Update these fields in `appsettings.json` with the GitHub settings.
  ```
    "GithubAppId": <APP_ID>,
    "GithubAppPrivateKey": <PRIVATE_KEY>,
  ```
3. Log out and log back in on the locally-run website to refresh your session cookie. It has a TTL of 1 hour.
4. Navigate to https://github.com/apps/<YOUR_APP_NAME>/installations/select_target?state=uuid to install the app. This will open the callback URL and you will successfully be enrolled.

## Troubleshooting
> I'm getting a `Not Found` error from GitHub API.
>  ```json
>  {
>    "message": "Not Found",
>    "documentation_url": "https://docs.github.com/rest/apps/apps#get-an-installation-for-the-authenticated-app"
>  }
>  ```
Your private key is not configured properly for your GitHub app. In `GitHubClientFactory.cs`, use `FilePrivateKeySource` and specify the key file by name instead of loading it from options as a string.
```csharp
var generator = new GitHubJwt.GitHubJwtFactory(
    new FilePrivateKeySource("./<YOUR_PRIVATE_KEY>.pem"),
    // new Base64PrivateKeySource(options.GithubAppPrivateKey),
    new GitHubJwt.GitHubJwtFactoryOptions
    {
        AppIntegrationId = options.GithubAppId,
        ExpirationSeconds = 600
    }
);
```

---

> I'm getting a null reference error.
> ```
> fail: Engi.Substrate.Server.RootSchema[0]
>     Error occurred: Object reference not set to an instance of an object.
>     System.NullReferenceException: Object reference not set to an instance of an object.
>        at Engi.Substrate.Server.Types.Github.GithubMutations.EnrollAsync(IResolveFieldContext`1 context)
>        at Engi.Substrate.Server.Types.Github.GithubMutations.EnrollAsync(IResolveFieldContext`1 context)
>        at Engi.Substrate.Server.Types.Validation.ValidationMiddleware.ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
>        at GraphQL.Execution.ExecutionStrategy.ExecuteNodeAsync(ExecutionContext context, ExecutionNode node) in /_/src/GraphQL/Execution/ExecutionStrategy.cs:line 502
> ```
Your session cookie has expired and the API is failing to fetch your user. Log out and log back in. Refresh the callback URL and your enrollment will work.
