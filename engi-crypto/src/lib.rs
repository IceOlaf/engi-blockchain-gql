use schnorrkel::{
	ExpansionMode, MiniSecretKey, PublicKey, SecretKey, Signature
};
use std::{
	ptr, slice
};

// We must make sure that this is the same as declared in the substrate source code.
const CTX: &'static [u8] = b"substrate";

/// Generate a key pair.
///
/// * seed_raw: u8 array of length 32.
/// * keypair_raw: u8 array of length 96 where the keypair will be written to
/// 
#[no_mangle]
pub unsafe extern "C" fn sr25519_keypair_from_seed(seed_raw: *const u8, keypair_raw: *mut u8) -> () {
	let seed = slice::from_raw_parts(seed_raw, 32);

	match MiniSecretKey::from_bytes(seed) {
		Ok(mini_secret) => {
			let bytes = mini_secret
				.expand_to_keypair(ExpansionMode::Ed25519)
				.to_half_ed25519_bytes()
				.as_mut_ptr();
			
			ptr::copy_nonoverlapping(bytes, keypair_raw, 96);
		},
		_ => panic!("Invalid seed provided.")
	}
}

/// Sign a message
///
/// The combination of both public and private key must be provided.
/// This is effectively equivalent to a keypair.
///
/// * pub_key_raw: u8 array of length 32
/// * secret_raw: u8 array of length 64
/// * message_raw: u8 array of arbitrary length
/// * signature_raw: u8 array of length 64
/// 
#[no_mangle]
pub unsafe extern "C" fn sr25519_sign(pub_key_raw: *const u8, secret_raw: *const u8, message_raw: *const u8, sz: u32, signature_raw: *mut u8) -> () {
	let pub_key = slice::from_raw_parts(pub_key_raw, 32);
	let secret_key = slice::from_raw_parts(secret_raw, 64);
	let message = slice::from_raw_parts(message_raw, sz.try_into().unwrap());

	match (SecretKey::from_ed25519_bytes(secret_key), PublicKey::from_bytes(pub_key)) {
		(Ok(s), Ok(k)) => {
			let signature = s
				.sign_simple(CTX, message, &k)
				.to_bytes()
				.to_vec()
				.as_ptr() as * const u8;

			ptr::copy_nonoverlapping(signature, signature_raw, 64);
		},
		_ => panic!("Invalid secret or pubkey provided.")
	}
}

/// Verify a message and its corresponding against a public key;
///
/// * signature_raw: u8 array of length 64
/// * message_raw: u8 array of arbitrary length
/// * pubkey_raw: u8 array of length 32
#[no_mangle]
pub unsafe extern "C" fn sr25519_verify(signature_raw: *const u8, message_raw: *const u8, sz: u32, pubkey_raw: *const u8) -> bool {
	let pubkey = slice::from_raw_parts(pubkey_raw, 32);
	let signature = slice::from_raw_parts(signature_raw, 64);
	let message = slice::from_raw_parts(message_raw, sz.try_into().unwrap());

	match (Signature::from_bytes(signature), PublicKey::from_bytes(pubkey)) {
		(Ok(s), Ok(k)) => k
			.verify_simple(CTX, message, &s)
			.is_ok(),
		_ => panic!("Invalid signature or pubkey provided.")
	}
}