using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Jobs;

public class FilesRequirement : IScaleSerializable
{
    [Required(AllowEmptyStrings = true), MaxLength(64)]
    public string IsEditable { get; set; } = null!;

    [Required(AllowEmptyStrings = true), MaxLength(64)]
    public string IsAddable { get; set; } = null!;

    [Required(AllowEmptyStrings = true), MaxLength(64)]
    public string IsDeletable { get; set; } = null!;

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata _)
    {
        writer.Write(IsEditable);
        writer.Write(IsAddable);
        writer.Write(IsDeletable);
    }

    public static FilesRequirement Parse(ScaleStreamReader reader)
    {
        return new()
        {
            IsEditable = reader.ReadString()!,
            IsAddable = reader.ReadString()!,
            IsDeletable = reader.ReadString()!
        };
    }
}