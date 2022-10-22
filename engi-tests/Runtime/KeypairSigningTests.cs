using System.Security.Cryptography;
using System.Text;
using Engi.Substrate.Keys;
using Xunit;

namespace Engi.Substrate;

public class KeypairSigningTests
{
    private readonly Keypair Keypair = KeypairFactory.CreateFromAny("donor rocket find fan language damp yellow crouch attend meat hybrid pulse");

    [Fact]
    public void SignAndVerify()
    {
        byte[] message = Encoding.UTF8.GetBytes("This is a test message.");

        byte[] signature = Keypair.Sign(message);

        Assert.True(Keypair.Verify(signature, message));
    }

    [Fact]
    public void SignAndVerify_Over256Bytes()
    {
        byte[] message = RandomNumberGenerator.GetBytes(300);

        byte[] signature = Keypair.Sign(message);

        Assert.True(Keypair.Verify(signature, message));
    }
}