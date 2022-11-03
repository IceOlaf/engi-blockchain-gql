# engi-blockchain-gql

## Authentication

### Creating account

The API is protected and you must create a user before interacting with it. To do that you can issue the following mutation:

```
mutation {
  auth {
    register(
      user: {
        display: "georgiosd"
        email: "georgiosd@gmail.com"
        address: "5G1GQ5bb1bjBUwjSBcArBkbK5gfrW9nTJLhnz3G3nLDo1g5n"
      }
    )
  }
}
```

Once the user is registered, you may use the `user.importKey` mutation to have ENGI manage their key so that they can invoke mutations through the API.

```
mutation {
  user {
    importKey(
      args: {
        encryptedPkcs8Key: "dZWIrU5Bwj4vFFhoLzGHq0o/F4W0ItJf5+sKmaHaCDJdveMHRSP/HGo35yBQMvMl5+ST69dLYYKfy0nZgMIEhfpf5Hywfm0WJ6MoegwPiRNtZr72P3zFifnVODlCUGuH8X8w1mxLmQqpCqjmivswQ80eMZ76KEI3t5h7QTi4LrCUEXl5ICNbzavSpSFxowqkNj1RTyJTJ4pEUYRkIgefDhQmpu1N+AlzXNoTFtqZckzqMVdltV0dQJjJZprByQ7b/RdO6Rl72iKW8a03dyB6aRRRHCh+DJeO9auYtju7DjkOe77hPMh4dsynMbTzgy8NUvIlSNxPhxIXiMxNDy4pww=="
      }
    )
  }
}
```

To generate the value of `encryptedPkcs8key` you need to encrypt your keypair using ENGI's public key; e.g.
```
const fetch = require("node-fetch");
const { Keyring } = require("@polkadot/keyring");
const { u8aToBuffer } = require("@polkadot/util");
const { JSEncrypt } = require("nodejs-jsencrypt"); // use jsencrypt on the web

async function main() {
  const publicKey = await fetch(
    "http://localhost:5000/api/engi/public-key"
  ).then((r) => r.text());

  const rsa = new JSEncrypt();
  rsa.setPublicKey(publicKey);

  const pair = new Keyring().addFromMnemonic(
    "ridge accuse cotton debate step theory fade bench flock liar seek day",
    undefined,
    "sr25519"
  );

  const pkcs8 = u8aToBuffer(pair.encodePkcs8());
  const base64 = pkcs8.toString("base64");

  console.log(rsa.encrypt(base64));
}

main();
```

### Logging in

To login, you must sign a string containing your address and the current unix milliseconds value and submit it along with your address and the timestamp:

```
mutation {
  auth {
    login(
      args: {
        address: "5G1GQ5bb1bjBUwjSBcArBkbK5gfrW9nTJLhnz3G3nLDo1g5n"
        signature {
            signedOn: "2022-09-11T17:20:17.6004682Z"
            value: "0xde22c5e3455e298473da96367cc95200c4c09ca8dcc3db1070661df92f326d683c59861de284c390b3ddfac815ff3e141310dfcd136f30e0bd969f737220b281"
        }
      }
    ) {
      accessToken
    }
  }
}
```

From the docs:

```
The hex-formatted signature, calculated with the user's private key, for the string `{address}|{unixTimeMs}`, where 'address' is the address submitted and 'unixTimeMs' the current time, in milliseconds since the UNIX epoch.
```

Example in JS:
```
const axios = require("axios");
const { Keyring } = require("@polkadot/keyring");
const { waitReady } = require("@polkadot/wasm-crypto");
const { u8aToHex, hexToU8a, stringToHex } = require("@polkadot/util");
const { gql } = require('graphql-request');

async function main() {
  await waitReady();

  const pair = new Keyring().addFromMnemonic(
    "unlock romance holiday fruit prefer mail chuckle banner margin oval unusual keen",
    undefined,
    "sr25519"
  );

  const dt = new Date();

  const payload = stringToHex(`${pair.address}|${dt.getTime()}`)
  const signature = u8aToHex(pair.sign(hexToU8a(payload)));

  const response = await axios.post("http://localhost:5000/api/graphql", {
    query: gql`
      mutation LoginUser($loginArgs: LoginArguments!) {
        auth {
          login(args: $loginArgs) {
            accessToken
          }
        }
      }
    `,
    operationName: "LoginUser",
    variables: {
      loginArgs: {
        address: pair.address,
        signature: {
          signedOn: dt.toISOString(),
          value: signature,
        },
      },
    },
  });

  console.log(response.data.data.auth.login)
}

main();
```

The validation runs as:

```
string expectedSignatureContent =
    $"{address}|{new DateTimeOffset(SignedOn).ToUniversalTime().ToUnixTimeMilliseconds()}";

bool valid = address.Verify(
    Hex.GetBytes0X(Signature),
    Encoding.UTF8.GetBytes(expectedSignatureContent));

return valid && SignedOn < DateTime.UtcNow.Add(tolerance);
```

`tolerance` is the amount of time that can pass between the timestamp you sign and submit and the time you invoke the mutation. The default is 5 seconds, but you can make it more tolerant in appsettings.json, `Engi.SignatureSkew`.

On successful invocation of the login mutation, the API will return an `accessToken` in the GQL response, and create a secure cookie called `refreshToken` with a refresh token.

On each request, you must attach the access token as header `Authorization: Bearer <access token>`. Altair permits you to do this from the headers button.

Best practices:

- Hold the access token in memory only. It's a JWT populated with the user's id and roles, should you need to use them.
- If your access token expires (by way of checking it's expiry or getting an AUTHENTICATION_FAILED from the API), call the  `refreshToken` mutation, which will read the cookie and issue a new token. If you're starting from cold (e.g. refresh), you can call this mutation to check if the user is logged in and gets a new access token.

### Invoking signed mutations

All mutations (thus far - except those under `auth`) require you to sign a payload with the user's key and send it to the server, like you did with login. Example:

```
mutation {
  currency {
    balanceTransfer(
      transfer: {
        destination: "5GrwvaEF5zXb26Fz9rcQpDWS57CtERHpNehXCPcNoHGKutQY"
        amount: 100
      }
      signature: {
        signedOn: "2022-09-11T17:16:18.6421808Z"
        value: "0xbe2ec27e2dcd710f11d3f152978cb8e43e40e81cec235c806ffcdd02ca2c050a336bbdbdd2ac148e4d4d2df31a8796efd350566fb882dd228d87307804b27380"
      }
    )
  }
}
```

### Elevated (Sudo) Authentication

Elevated access is provided to other services that interact with the API by way of using API key authentication via a custom header.

```
X-API-KEY: <api key>
```

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