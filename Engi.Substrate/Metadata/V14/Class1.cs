using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engi.Substrate.Metadata.V14
{
    public class RuntimeMetadataV14
    {
        public PortableType[] Types { get; set; }

        public PalletMetadata[]? Pallets { get; set; }

        public ExtrinsicMetadata Extrinsic { get; set; }

        public static RuntimeMetadataV14 Parse(ScaleStream stream)
        {
            return new()
            {
                Types = stream.ReadList(PortableType.Parse)
            };
        }
    }

    public class TypePortableForm
    {
        public static TypePortableForm Parse(ScaleStream stream)
        {

        }
    }

    public class PortableType
    {
        public ulong Id { get; set; }

        public TypePortableForm Ty { get; set; }

        public static PortableType Parse(ScaleStream stream)
        {
            return new()
            {
                Id = stream.ReadCompactInteger(),
                Ty = TypePortableForm.Parse(stream)
            };
        }
    }

    public class PalletMetadata
    {

    }

    public class ExtrinsicMetadata
    {

    }
}
