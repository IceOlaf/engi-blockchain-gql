using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public class MultiAddress
{
    public byte Type { get; init; }

    public string Value { get; init; } = null!;

    public static MultiAddress Parse(ScaleStreamReader reader, RuntimeMetadata meta)
    {
        int addressType = reader.ReadByte();

        if (addressType != meta.MultiAddressTypeDefinition.Variants.IndexOf("Id"))
        {
            throw new NotImplementedException("Only Id address types can be decoded.");
        }

        var address = Address.Parse(reader);

        return new()
        {
            Type = (byte) addressType,
            Value = address.Id
        };
    }
}