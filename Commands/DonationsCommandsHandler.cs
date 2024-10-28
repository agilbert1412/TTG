using System;
using System.IO;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using TTGHotS.Discord;
using TTGHotS.Events;

namespace TTGHotS.Commands
{
    internal class DonationsCommandsHandler
    {
        private readonly IBotCommunicator _communications;
        private readonly ChannelSet _channels;

        public DonationsCommandsHandler(IBotCommunicator discord, ChannelSet channels)
        {
            _communications = discord;
            _channels = channels;
        }

        public void HandleEventsDonationCommands(SocketUserMessage message, CreditAccounts accounts)
        {
            HandleDonationCommand(message, accounts);
        }

        private void HandleDonationCommand(SocketUserMessage message, CreditAccounts accounts)
        {
            // 'St. John Johnson just donated $50.00 with the message "Happy National Child Health Day"!'
            var donationRegex = new Regex(@"(.+) just donated \$(.+) with the message ""(.*)""!", RegexOptions.IgnoreCase);
            if (!donationRegex.IsMatch(message.Content))
            {
                _communications.ReplyTo(message, $"Message detected in the donation channel, but doesn't appear to be a donation...");
                return;
            }

            var match = donationRegex.Match(message.Content);
            var groups = match.Groups;

            if (groups.Count < 4)
            {
                _communications.ReplyTo(message, $"Donation detected, but donation message was improperly formatted. {_channels.AdminPing} can you help?");
                return;
            }

            var name = groups[1].Value;
            var donationMessage = groups[3].Value;
            if (!double.TryParse(groups[2].Value, out var donationAmount))
            {
                _communications.ReplyTo(message, $"Donation detected, but couldn't read the dollar amount. {_channels.AdminPing} can you help?");
                return;
            }

            var creditsEarned = (int)Math.Round(donationAmount * 100);
            var discordIdRegex = new Regex("([^ ]+)#([0-9]{4})");

            if (!discordIdRegex.IsMatch(donationMessage))
            {
                _communications.ReplyTo(message, $"Donation detected, but couldn't find username. {_channels.AdminPing} can you help?");
                return;
            }

            var donatorMatch = discordIdRegex.Match(donationMessage);
            var donatorGroups = donatorMatch.Groups;

            if (donatorGroups.Count < 3)
            {
                _communications.ReplyTo(message, $"Donation detected, but username was improperly formatted. {_channels.AdminPing} can you help?");
                return;
            }

            var discordName = donatorGroups[1].Value;
            var discordDiscriminator = donatorGroups[2].Value;
            var discordId = _communications.GetUserId(discordName, discordDiscriminator);

            if (discordId == 0)
            {
                _communications.ReplyTo(message, $"Donation detected, but couldn't find user {discordName}#{discordDiscriminator}. {_channels.AdminPing} can you help?");
                return;
            }

            var account = accounts[discordId];
            account.credits += creditsEarned;
            _communications.ReplyTo(message, $"Donation registered for <@{account.discordId}>. Added {creditsEarned} credits to your account. New Balance: {account.credits}. Thank you for donating!");
        }
    }
}