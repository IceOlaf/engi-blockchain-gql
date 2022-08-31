using System;
using Engi.Substrate.Keys;
using Xunit;

namespace Engi.Substrate;

public class KeypairTests
{
    const string mnemonic = "donor rocket find fan language damp yellow crouch attend meat hybrid pulse";

    [Fact]
    public void Keyring_CreateFromMnemonic_NoPassword()
    {
        var keyPair = KeypairFactory.CreateFromAny(mnemonic);
        
        Assert.Equal("5CSFNKvSFchQd7TjuuvPca1RheLAqZfFKiqAM6Fv6us9QhvR", Address.From(keyPair.PublicKey).Id);
        Assert.Equal(64, keyPair.SecretKey.Length);
        Assert.Equal("EG466y9Z5pp6iTOPEbNBruc16dEtEFWrbJAOHhg/Pgc=", Convert.ToBase64String(keyPair.PublicKey));
        Assert.Equal("2AcLSTs5BjCb0THCfV3KHszANny4bTPlhAOZ8q0oU3B6ygidfALTnuo/AUMU1Ml31HyXz4meKy8nJtUzj36qpA==", Convert.ToBase64String(keyPair.SecretKey));
    }

    [Fact]
    public void Keyring_CreateFromMnemonic_WithKeyPassword()
    {
        var keyPair = KeypairFactory.CreateFromAny(mnemonic, "Substrate");

        Assert.Equal(
            "5FRbTVsuNAXFDq19gSnwihXUDMeEQKfhDnUWgYuUq6jFknVq", 
            Address.From(keyPair.PublicKey).Id);
        Assert.Equal(64, keyPair.SecretKey.Length);
        Assert.Equal("lKNPr01GTwQExB7+rmxHbCF1VJL3e1cVzlKRzmAagxg=", Convert.ToBase64String(keyPair.PublicKey));
        Assert.Equal("4PvnUnd/nXw3qvtWvvifGLRtq6f4SJpIuFbggjicjmx2e2RqS+rWkHTHe2nRQqIZeNhnNXoiJ9FjcPimdZkQEQ==", Convert.ToBase64String(keyPair.SecretKey));
    }

    [Fact]
    public void Export()
    {
        var keyPair = KeypairFactory.CreateFromAny(mnemonic);
        var pkcs8 = keyPair.ExportToPkcs8();

        Assert.Equal(
            "MFMCAQEwBQYDK2VwBCIEINgHC0k7OQYwm9Exwn1dyh7MwDZ8uG0z5YQDmfKtKFNwesoInXwC057qPwFDFNTJd9R8l8+JnisvJybVM49+qqShIwMhABBuOusvWeaaeokzjxGzQa7nNenRLRBVq2yQDh4YPz4H", 
            Convert.ToBase64String(pkcs8)
        );
    }

    [Fact]
    public void Export_WithPassword()
    {
        var keyPair = KeypairFactory.CreateFromAny(mnemonic);

        var salt = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31  };
        var xsalsaNonce = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };
        var pkcs8 = keyPair.ExportToPkcs8("Substrate", salt, xsalsaNonce);

        Assert.Equal(
            "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8AgAAAAQAAAAgAAAAAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhdIdLCBAzJL+stg++skGJe8yLalpumk6ma4SWR5OBaDg01MQGlXOlJp5wLJcsgxjxcJ6APPvve3Y6hlFb8mcOHqRBYm7M2QDIEMu0Zdy4TdjWYC3el93e4i0jc7jrKRbl8BpH5L9HR6A6sc4WOkiHBtPbFtTaH3Vwt5+TDsEVyRlZbE+070",
            Convert.ToBase64String(pkcs8)
        );
    }
}