using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TTGHotS.Discord;
using TTGHotS.Events;

namespace TTGHotS
{
    internal class HelpProvider
    {
        private readonly IBotCommunicator _communications;
        private readonly XmlHandler _xml;
        private readonly ChannelSet _channels;

        public HelpProvider(IBotCommunicator discord, XmlHandler xml, ChannelSet channels)
        {
            _communications = discord;
            _xml = xml;
            _channels = channels;
        }

        public async void SendAllHelpMessages(EventCollection events)
        {
            SendAllEventsHelpMessages(events);
            await SendUserCommandsListHelp();
            await SendAdminCommandsListHelp();
        }

        public async void SendAllEventsHelpMessages(EventCollection events)
        {
            await SendEventsListHelp(events, "all", _channels.HelpGenericEventsChannel);
            SendEventsHelpForCurrentMission(events);
        }

        public async void SendEventsHelpForCurrentMission(EventCollection events)
        {
            var currentMission = _xml.GetCurrentMission(Format.TitleCase);
            await SendEventsListHelp(events, currentMission, _channels.HelpCurrentMissionEventsChannel);
        }

        private async Task SendEventsListHelp(EventCollection events, string missionFilter, ulong channel)
        {
            _communications.DeleteAllMessagesInChannel(channel);
            Thread.Sleep(200);

            var lowerCaseFilter = missionFilter.ToLower();

            var isForAllMissions = lowerCaseFilter == "all";
            var eventsAvailabilityString = isForAllMissions ? "all missions" : $"current mission: {missionFilter}";
            var eventsListString = $"**Events available for {eventsAvailabilityString}:**" + Environment.NewLine;
            eventsListString = StartListString(eventsListString);

            var foundAnyEvent = false;
            var alreadySentMessage = false;

            foreach (var eventToDocument in events.ToList())
            {
                if (eventToDocument.mission.ToLower() == lowerCaseFilter)
                {
                    foundAnyEvent = true;
                    alreadySentMessage = false;

                    eventsListString += $"{eventToDocument.name} - Cost: {eventToDocument.GetMultiplierCost(events.CurrentMultiplier)} credits - Mission: [{eventToDocument.mission}]" + Environment.NewLine;
                    eventsListString += "    " + eventToDocument.description + Environment.NewLine + Environment.NewLine;

                    if (eventsListString.Length > 1600)
                    {
                        await FinishStringAndSend(channel, eventsListString);
                        Thread.Sleep(200);
                        eventsListString = StartListString("");
                        alreadySentMessage = true;
                    }
                }
            }

            if (alreadySentMessage)
            {
                return;
            }

            if (!foundAnyEvent)
            {
                eventsListString += "None" + Environment.NewLine;
            }

            await FinishStringAndSend(channel, eventsListString);
        }

        private static string StartListString(string eventsListString)
        {
            eventsListString += "```" + Environment.NewLine;
            return eventsListString;
        }

        private async Task FinishStringAndSend(ulong channelId, string eventsListString)
        {
            eventsListString += "```";
            await _communications.SendMessage(channelId, eventsListString);
        }

        private async Task SendUserCommandsListHelp()
        {
            _communications.DeleteAllMessagesInChannel(_channels.HelpCommandsChannel);
            Thread.Sleep(200);

            var userCommandsListString = "**Commands:**" + Environment.NewLine;
            userCommandsListString += "```" + Environment.NewLine;

            userCommandsListString += "!credits" + Environment.NewLine;
            userCommandsListString += "    Gets the current amount of credits in your wallet" + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "!prices" + Environment.NewLine;
            userCommandsListString += "    Gets the current global price multiplier" + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "!bank [eventName]" + Environment.NewLine;
            userCommandsListString += "    Gets the current bank of an event and the number of credits required to activate it" + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "!purchase [eventName]" + Environment.NewLine;
            userCommandsListString += "    Pays however many credits are required to activate the chosen event exactly once" + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "!pay [eventName] [creditAmount]" + Environment.NewLine;
            userCommandsListString += "    Pay credits into an event's bank. If the cost threshold is reached, the event will activate. This can activate the event multiple times, if enough credits are paid." + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "!transfercredits [discordFullUsername]" + Environment.NewLine;
            userCommandsListString += "    Transfer your entire credit balance to a specific user. Use with Caution" + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "!transfercredits random" + Environment.NewLine;
            userCommandsListString += "    Transfer your entire credit balance to a random person. Use with Caution" + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "```";

            await _communications.SendMessage(_channels.HelpCommandsChannel, userCommandsListString);
        }

        private async Task SendAdminCommandsListHelp()
        {
            _communications.DeleteAllMessagesInChannel(_channels.AdminHelpChannel);
            Thread.Sleep(200);

            var adminCommandsListString = "**Admin Commands:**" + Environment.NewLine;
            adminCommandsListString += "```" + Environment.NewLine;

            adminCommandsListString += "!credits [discordId]" + Environment.NewLine;
            adminCommandsListString += "    Gets the amount of credits in a user's wallet" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!addcredits [discordId] [amount]" + Environment.NewLine;
            adminCommandsListString += "    Adds credits to a user's wallet" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!removecredits [discordId] [amount]" + Environment.NewLine;
            adminCommandsListString += "    Removes credits from a user's wallet" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!setcredits [discordId] [amount]" + Environment.NewLine;
            adminCommandsListString += "    Set a user's wallet to a specific amount" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!resetcredits [discordId]" + Environment.NewLine;
            adminCommandsListString += "    Resets a user's wallet to its starting credits" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!resetallcredits" + Environment.NewLine;
            adminCommandsListString += "    Resets everyone's credits to the starting amount. USE WITH CAUTION" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!setmultiplier [multiplier]" + Environment.NewLine;
            adminCommandsListString += "    Sets the global price multiplier for all events. Default: 1" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!queueevent [eventName]" + Environment.NewLine;
            adminCommandsListString += "    Queues up one instance of the given event" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!triggerevent [eventName]" + Environment.NewLine;
            adminCommandsListString += "    Triggers one instance of the given event right now, bypassing the queue" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!setbank [eventName] [bankAmount]" + Environment.NewLine;
            adminCommandsListString += "    Sets the current bank of the given event to a specific value. This can queue one or many instances of the event if the bank crosses the cost threshold" + Environment.NewLine + Environment.NewLine;

            // adminCommandsListString += "!setmission [mission]" + Environment.NewLine;
            // adminCommandsListString += "    Sets the current mission" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "```";

            await _communications.SendMessage(_channels.AdminHelpChannel, adminCommandsListString);
        }
    }
}
