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
