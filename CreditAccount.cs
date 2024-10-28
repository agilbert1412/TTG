using System;
using Newtonsoft.Json.Linq;

namespace TTGHotS
{
    class CreditAccount
    {
        private const int CREDITS_STARTING_AMOUNT = 5;

        public string discordName;
        public ulong discordId;
        public int credits;

        public CreditAccount()
        {
            Reset();
        }

        public CreditAccount(JObject data)
        {
            discordName = data["discordName"].ToString();
            discordId = ulong.Parse(data["discordId"].ToString());
            credits = Int32.Parse(data["credits"].ToString());
        }

        public void Reset()
        {
            credits = CREDITS_STARTING_AMOUNT;
        }
    }
}
