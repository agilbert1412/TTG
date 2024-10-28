using System;
using Discord.WebSocket;
using TTGHotS.Discord;

namespace TTGHotS.Commands
{
    internal class CreditsCommandsHandler
    {
        private readonly IBotCommunicator _communications;
        private readonly CommandReader _commandReader;

        public CreditsCommandsHandler(IBotCommunicator discord, CommandReader commandReader)
        {
            _communications = discord;
            _commandReader = commandReader;
        }

        public void HandleCreditsAdminCommands(SocketUserMessage message, string messageText, CreditAccounts creditAccounts)
        {
            creditAccounts.CreateBackup(5);

            HandleAddCredits(message, messageText, creditAccounts);
            HandleRemoveCredits(message, messageText, creditAccounts);
            HandleSetCredits(message, messageText, creditAccounts);
            HandleResetCredits(message, messageText, creditAccounts);
            HandleResetAllCredits(message, messageText, creditAccounts);
            HandleReadCreditsOfSomeone(message, messageText, creditAccounts);

            // Add Credits to everyone
        }

        public void HandleCreditsUserCommands(SocketUserMessage message, string messageText, CreditAccounts creditAccounts)
        {
            HandleReadCredits(message, messageText, creditAccounts);
            HandleTransferCredits(message, messageText, creditAccounts);
        }

        private void HandleAddCredits(SocketUserMessage message, string messageText, CreditAccounts creditAccounts)
        {
            if (!messageText.StartsWith("!addcredits "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out ulong discordId, out var creditsAmount))
            {
                _communications.ReplyTo(message, "Usage: !addcredits [discordId] [amount]");
                return;
            }

            AddCredits(message, creditAccounts, discordId, creditsAmount);
        }

        private void HandleRemoveCredits(SocketUserMessage message, string messageText, CreditAccounts creditAccounts)
        {
            if (!messageText.StartsWith("!removecredits "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out ulong discordId, out var creditsAmount))
            {
                _communications.ReplyTo(message, "Usage: !removecredits [discordId] [amount]");
                return;
            }

            RemoveCredits(message, creditAccounts, discordId, creditsAmount);
        }

        private void HandleSetCredits(SocketUserMessage message, string messageText, CreditAccounts creditAccounts)
        {
            if (!messageText.StartsWith("!setcredits "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out ulong discordId, out var creditsAmount))
            {
                _communications.ReplyTo(message, "Usage: !setcredits [discordId] [amount]");
                return;
            }

            SetCredits(message, creditAccounts, discordId, creditsAmount);
        }

        private void HandleResetCredits(SocketUserMessage message, string messageText, CreditAccounts creditAccounts)
        {
            if (!messageText.StartsWith("!resetcredits "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out ulong discordId))
            {
                _communications.ReplyTo(message, "Usage: !resetcredits [discordId]");
                return;
            }

            ResetCredits(message, creditAccounts, discordId);
        }

        private void HandleResetAllCredits(SocketUserMessage message, string messageText, CreditAccounts creditAccounts)
        {
            if (!messageText.Equals("!resetallcredits"))
            {
                return;
            }

            ResetAllCredits(message, creditAccounts);
        }

        private void HandleReadCredits(SocketUserMessage message, string messageText, CreditAccounts creditAccounts)
        {
            if (!messageText.Equals("!credits"))
            {
                return;
            }

            TellUserHisCreditAmount(message, creditAccounts);
        }

