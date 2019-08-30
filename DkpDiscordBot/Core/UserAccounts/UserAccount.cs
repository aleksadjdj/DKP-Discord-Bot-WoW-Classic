namespace DkpDiscordBot.Core.UserAccounts
{
    public class UserAccount
    {
        public ulong UserID { get; set; }
        public string Username { get; set; }
        public ushort DiscriminatorValue { get; set; }
        public uint PointsDKP { get; set; }
        public ulong Time { get; set; }
    }
}
