using System.Numerics;
namespace Engi.Substrate.Server.Types;

public class EngineerEarnings
{
    public BigInteger PastDay { get; set; }
    public BigInteger PastWeek { get; set; }
    public BigInteger PastMonth { get; set; }
    public BigInteger Lifetime { get; set; }
}
