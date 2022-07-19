using System.Numerics;
using Engi.Substrate.Keys;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.Pallets;

namespace Engi.Substrate;

public static class JobExtensions
{
    public static Task<string> CreateJobAsync(
        this SubstrateClient client,
        ChainState chainState,
        Keypair sender,
        AccountInfo senderAccount,
        BigInteger funding,
        byte[] era,
        byte tip = 0)
    {
        var (jobs, createJobVariant) = chainState.Metadata.FindPalletCallVariant("Jobs", "create_job");

        chainState.Metadata.VerifySignature(createJobVariant, (field, type) => field.Name == "funding" && type.Definition is CompactTypeDefinition);

        using var writer = new ScaleStreamWriter();

        writer.Write(jobs.Index);
        writer.Write(createJobVariant.Index);
        writer.WriteCompact(funding);

        return client.SignAndAuthorSubmitExtrinsicAsync(
            chainState, sender, senderAccount, writer.GetBytes(), era, tip);
    }
}