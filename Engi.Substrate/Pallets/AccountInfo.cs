namespace Engi.Substrate.Pallets
{
    /// <see cref="https://crates.parity.io/frame_system/struct.AccountInfo.html"/>
    public class AccountInfo
    {
        public uint Nonce { get; set; }

        public uint Consumers { get; set; }

        public uint Providers { get; set; }

        public uint Sufficients { get; set; }

        public AccountData Data { get; set; } = null!;

        public static AccountInfo Parse(ScaleStreamReader stream)
        {
            return new()
            {
                Nonce = stream.ReadUInt32(),
                Consumers = stream.ReadUInt32(),
                Providers = stream.ReadUInt32(),
                Sufficients = stream.ReadUInt32(),
                Data = AccountData.Parse(stream)
            };
        }
    }
}
