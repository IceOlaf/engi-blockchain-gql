using System.Numerics;

namespace Engi.Substrate.Server.Types;

public class CreateJobInput : SignedExtrinsicInputBase
{
    public BigInteger Funding { get; set; }
}