using Engi.Substrate.Keys;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.Pallets;

namespace Engi.Substrate;

public static class BalanceTransferExtensions
{
    private const int SIGNED_EXTRINSIC = 128;

    public static Task<string> BalanceTransferAsync(
        this SubstrateClient client,
        ChainSnapshot snapshot,
        Keypair sender,
        AccountInfo senderAccount,
        Address recipient,
        ulong amount,
        byte[] era,
        byte tip = 0)
    {
        var method = GetBalanceTransferMethodPayload(snapshot.Metadata, recipient, amount);

        var multiAddressTypeDef = snapshot.Metadata.MultiAddressTypeDefinition;
        var addressType = multiAddressTypeDef.Variants.IndexOf("Id");

        var unsigned = GetSignaturePayload(method, era, senderAccount, tip, snapshot);

        byte[] signature = sender.Sign(unsigned);

        int payloadLength = 1 // version
            + 1 // addressType
            + 32 // address
            + 1 // sig type
            + signature.Length
            + era.Length
            + ScaleStreamWriter.GetCompactLength(senderAccount.Nonce)
            + ScaleStreamWriter.GetCompactLength(tip)
            + method.Length;

        using var ms = new MemoryStream();
        using var writer = new ScaleStreamWriter(ms);

        writer.WriteCompact((ulong)payloadLength);
        writer.Write((byte)(snapshot.Metadata.Extrinsic.Version + SIGNED_EXTRINSIC));
        writer.Write(addressType);
        writer.Write(sender.Address.Raw);
        writer.Write((byte)1); // signature type
        writer.Write(signature);
        writer.Write(era);
        writer.WriteCompact(senderAccount.Nonce);
        writer.WriteCompact(tip);
        writer.Write(method);

        var payloadWithLength = ms.ToArray();

        return client.AuthorSubmitExtrinsicAsync(payloadWithLength);
    }

    private static byte[] GetSignaturePayload(
        byte[] method,
        byte[] era,
        AccountInfo account,
        byte tip,
        ChainSnapshot snapshot)
    {
        using var ms = new MemoryStream();
        using var writer = new ScaleStreamWriter(ms);

        var blockHash = Era.IsImmortal(era) ? snapshot.GenesisHash : snapshot.LatestHeader.ParentHash;

        writer.Write(method);
        writer.Write(era);
        writer.WriteCompact(account.Nonce);
        writer.WriteCompact(tip);
        writer.Write(snapshot.RuntimeVersion.SpecVersion);
        writer.Write(snapshot.RuntimeVersion.TransactionVersion);
        writer.WriteHex0X(snapshot.GenesisHash);
        writer.WriteHex0X(blockHash);

        return ms.ToArray();
    }

    private static byte[] GetBalanceTransferMethodPayload(
        RuntimeMetadata meta,
        Address dest,
        ulong amount)
    {
        var (balances, transfer) = meta
            .FindPalletCallVariant("balances", "transfer");

        meta.VerifySignature(transfer,
            (field, type) => field.Name == "dest" && type.FullPath == "sp_runtime:multiaddress:MultiAddress",
            (field, type) => field.Name == "value" && type.Definition is CompactTypeDefinition);

        var addressType = meta.MultiAddressTypeDefinition.Variants.IndexOf("Id");

        using var ms = new MemoryStream();
        using var writer = new ScaleStreamWriter(ms);

        writer.Write(balances.Index);
        writer.Write(transfer.Index);
        writer.Write(addressType);
        writer.Write(dest.Raw);
        writer.WriteCompact(amount);

        return ms.ToArray();
    }
}