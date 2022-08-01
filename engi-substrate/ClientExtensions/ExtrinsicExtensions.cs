using Engi.Substrate.Keys;
using Engi.Substrate.Pallets;

namespace Engi.Substrate;

public static class ExtrinsicExtensions
{
    public const int SIGNED_EXTRINSIC = 128;

    public static Task<string> SignAndAuthorSubmitExtrinsicAsync(
        this SubstrateClient client,
        ChainState chainState,
        Keypair sender,
        AccountInfo senderAccount,
        byte[] method,
        ExtrinsicEra era,
        byte tip = 0)
    {
        var multiAddressTypeDef = chainState.Metadata.MultiAddressTypeDefinition;
        var addressType = multiAddressTypeDef.Variants.IndexOf("Id");

        var unsigned = GetSignaturePayload(method, era, senderAccount, tip, chainState);

        byte[] signature = sender.Sign(unsigned);
        byte[] rawEra = era.Serialize();

        int payloadLength = 1 // version
                            + 1 // addressType
                            + 32 // address
                            + 1 // sig type
                            + signature.Length
                            + rawEra.Length
                            + ScaleStreamWriter.GetCompactLength(senderAccount.Nonce)
                            + ScaleStreamWriter.GetCompactLength(tip)
                            + method.Length;

        using var writer = new ScaleStreamWriter();

        writer.WriteCompact((ulong)payloadLength);
        writer.Write((byte)(chainState.Metadata.Extrinsic.Version + SIGNED_EXTRINSIC));
        writer.Write(addressType);
        writer.Write(sender.Address.Raw);
        writer.Write((byte)1); // signature type
        writer.Write(signature);
        writer.Write(rawEra);
        writer.WriteCompact(senderAccount.Nonce);
        writer.WriteCompact(tip);
        writer.Write(method);

        var payloadWithLength = writer.GetBytes();

        return client.AuthorSubmitExtrinsicAsync(payloadWithLength);
    }

    private static byte[] GetSignaturePayload(
        byte[] method,
        ExtrinsicEra era,
        AccountInfo account,
        byte tip,
        ChainState chainState)
    {
        using var writer = new ScaleStreamWriter();

        var blockHash = era.IsMortal ? chainState.LatestFinalizedHeader.Hash.Value : chainState.GenesisHash;

        writer.Write(method);
        writer.Write(era);
        writer.WriteCompact(account.Nonce);
        writer.WriteCompact(tip);
        writer.Write(chainState.Version.SpecVersion);
        writer.Write(chainState.Version.TransactionVersion);
        writer.WriteHex0X(chainState.GenesisHash);
        writer.WriteHex0X(blockHash);

        return writer.GetBytes();
    }
}