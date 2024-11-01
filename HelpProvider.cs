﻿using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
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
        private readonly Goals _goals;

        public HelpProvider(IBotCommunicator discord, XmlHandler xml, ChannelSet channels, Goals goals)
        {
            _communications = discord;
            _xml = xml;
            _channels = channels;
            _goals = goals;
        }

        public async void SendAllHelpMessages(EventCollection events)
        {
            SendAllEventsHelpMessages(events);
            await SendUserCommandsListHelp();
            await SendAdminCommandsListHelp();
        }

        public void SendAllEventsHelpMessages(EventCollection events)
        {
            SendEventsHelpForAllMissions(events);
            SendEventsHelpForCurrentMission(events);
            SendEventsHelpForUnits(events);
            SendEventsHelpForSpearOfAdun(events);
        }

        public async Task SendEventsHelpForAllMissions(EventCollection events)
        {
            await _communications.DeleteAllMessagesInChannel(_channels.HelpGenericEventsChannel);
            Thread.Sleep(200);
            var filteredEvents = events.ToList().Where(x => x.mission == "all" && x.category == "generic");
            await SendEventsListHelp(filteredEvents, "all missions", _channels.HelpGenericEventsChannel, events.CurrentMultiplier);
        }

        public async Task SendEventsHelpForCurrentMission(EventCollection events)
        {
            await _communications.DeleteAllMessagesInChannel(_channels.HelpCurrentMissionEventsChannel);
            Thread.Sleep(200);
            var currentMission = _xml.GetCurrentMission(Format.TitleCase);
            var filteredEvents = events.ToList().Where(x => x.mission.Equals(currentMission, StringComparison.InvariantCultureIgnoreCase) && x.category == "mission");
            await SendCurrentMissionHelpMessage($"The Current Mission is '**{currentMission}**'");
            var goal = _goals.GetGoal(currentMission);
            if (goal != null)
            {
                if (!string.IsNullOrWhiteSpace(goal.missionChanges))
                {
                    await SendCurrentMissionHelpMessage($"*{goal.missionChanges}*");
                }
                
                if (!string.IsNullOrWhiteSpace(goal.goalDescription))
                {
                    await SendCurrentMissionHelpMessage($"**{XmlHandler.MakeTitleCase(goal.timeframe)} Goal [{goal.cost} credits]**: {goal.goalDescription}");
                }
            }

            await SendEventsListHelp(filteredEvents, $"current mission", _channels.HelpCurrentMissionEventsChannel, events.CurrentMultiplier);
        }

        public async Task SendEventsHelpForUnits(EventCollection events)
        {
            await _communications.DeleteAllMessagesInChannel(_channels.HelpUnitSwapEventsChannel);
            Thread.Sleep(200);
            var filteredEvents = events.ToList().Where(x => x.category == "units");
            await SendUnitsHelpMessage($"These event switch all current and future instance of a unit category to one of its 3 variants");
            await SendEventsListHelp(filteredEvents, $"switching unit types", _channels.HelpUnitSwapEventsChannel, events.CurrentMultiplier);
        }

        public async Task SendEventsHelpForSpearOfAdun(EventCollection events)
        {
            await _communications.DeleteAllMessagesInChannel(_channels.HelpSpearOfAdunEventsChannel);
            Thread.Sleep(200);
            var filteredEvents = events.ToList().Where(x => x.category == "soa");
            await SendSpearOfAdunHelpMessage($"These each add one charge of the ability to the top bar, for use by the player at their chosen time.");
            await SendSpearOfAdunHelpMessage($"The player does not get any Spear of Adun abilities on their own and must rely on the audience.");
            await SendEventsListHelp(filteredEvents, $"Spear of Adun abilities", _channels.HelpSpearOfAdunEventsChannel, events.CurrentMultiplier);
        }

        private async Task SendEventsListHelp(IEnumerable<Event> events, string eventsAvailabilityString, ulong channel, double priceMultiplier)
        {
            var eventsListString = $"**Events available for {eventsAvailabilityString}:**" + Environment.NewLine;
            eventsListString = StartListString(eventsListString);

            const string START_GREEN = "\u001b[2;36m";
            const string START_RED = "\u001b[2;31m";
            const string START_YELLOW = "\u001b[2;33m";
            const string END_COLOR = "\u001b[0m";

            var foundAnyEvent = false;
            var alreadySentMessage = false;

            foreach (var eventToDocument in events.ToList())
            {
                foundAnyEvent = true;
                alreadySentMessage = false;

                eventsListString += $"{eventToDocument.name} - Cost: {eventToDocument.GetMultiplierCost(priceMultiplier)} credits" + Environment.NewLine;


                if (eventToDocument.alignment == "positive")
                {
                    eventsListString += START_GREEN;
                }
                else if (eventToDocument.alignment == "negative")
                {
                    eventsListString += START_RED;
                }
                else if (eventToDocument.alignment == "neutral")
                {
                    eventsListString += START_YELLOW;
                }

                eventsListString += "    " + eventToDocument.descriptionAnsi + Environment.NewLine;
                eventsListString += END_COLOR;

                if (eventsListString.Length > 1600)
                {
                    await FinishStringAndSend(channel, eventsListString);
                    Thread.Sleep(200);
                    eventsListString = StartListString("");
                    alreadySentMessage = true;
                }
                else
                {
                    eventsListString += Environment.NewLine;
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
            eventsListString += "```ansi" + Environment.NewLine;
            return eventsListString;
        }

        private async Task FinishStringAndSend(ulong channelId, string eventsListString)
        {
            eventsListString += "```";
            await _communications.SendMessageAsync(channelId, eventsListString);
        }

        private async Task SendUserCommandsListHelp()
        {
            await _communications.DeleteAllMessagesInChannel(_channels.HelpCommandsChannel);
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

            userCommandsListString += "!goal" + Environment.NewLine;
            userCommandsListString += "    Get information about the currently active goal." + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "!transfercredits [discordFullUsername]" + Environment.NewLine;
            userCommandsListString += "    Transfer your entire credit balance to a specific user. Use with Caution" + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "!transfercredits random" + Environment.NewLine;
            userCommandsListString += "    Transfer your entire credit balance to a random person. Use with Caution" + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "!sharecredits [minutes]" + Environment.NewLine;
            userCommandsListString += "    Transfer your entire credit balance to all the players that have been active in the past X minutes. If omitted, defaults to 20 minutes. Use with Caution" + Environment.NewLine + Environment.NewLine;

            userCommandsListString += "```";

            await _communications.SendMessageAsync(_channels.HelpCommandsChannel, userCommandsListString);
        }

        private async Task SendAdminCommandsListHelp()
        {
            await _communications.DeleteAllMessagesInChannel(_channels.AdminHelpChannel);
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

            adminCommandsListString += "!triggergoal" + Environment.NewLine;
            adminCommandsListString += "    Triggers one instance of the current mission's goal. This works even if the goal is a planet or campaign goal" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!setbank [eventName] [bankAmount]" + Environment.NewLine;
            adminCommandsListString += "    Sets the current bank of the given event to a specific value. This can queue one or many instances of the event if the bank crosses the cost threshold" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "!pause and !unpause" + Environment.NewLine;
            adminCommandsListString += "    Pauses and Resumes the dequeueing of events. Viewers can still purchase events, but they will not trigger in-game, they will wait patiently in the queue." + Environment.NewLine + Environment.NewLine;

            // adminCommandsListString += "!setmission [mission]" + Environment.NewLine;
            // adminCommandsListString += "    Sets the current mission" + Environment.NewLine + Environment.NewLine;

            adminCommandsListString += "```";

            await _communications.SendMessageAsync(_channels.AdminHelpChannel, adminCommandsListString);
        }

        private async Task SendCurrentMissionHelpMessage(string message)
        {
            await _communications.SendMessageAsync(_channels.HelpCurrentMissionEventsChannel, message);
        }

        private async Task SendUnitsHelpMessage(string message)
        {
            await _communications.SendMessageAsync(_channels.HelpUnitSwapEventsChannel, message);
        }

        private async Task SendSpearOfAdunHelpMessage(string message)
        {
            await _communications.SendMessageAsync(_channels.HelpSpearOfAdunEventsChannel, message);
        }
    }
}
