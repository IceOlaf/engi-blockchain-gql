const { Keyring } = require("@polkadot/keyring");
const {
  cryptoWaitReady,
  encodeAddress,
  naclEncrypt,
  scryptEncode,
  scryptToU8a
} = require("@polkadot/util-crypto");
const {
  assert,
  u8aConcat,
  u8aToU8a,
  stringToU8a,
} = require("@polkadot/util");

const { sr25519KeypairFromSeed } = require("@polkadot/wasm-crypto");
const { mnemonicToEntropy } = require("@polkadot/util-crypto/cjs/mnemonic/bip39");
const { pbkdf2Encode } = require("@polkadot/util-crypto/cjs/pbkdf2/encode");

const SEC_LEN = 64;
const PUB_LEN = 32;
const TOT_LEN = SEC_LEN + PUB_LEN;

function sr25519PairFromU8a(full) {
  const fullU8a = u8aToU8a(full);

  assert(
    fullU8a.length === TOT_LEN,
    () => `Expected keypair with ${TOT_LEN} bytes, found ${fullU8a.length}`
  );

  return {
    publicKey: fullU8a.slice(SEC_LEN, TOT_LEN),
    secretKey: fullU8a.slice(0, SEC_LEN),
  };
}

function sr25519FromSeed(seed) {
  const seedU8a = u8aToU8a(seed);

  assert(
    seedU8a.length === 32,
    () => `Expected a seed matching 32 bytes, found ${seedU8a.length}`
  );

  const bytes = sr25519KeypairFromSeed(seedU8a);

  return sr25519PairFromU8a(bytes);
}

const PKCS8_DIVIDER = new Uint8Array([161, 35, 3, 33, 0]);
const PKCS8_HEADER = new Uint8Array([
  48, 83, 2, 1, 1, 48, 5, 6, 3, 43, 101, 112, 4, 34, 4, 32,
]);

function encodePair({ publicKey, secretKey }, passphrase) {
  assert(secretKey, "Expected a valid secretKey to be passed to encode");

  const encoded = u8aConcat(PKCS8_HEADER, secretKey, PKCS8_DIVIDER, publicKey);

  if (!passphrase) {
    return encoded;
  }

  const salt = u8aToU8a([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31]);
  const { params, password } = scryptEncode(passphrase, salt, undefined, true);

  const nonce = u8aToU8a([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23]);
  const { encrypted } = naclEncrypt(encoded, password.subarray(0, 32), nonce);

  // console.log('password', Buffer.from(password).toString("base64"))
  // console.log('salt', Buffer.from(encrypted).toString("base64"))
  // console.log('encrypted', Buffer.from(encrypted).toString("base64"))

  var p1 = scryptToU8a(salt, params);

  console.log('salt', Buffer.from(salt).toString("base64"))
  console.log('p1', Buffer.from(p1).toString("base64"))
  console.log('nonce', Buffer.from(nonce).toString("base64"))
  console.log('encrypted', Buffer.from(encrypted).toString("base64"))

  return u8aConcat(p1, nonce, encrypted);
}

function pairToJson(type, { address, meta }, encoded, isEncrypted) {
  return {
    encoded: Buffer.from(encoded).toString("base64"),
    encoding: {
      content: ["pkcs8", type],
      type: isEncrypted ? ["scrypt", "xsalsa20-poly1305"] : ["none"],
      version: 3,
    },
    address,
    meta,
  };
}

const toJson = (keypair, passphrase) => {
  const { type, publicKey, meta, secretKey } = keypair;
  // NOTE: For ecdsa and ethereum, the publicKey cannot be extracted from the address. For these
  // pass the hex-encoded publicKey through to the address portion of the JSON (before decoding)
  // unless the publicKey is already an address
  const address = encodeAddress(publicKey);
  const encoded = encodePair({ publicKey, secretKey }, passphrase);

  return pairToJson(type, { address, meta }, encoded, !!passphrase);
};

function createFromUri(phrase, meta = {}, type, password) {
  let seed;

  const parts = phrase.split(" ");

  if ([12, 15, 18, 21, 24].includes(parts.length)) {
    const entropy = mnemonicToEntropy(phrase);
    const salt = stringToU8a(`mnemonic${password}`); // return the first 32 bytes as the seed

    seed = pbkdf2Encode(entropy, salt, 2048, true).password.slice(0, 32);
    //seed = mnemonicToMiniSecret(phrase);
  } else {
    assert(
      phrase.length <= 32,
      "specified phrase is not a valid mnemonic and is invalid as a raw seed at > 32 bytes"
    );

    seed = stringToU8a(phrase.padEnd(32));
  }

  const pair = sr25519FromSeed(seed);

  return {
    type,
    ...pair,
    meta,
  };
}

cryptoWaitReady().then(async () => {
  const mnemonic =
    "donor rocket find fan language damp yellow crouch attend meat hybrid pulse";
  //"in the end there can be only one";
  const type = "sr25519";
  const keyPassword = "";
  //const keyPassword = "Substrate";
  //const exportPassword = "";
  const exportPassword = "Substrate";

  var keypair = createFromUri(mnemonic, {}, type, keyPassword);

  console.log(Buffer.from(keypair.publicKey).toString("base64"));
  console.log(Buffer.from(keypair.secretKey).toString("base64"));

  console.log(toJson(keypair, exportPassword));

  // console.log(
  //   new Keyring({ type }).addFromMnemonic(mnemonic).toJson(exportPassword)
  // );
});
