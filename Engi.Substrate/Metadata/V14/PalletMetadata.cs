﻿namespace Engi.Substrate.Metadata.V14;

public class PalletMetadata
{
    public string? Name { get; set; }

    public PalletStorageMetadata? Storage { get; set; }

    public PalletCallMetadata Calls { get; set; } = null!;

    public PalletEventMetadata? Events { get; set; }

    public PalletConstantMetadata[]? Constants { get; set; }

    public PalletErrorMetadata? Errors { get; set; }

    public byte Index { get; set; }

    public static PalletMetadata Parse(ScaleStreamReader stream)
    {
        return new PalletMetadata
        {
            Name = stream.ReadString(),
            Storage = stream.ReadOptional(PalletStorageMetadata.Parse),
            Calls = stream.ReadOptional(PalletCallMetadata.Parse)!,
            Events = stream.ReadOptional(PalletEventMetadata.Parse),
            Constants = stream.ReadList(PalletConstantMetadata.Parse),
            Errors = stream.ReadOptional(PalletErrorMetadata.Parse),
            Index = (byte)stream.ReadByte()
        };
    }
}