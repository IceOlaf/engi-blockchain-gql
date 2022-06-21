using Engi.Substrate.Keys;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.Pallets;

namespace Engi.Substrate;

public static class BalanceTransferExtensions
{
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
        var (balances, transfer) = snapshot.Metadata
            .FindPalletCallVariant("balances", "transfer");

        snapshot.Metadata.VerifySignature(transfer,
            (field, type) => field.Name == "dest" && type.FullName == "sp_runtime:multiaddress:MultiAddress",
            (field, type) => field.Name == "value" && type.Definition is CompactTypeDefinition);

        var addressType = snapshot.Metadata.MultiAddressTypeDefinition.Variants.IndexOf("Id");

        using var ms = new MemoryStream();
        using var writer = new ScaleStreamWriter(ms);

        writer.Write(balances.Index);
        writer.Write(transfer.Index);
        writer.Write(addressType);
        writer.Write(recipient.Raw);
        writer.WriteCompact(amount);

        return client.SignAndAuthorSubmitExtrinsicAsync(
            snapshot, sender, senderAccount, ms.ToArray(), era, tip);
    }
    
    private static byte[] GetBalanceTransferMethodPayload(
        RuntimeMetadata meta,
        Address dest,
        ulong amount)
    {
        var (balances, transfer) = meta
            .FindPalletCallVariant("balances", "transfer");

        meta.VerifySignature(transfer,
            (field, type) => field.Name == "dest" && type.FullName == "sp_runtime:multiaddress:MultiAddress",
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