        private void HandleTransferCredits(SocketUserMessage message, string messageText, CreditAccounts creditAccounts)
        {
            if (!messageText.StartsWith("!transfercredits "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(message.Content, out string discordName))
            {
                _communications.ReplyTo(message, $"Usage:{Environment.NewLine}!transfercredits [Username#Discriminator]{Environment.NewLine}!transfercredits random");
                return;
            }

            TransferCreditsToSomeone(message, discordName, creditAccounts);
        }

        private void HandleReadCreditsOfSomeone(SocketUserMessage message, string messageText, CreditAccounts creditAccounts)
        {
            if (!messageText.StartsWith("!credits "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out ulong discordId))
            {
                _communications.ReplyTo(message, "Usage: !credits [discordId]");
                return;
            }

            TellAdminCreditAmountOfSomeone(message, creditAccounts, discordId);
        }

        private void AddCredits(SocketUserMessage message, CreditAccounts creditAccounts, ulong discordId, int creditsAmount)
        {
            var account = creditAccounts[discordId];
            account.credits += creditsAmount;
            _communications.ReplyTo(message, $"Added {creditsAmount} credits to {account.discordName}. New Balance: {account.credits}");
        }

        private void RemoveCredits(SocketUserMessage message, CreditAccounts creditAccounts, ulong discordId, int creditsAmount)
        {
            var account = creditAccounts[discordId];
            account.credits -= creditsAmount;
            _communications.ReplyTo(message, $"Removed {creditsAmount} credits from {account.discordName}. New Balance: {account.credits}");
        }

        private void ResetCredits(SocketUserMessage message, CreditAccounts creditAccounts, ulong discordId)
        {
            var account = creditAccounts[discordId];
            account.Reset();
            _communications.ReplyTo(message, $"Reset credits for {account.discordName}. New Balance: {account.credits}");
        }

        private void ResetAllCredits(SocketUserMessage message, CreditAccounts creditAccounts)
        {
            creditAccounts.ResetAll();
            _communications.ReplyTo(message, $"Reset credits for everyone!");
        }

        private void SetCredits(SocketUserMessage message, CreditAccounts creditAccounts, ulong discordId, int creditsAmount)
        {
            var account = creditAccounts[discordId];
            account.credits = creditsAmount;
            _communications.ReplyTo(message, $"Set credits for {account.discordName} to {account.credits}");
        }

        private void TellUserHisCreditAmount(SocketUserMessage message, CreditAccounts creditAccounts)
        {
            var userAccount = creditAccounts[message.Author.Id];
            var creditAmount = userAccount.credits;
            _communications.ReplyTo(message, $@"You currently have {creditAmount} credits.");
        }

        private void TellAdminCreditAmountOfSomeone(SocketUserMessage message, CreditAccounts creditAccounts, ulong discordId)
        {
            var userAccount = creditAccounts[discordId];
            var userName = userAccount.discordName;
            var creditAmount = userAccount.credits;
            _communications.ReplyTo(message, $@"{userName} currently has {creditAmount} credits.");
        }

        private void TransferCreditsToSomeone(SocketUserMessage message, string targetUsername, CreditAccounts creditAccounts)
        {
            var userAccount = creditAccounts[message.Author.Id];
            var creditAmount = userAccount.credits;

            CreditAccount targetAccount = null;
            if (targetUsername.ToLower() == "random")
            {
                targetAccount = creditAccounts.GetRandomAccount();
            }
            else
            {
                var targetParts = targetUsername.Split('#');
                if (targetParts.Length != 2 || targetParts[0].Length < 1 || targetParts[1].Length != 4)
                {
                    _communications.ReplyTo(message, $"Cannot find user {targetUsername}. Please give the full Discord Username including the discriminator, formatted as such: Username#1234");
                    return;
                }

                var targetName = targetParts[0];
                var targetDiscriminator = targetParts[1];
                var userId = _communications.GetUserId(targetName, targetDiscriminator);

                if (userId == 0)
                {
                    _communications.ReplyTo(message, $"Cannot find user {targetUsername}. The user needs to be online and be in this server.");
                    return;
                }

                targetAccount = creditAccounts[userId];
            }

            userAccount.credits -= creditAmount;
            targetAccount.credits += creditAmount;
            _communications.ReplyTo(message, $@"You have transferred your entire balance of {creditAmount} credits to {targetAccount.discordName}! Their new balance: {targetAccount.credits}");
        }
    }
}