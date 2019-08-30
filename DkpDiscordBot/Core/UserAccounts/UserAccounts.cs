using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;

namespace DkpDiscordBot.Core.UserAccounts
{
    public static class UserAccounts
    {
        private static readonly List<UserAccount> accounts;

        private static readonly string accountsFile = "Resources/Accounts.json";

        static UserAccounts()
        {
            if (DataStorage.SaveExists(accountsFile))
            {
                accounts = DataStorage.LoadUserAccounts(accountsFile).ToList();
            }
            else
            {
                accounts = new List<UserAccount>();
                SaveAccounts();
            }
        }

        public static List<UserAccount> GetListAccounts()
        {
            return accounts;
        }

        public static void SaveAccounts()
        {
            DataStorage.SaveUserAccounts(accounts, accountsFile);
        }

        public static UserAccount GetAccount(SocketUser user)
        {
            return GetOrCreateAccount(user.Id, user.Username, user.DiscriminatorValue);
        }

        private static UserAccount GetOrCreateAccount(ulong id, string username, ushort discriminatorValue)
        {
            var account = accounts.Where(x => x.UserID == id).FirstOrDefault();

            if (account == null)
                account = CreateUserAccount(id, username, discriminatorValue);

            return account;
        }

        private static UserAccount CreateUserAccount(ulong id, string username, ushort discriminatorValue)
        {
            var newAccount = new UserAccount()
            {
                UserID = id,
                Username = username,
                DiscriminatorValue = discriminatorValue,
                PointsDKP = 0,
                Time = 0
            };

            accounts.Add(newAccount);
            SaveAccounts();
            return newAccount;
        }

        public static void RemoveAccount(SocketUser user)
        {
            var account = accounts.Where(x => x.UserID == user.Id).FirstOrDefault();
            accounts.Remove(account);
            SaveAccounts();

        }
    }
}
