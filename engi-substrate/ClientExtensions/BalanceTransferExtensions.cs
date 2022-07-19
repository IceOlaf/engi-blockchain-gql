using Engi.Substrate.Keys;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.Pallets;

namespace Engi.Substrate;

public static class BalanceTransferExtensions
{
    public static Task<string> BalanceTransferAsync(
        this SubstrateClient client,
        ChainState chainState,
        Keypair sender,
        AccountInfo senderAccount,
        Address recipient,
        ulong amount,
        byte[] era,
        byte tip = 0)
    {
        byte[] method = GetBalanceTransferMethodPayload(chainState.Metadata, recipient, amount);

        return client.SignAndAuthorSubmitExtrinsicAsync(
            chainState, sender, senderAccount, method, era, tip);
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

        using var writer = new ScaleStreamWriter();

        writer.Write(balances.Index);
        writer.Write(transfer.Index);
        writer.Write(addressType);
        writer.Write(dest.Raw);
        writer.WriteCompact(amount);

        return writer.GetBytes();
    }
}