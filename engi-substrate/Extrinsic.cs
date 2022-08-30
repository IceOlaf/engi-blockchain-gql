using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public class Extrinsic
{
    public int Version { get; init; }

    public ExtrinsicSignature? Signature { get; init; }

    public string PalletName { get; init; } = null!;

    public string CallName { get; init; } = null!;

    public Dictionary<string, object> Arguments { get; init; } = null!;

    public string[] ArgumentKeys => Arguments.Keys.ToArray();

    public EventRecordCollection Events { get; set; } = null!;

    public bool IsSuccessful
    {
        get
        {
            if (PalletName == "Sudo")
            {
                return Events.Any(e => e.Event.Section == "Sudo" && e.Event.Method == "Sudid" && e.Event.DataKeys.Contains("Ok"));
            }

            return Events.Any(e => e.Event.Section == "System" && e.Event.Method == "ExtrinsicSuccess");
        }
    }

    public static Extrinsic Parse(string s, RuntimeMetadata meta)
    {
        const byte BIT_SIGNED = 0b10000000;
        const byte UNMASK_VERSION = 0b01111111;

        using var reader = new ScaleStreamReader(s);

        ulong length = reader.ReadCompactInteger();
        int secondByte = reader.ReadByte();
        int version = secondByte & UNMASK_VERSION;

        if (version != 4)
        {
            throw new NotImplementedException("Only extrinsics V4 can be decoded.");
        }

        bool isSigned = (secondByte & BIT_SIGNED) == BIT_SIGNED;

        ExtrinsicSignature? signature = null;

        if (isSigned)
        {
            var address = MultiAddress.Parse(reader, meta);
            byte signatureType = (byte) reader.ReadByte();
            byte[] rawSignature = reader.ReadFixedSizeByteArray(64);
            var era = ExtrinsicEra.Parse(reader);
            byte nonce = (byte) reader.ReadByte();
            byte tip = (byte) reader.ReadByte();

            if (signatureType != 1)
            {
                throw new NotImplementedException("Only Sr25519 signatures are supported.");
            }

            // TODO: validate signature

            signature = new()
            {
                Address = address,
                Signature = rawSignature,
                Era = era,
                Nonce = nonce,
                Tip = tip
            };
        }

        int moduleIndex = reader.ReadByte();
        int callIndex = reader.ReadByte();

        var (pallet, call) = meta.FindPalletCallVariant(moduleIndex, callIndex);

        var fields = new Dictionary<string, object>();

        foreach (var field in call.Fields)
        {
            if (string.IsNullOrEmpty(field.Name))
            {
                throw new NotImplementedException(
                    $"Field without a name in module={moduleIndex} call={callIndex}");
            }

            fields[field.Name] = reader.Deserialize(field, meta);
        }

        return new()
        {
            Version = version,
            PalletName = pallet.Name,
            CallName = call.Name,
            Signature = signature,
            Arguments = fields
        };
    }
}