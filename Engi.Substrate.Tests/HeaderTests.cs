using Xunit;

namespace Engi.Substrate
{
    public class HeaderTests
    {
        [Fact]
        public void ComputeHash()
        {
            var header = new Header
            {
                Digest = new Digest
                {
                    Logs = new[]
                    {
                        "0x0642414245b5010327000000b9d2681000000000a0c87e9f909d43d4af3df2df6d711f1557ebe450f38dcafba74ad1b6fd989959b5e5c7fef83d54066ec526f48e300e30392ca87c674abebbcf81a5588ee8b2036ba05cadd690dac2f5c3565542bc6dc79383e778f7037e58f8106f454499730c",
                        "0x05424142450101be6feae9ecaf921c2b6697a9a338c4fadf8dce847cbea8e8d19438d0b2e52f4601ef2a1ef9c3744b934a1be0a00d3412401e6303c6b62bb186f42bf65b7df682"
                    }
                },
                ExtrinsicsRoot = "0xbe495a95c18e969316a20dd0bde2f2daef46493942b6416eaab38abccd3651b4",
                Number = uint.Parse("9b6b01", System.Globalization.NumberStyles.HexNumber),
                ParentHash = "0x015aaf652a5fa9fc18ffc131eaab45b19828218f45ac462fadac80e6991852db",
                StateRoot = "0xfd6e83c2d65358bc2e8cd8d301740776fa74ec3342343785b8a61173078d6049"
            };

            Assert.Equal(
                "0xf9e9c09468400f4f10839a72cca394ab343b97dad5097f7326f191f48809fe5e",
                Hex.GetString0X(header.ComputeHash()));
		}
    }
}
