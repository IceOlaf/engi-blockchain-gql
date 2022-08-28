using Engi.Substrate.Jobs;
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
        CreateJobArguments args)
    {
        var (jobs, createJobVariant) = chainState.Metadata.FindPalletCallVariant("Jobs", "create_job");

        chainState.Metadata.VerifySignature(createJobVariant, 
            (field, type, innerType) => field.Name == "funding" && type.Definition is CompactTypeDefinition && innerType!.FullName == "u128",
            (field, type, _) => field.Name == "language" && type.Definition is VariantTypeDefinition v && v.Variants.Count == 1,
            (field, _, _) => field.Name == "repository_url",
            (field, _, _) => field.Name == "branch_name",
            (field, _, _) => field.Name == "commit_hash",
            (field, _, innerType) => field.Name == "tests" && innerType!.Definition is CompositeTypeDefinition c && c.Fields.Count == 3,
            (field, _, _) => field.Name == "name",
            (field, type, _) => field.Name == "files_requirement" && type.Definition is TupleTypeDefinition t && t.Fields.Length == 3
        );

        using var writer = new ScaleStreamWriter();

        writer.Write(jobs.Index);
        writer.Write(createJobVariant.Index);
        writer.Write(args);

        return client.SignAndAuthorSubmitExtrinsicAsync(
            chainState, sender, senderAccount, writer.GetBytes(), ExtrinsicEra.Immortal, args.Tip);
    }

    public static Task<string> AttemptJobAsync(
        this SubstrateClient client,
        ChainState chainState,
        Keypair sender,
        AccountInfo senderAccount,
        AttemptJobArguments args)
    {
        var (jobs, attemptJobVariant) = chainState.Metadata.FindPalletCallVariant("Jobs", "attempt_job");

        chainState.Metadata.VerifySignature(attemptJobVariant,
            (field, type, _) => field.Name == "job_id" && type.FullName == "u64",
            (field, _, _) => field.Name == "submission_patch_file_url");

        using var writer = new ScaleStreamWriter();

        writer.Write(jobs.Index);
        writer.Write(attemptJobVariant.Index);
        writer.Write(args);

        return client.SignAndAuthorSubmitExtrinsicAsync(
            chainState, sender, senderAccount, writer.GetBytes(), ExtrinsicEra.Immortal, args.Tip);
    }
}