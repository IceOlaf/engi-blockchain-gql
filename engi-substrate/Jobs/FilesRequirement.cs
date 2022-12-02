using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Jobs;

public class FilesRequirement : IScaleSerializable
{
    [MaxLength(64)]
    public string? IsEditable { get; set; }

    [MaxLength(64)]
    public string? IsAddable { get; set; }

    [MaxLength(64)]
    public string? IsDeletable { get; set; }

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata _)
    {
        writer.WriteOptional(IsEditable != null, writer => writer.Write(IsEditable!));
        writer.WriteOptional(IsAddable != null, writer => writer.Write(IsAddable!));
        writer.WriteOptional(IsDeletable != null, writer => writer.Write(IsDeletable!));
    }

    public static FilesRequirement Parse(ScaleStreamReader reader)
    {
        return new()
        {
            IsEditable = reader.ReadOptional(reader => reader.ReadString()),
            IsAddable = reader.ReadOptional(reader => reader.ReadString()),
            IsDeletable = reader.ReadOptional(reader => reader.ReadString())
        };
    }
}
