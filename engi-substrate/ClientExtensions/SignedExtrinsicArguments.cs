using Engi.Substrate.Keys;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.Pallets;

namespace Engi.Substrate;

public class SignedExtrinsicArguments<TExtrinsic> : IScaleSerializable where TExtrinsic : IExtrinsic
{
    private readonly byte addressType;

    public SignedExtrinsicArguments(
        Keypair sender,
        TExtrinsic extrinsic, 
        AccountInfo accountInfo, 
        ExtrinsicEra era, 
        ChainState chainState, 
        byte tip) 
    {
        CallIndex = extrinsic.VerifySignature(chainState.Metadata);
        Extrinsic = extrinsic;
        Payload = extrinsic.Serialize(chainState.Metadata);
        Era = era;
        AccountNonce = accountInfo.Nonce;
        Tip = tip;
        RuntimeVersion = chainState.Version;
        GenesisHash = chainState.GenesisHash;
        BlockHash = era.IsMortal ? chainState.LatestFinalizedHeader.ParentHash : chainState.GenesisHash;
        ExtrinsicVersion = chainState.Metadata.Extrinsic.Version;
        Sender = sender;

        addressType = chainState.Metadata.MultiAddressTypeDefinition.Variants.IndexOf("Id");
    }

    public PalletCallIndex CallIndex { get; init; }

    public TExtrinsic Extrinsic { get; init; }

    public byte[] Payload { get; init; }

    public ExtrinsicEra Era { get; init; }

    public uint AccountNonce { get; init; }

    public byte Tip { get; init; }

    public RuntimeVersion RuntimeVersion { get; init; }

    public string GenesisHash { get; init; }

    public string BlockHash { get; init; }

    public int ExtrinsicVersion { get; }

    public Keypair Sender { get; }

    public virtual void Serialize(ScaleStreamWriter writer, RuntimeMetadata meta)
    {
        var methodCall = SerializeMethodCall(meta);

        byte[] signature = CreateSignature(meta);

        int payloadLength = 1 // version
                            + 1 // addressType
                            + 32 // address
                            + 1 // sig type
                            + signature.Length
                            + Era.CalculateLength()
                            + ScaleStreamWriter.GetCompactLength(AccountNonce)
                            + ScaleStreamWriter.GetCompactLength(Tip)
                            + methodCall.Length;

        writer.WriteCompact((ulong)payloadLength);
        writer.Write((byte)(ExtrinsicVersion + SIGNED_EXTRINSIC));
        writer.Write(addressType);
        writer.Write(Sender.Address, meta);
        writer.Write((byte)1); // signature type
        writer.Write(signature);
        writer.Write(Era, meta);
        writer.WriteCompact(AccountNonce);
        writer.WriteCompact(Tip);
        writer.Write(methodCall);
    }

    private byte[] SerializeMethodCall(RuntimeMetadata meta)
    {
        using var writer = new ScaleStreamWriter();

        writer.Write(CallIndex, meta);
        writer.Write(Payload);

        return writer.GetBytes();
    }
    private byte[] CreateSignature(RuntimeMetadata meta)
    {
        using var writer = new ScaleStreamWriter();

        writer.Write(CallIndex, meta);
        writer.Write(Payload);
        writer.Write(Era, meta);
        writer.WriteCompact(AccountNonce);
        writer.WriteCompact(Tip);
        writer.Write(RuntimeVersion.SpecVersion);
        writer.Write(RuntimeVersion.TransactionVersion);
        writer.WriteHex0X(GenesisHash);
        writer.WriteHex0X(BlockHash);

        return Sender.Sign(writer.GetBytes());
    }

    public const int SIGNED_EXTRINSIC = 128;
}