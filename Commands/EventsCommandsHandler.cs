using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TTGHotS.Discord;
using TTGHotS.Events;

namespace TTGHotS.Commands
{
    internal class EventsCommandsHandler
    {
        private readonly IBotCommunicator _communications;
        private readonly CommandReader _commandReader;
        private readonly XmlHandler _xml;
        private readonly HelpProvider _helpProvider;

        public EventsCommandsHandler(IBotCommunicator discord, CommandReader commandReader, XmlHandler xml, HelpProvider helpProvider)
        {
            _communications = discord;
            _commandReader = commandReader;
            _xml = xml;
            _helpProvider = helpProvider;
        }

        public void HandleEventsAdminCommands(SocketUserMessage message, string messageText, EventCollection events, EventQueue eventQueue, Goals goals)
        {
            HandleQueueEvent(message, messageText, events, eventQueue);
            HandleTriggerEvent(message, messageText, events, eventQueue);
            HandleSetBank(message, messageText, events, goals);
            HandleSetMission(message, messageText);
            HandleSetGlobalPriceMultiplier(message, messageText, events);
            HandleGlobalPause(message, messageText, eventQueue);
        }

        public async Task HandleEventsUserCommands(SocketUserMessage message, string messageText, CreditAccounts creditAccounts, EventCollection events,
            EventQueue eventQueue, Goals goals)
        {
            HandleCommandBank(message, messageText, events);
            await HandleCommandPurchase(message, messageText, creditAccounts, events, eventQueue, goals);
            await HandleCommandPay(message, messageText, creditAccounts, events, eventQueue, goals);
            HandleGetGlobalPriceMultiplier(message, messageText, events);
        }

        private void HandleQueueEvent(SocketUserMessage message, string messageText, EventCollection events, EventQueue eventQueue)
        {
            if (!messageText.StartsWith("!queueevent "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out string eventName))
            {
                _communications.ReplyTo(message, "Usage: !queueevent [eventName]");
                return;
            }

            var chosenEvent = events.GetEvent(eventName);

            if (chosenEvent == null)
            {
                _communications.ReplyTo(message, $"{eventName} is not a valid event");
                return;
            }

            AddOrIncrementEventInQueue(message.Author.Username, chosenEvent, eventQueue);
            _communications.ReplyTo(message, $"Queued up one instance of {chosenEvent.name}.");
            eventQueue.PrintToConsole();
        }

        private void HandleTriggerEvent(SocketUserMessage message, string messageText, EventCollection events, EventQueue eventQueue)
        {
            if (!messageText.StartsWith("!triggerevent "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out string eventName))
            {
                _communications.ReplyTo(message, "Usage: !triggerevent [eventName]");
                return;
            }

            var chosenEvent = events.GetEvent(eventName);

            if (chosenEvent == null)
            {
                _communications.ReplyTo(message, $"{eventName} is not a valid event");
                return;
            }

            var forcedEvent = new Event();
            forcedEvent.name = eventName;
            forcedEvent.SetBank(1);
            forcedEvent.SetCost(1);
            forcedEvent.donationType = "onetime";
            forcedEvent.mission = Mission.ALL;
            forcedEvent.tier = 0;
            var queuedForcedEvent = new QueuedEvent(forcedEvent);
            queuedForcedEvent.queueCount = 1;
            queuedForcedEvent.username = message.Author.Username;
            eventQueue.PushAtBeginning(queuedForcedEvent);
            _communications.ReplyTo(message, $"Forced {forcedEvent.name} immediately.");

            eventQueue.PrintToConsole();
        }

        private void HandleSetBank(SocketUserMessage message, string messageText, EventCollection events, Goals goals)
        {
            if (!messageText.StartsWith("!setbank "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out string eventName, out var bankAmount))
            {
                _communications.ReplyTo(message, "Usage: !setbank [eventName] [bankAmount]");
                return;
            }

            if (bankAmount < 0)
            {
                _communications.ReplyTo(message, $"{bankAmount} is not a valid amount of credits");
                return;
            }

            eventName = eventName.ToLower().Replace(" ", "");

            foreach (var e in events.ToList())
            {
                if (e.name.ToLower().Replace(" ", "") == eventName)
                {
                    e.SetBank(bankAmount);
                    _communications.ReplyTo(message, $"{e.name} set to {e.GetBank()} credits.");
                }
            }

            foreach (var g in goals.ToList())
            {
                if (g.mission.ToLower().Replace(" ", "") == eventName)
                {
                    g.currentBank = bankAmount;
                    _communications.ReplyTo(message, $"{g.mission} set to {g.currentBank} credits.");
                }
            }
        }

        private void HandleSetMission(SocketUserMessage message, string messageText)
        {
            if (!messageText.StartsWith("!setmission "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out string mission))
            {
                _communications.ReplyTo(message, "Usage: !setmission [mission]");
                return;
            }

            var planet = GetPlanetFromMission(mission);
            _xml.WriteXML(XmlHandler.XML_PLANET, planet);
            _communications.ReplyTo(message, $"Force set: {mission} and {planet}!");
        }

        private void HandleSetGlobalPriceMultiplier(SocketUserMessage message, string messageText, EventCollection events)
        {
            if (!messageText.StartsWith("!setmultiplier "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out double multiplier))
            {
                _communications.ReplyTo(message, "Usage: !setmultiplier [multiplier]");
                return;
            }

            events.CurrentMultiplier = multiplier;
            _communications.SetStatusMessage($"your donations. Price Multiplier: {events.CurrentMultiplier}", ActivityType.Listening);
            _communications.ReplyTo(message, $"Set global event price multiplier to {multiplier}");
            _helpProvider.SendAllEventsHelpMessages(events);
        }

        private void HandleGlobalPause(SocketUserMessage message, string messageText, EventQueue eventQueue)
        {
            if (messageText.StartsWith("!pause"))
            {
                eventQueue.Pause();
                _communications.ReplyTo(message, $"All events are now paused");
                return;
            }

            if (messageText.StartsWith("!unpause") || messageText.StartsWith("!resume"))
            {
                eventQueue.Unpause();
                _communications.ReplyTo(message, $"All events are now resumed");
                return;
            }
        }

        private void HandleGetGlobalPriceMultiplier(SocketUserMessage message, string messageText, EventCollection events)
        {
            if (!messageText.Equals("!prices"))
            {
                return;
            }

            _communications.ReplyTo(message, $"The global event price is currently {events.CurrentMultiplier}");
        }

        private void HandleCommandBank(SocketUserMessage message, string messageText, EventCollection events)
        {
            if (!messageText.StartsWith("!bank "))
            {
                return;
            }

            if (!_commandReader.IsCommandValid(messageText, out string eventName))
            {
                _communications.ReplyTo(message, "Usage: !bank [eventName]");
                return;
            }

            CheckEventBank(message, events, eventName);
        }

        private async Task HandleCommandPurchase(SocketUserMessage message, string messageText, CreditAccounts creditAccounts, EventCollection events,
            EventQueue eventQueue, Goals goals)
        {
            if (!messageText.StartsWith("!purchase "))
            {
                return;
            }

            Console.WriteLine("Purchase: " + messageText);

            if (!_commandReader.IsCommandValid(messageText, out string eventName))
            {
                _communications.ReplyTo(message, "Usage: !purchase [eventName]");
                return;
            }

            var chosenEvent = events.GetEvent(eventName);
            if (chosenEvent == null)
            {
                _communications.ReplyTo(message, $"{eventName} is not a valid event");
                return;
            }

            var costForNextStack = chosenEvent.GetCostToNextActivation(events.CurrentMultiplier);

            LogPay(message.Author.Username, costForNextStack);
            await PayForEvent(message, creditAccounts, events, eventQueue, goals, chosenEvent, costForNextStack);
        }

        private async Task HandleCommandPay(SocketUserMessage message, string messageText, CreditAccounts creditAccounts, EventCollection events, EventQueue eventQueue,
            Goals goals)
        {
            if (!messageText.StartsWith("!pay "))
            {
                return;
            }

            Console.WriteLine("Pay: " + messageText);
            if (!_commandReader.IsCommandValid(messageText, out string eventName, out var creditsToPay))
            {
                _communications.ReplyTo(message, "Usage: !pay [eventName] [creditAmount]");
                return;
            }

            LogPay(message.Author.Username, creditsToPay);

            if (creditsToPay <= 0)
            {
                _communications.ReplyTo(message, $"{creditsToPay} is not a valid amount of credits");
                return;
            }

            await PayForEvent(message, creditAccounts, events, eventQueue, goals, eventName, creditsToPay);
        }

        private async Task PayForEvent(SocketUserMessage message, CreditAccounts creditAccounts, EventCollection events,
            EventQueue eventQueue, Goals goals, string eventName, int creditsToPay)
        {
            var chosenEvent = events.GetEvent(eventName);
            if (chosenEvent == null)
            {
                _communications.ReplyTo(message, $"{eventName} is not a valid event");
                return;
            }

            await PayForEvent(message, creditAccounts, events, eventQueue, goals, chosenEvent, creditsToPay);
        }

        private async Task PayForEvent(SocketUserMessage message, CreditAccounts creditAccounts, EventCollection events,
            EventQueue eventQueue, Goals goals, Event chosenEvent, int creditsToPay)
        {
            var userAccount = creditAccounts[message.Author.Id];

            var currentMission = _xml.GetCurrentMission(Format.AsIs);
            if (!chosenEvent.allowedInNoBuild && Mission.NO_BUILD_MISSIONS.Contains(currentMission))
            {
                _communications.ReplyTo(message, $"{chosenEvent.name} is disabled on no-build missions.");
                return;
            }

            if (!chosenEvent.mission.Equals(Mission.ALL, StringComparison.InvariantCultureIgnoreCase) &&
                !chosenEvent.mission.Equals(currentMission, StringComparison.InvariantCultureIgnoreCase))
            {
                _communications.ReplyTo(message,
                    $"{chosenEvent.name} can only be activated on mission '{chosenEvent.mission}', but the current mission is {currentMission}");
                return;
            }

            if (!chosenEvent.IsStackable())
            {
                var costToNextActivation = chosenEvent.GetCostToNextActivation(events.CurrentMultiplier);
                if (creditsToPay > costToNextActivation)
                {
                    creditsToPay = costToNextActivation;
                }
            }

            if (creditsToPay > userAccount.GetCredits())
            {
                _communications.ReplyTo(message, $"You cannot afford to pay {creditsToPay} credits. Balance: {userAccount.GetCredits()}");
                return;
            }

            await goals.AssignCreditsToGoal(creditsToPay, eventQueue);
            chosenEvent.AddToBank(creditsToPay);
            userAccount.RemoveCredits(creditsToPay);

            var numberOfActivations = TriggerEventAsNeeded(message.Author.Username, chosenEvent, eventQueue, events);

            if (numberOfActivations > 0)
            {
                _communications.ReplyTo(message,
                    $"Received {creditsToPay} credits from {message.Author.Username} to activate {chosenEvent.name} {numberOfActivations} times!");
            }
            else
            {
                _communications.ReplyTo(message,
                    $"Received {creditsToPay} credits from {message.Author.Username}.  {chosenEvent.name} is now at {chosenEvent.GetBank()}/{chosenEvent.GetMultiplierCost(events.CurrentMultiplier)}.");
            }

            eventQueue.PrintToConsole();
        }

        private int TriggerEventAsNeeded(string senderName, Event chosenEvent, EventQueue eventQueue, EventCollection events)
        {
            var numberOfActivations = 0;
            while (chosenEvent.GetBank() >= chosenEvent.GetMultiplierCost(events.CurrentMultiplier))
            {
                chosenEvent.CallEvent(events.CurrentMultiplier);
                chosenEvent.CalculateNewCost();
                LogEvent(senderName, chosenEvent);
                AddOrIncrementEventInQueue(senderName, chosenEvent, eventQueue);
                numberOfActivations++;
            }

            return numberOfActivations;
        }

        private void AddOrIncrementEventInQueue(string senderName, Event chosenEvent, EventQueue eventQueue)
        {
            var isInQueue = false;
            foreach (var qe in eventQueue)
            {
                if (qe.baseEventName == chosenEvent.name)
                {
                    if (qe.BaseEvent.donationType == "dynamic")
                    {
                        qe.queueCount += chosenEvent.tier;
                    }
                    else
                    {
                        qe.queueCount += 1;
                    }

                    isInQueue = true;
                    Console.WriteLine(
                        $"Increased queue count of {chosenEvent.name} to {qe.queueCount}.");
                }
            }

            AddEventToQueueIfNeeded(senderName, isInQueue, chosenEvent, eventQueue);
        }

        private void AddEventToQueueIfNeeded(string senderName, bool isInQueue, Event chosenEvent, EventQueue eventQueue)
        {
            if (isInQueue)
            {
                return;
            }

            var invokedEvent = new QueuedEvent(chosenEvent);
            invokedEvent.username = senderName;

            eventQueue.QueueEvent(invokedEvent);
            if (invokedEvent.BaseEvent.donationType == "dynamic")
            {
                invokedEvent.queueCount = invokedEvent.BaseEvent.tier;
            }
            else
            {
                invokedEvent.queueCount = 1;
            }

            Console.WriteLine($"Added {invokedEvent.baseEventName} to queue.");
        }

        public void LogEvent(string senderName, Event calledEvent)
        {
            var localDate = DateTime.Now;
            using (var w = File.AppendText("EventLog.txt"))
            {
                w.WriteLine($"[{localDate}] {senderName} activated {calledEvent.name}.");
            }
        }

        public void LogPay(string senderName, int payAmount)
        {
            var localDate = DateTime.Now;
            using (var w = File.AppendText("PayLog.txt"))
            {
                w.WriteLine($"[{localDate}] {senderName} paid {payAmount} credits.");
            }
        }

        private void CheckEventBank(SocketUserMessage message, EventCollection events, string eventName)
        {
            var invokedEvent = events.GetEvent(eventName);
            if (invokedEvent == null)
            {
                _communications.ReplyTo(message, $"{eventName} is not a valid Event.");
                return;
            }

            _communications.ReplyTo(message, $"{invokedEvent.name} is at {invokedEvent.GetBank()}/{invokedEvent.GetMultiplierCost(events.CurrentMultiplier)} credits.");
        }

        private string GetPlanetFromMission(string mission)
        {
            switch (mission.ToLower().Replace(" ", ""))
            {
                case "foraiur!":
                    return "AiurA";
                case "thegrowingshadow":
                    return "AiurA";
                case "spearofadun":
                    return "AiurA";
                case "amon'sreach":
                    return "Shakuras";
                case "laststand":
                    return "Shakuras";
                case "skyshield":
                    return "Korhal";
                case "brothersinarms":
                    return "Korhal";
                case "templar'scharge":
                    return "Moebius";
                case "templeofunification":
                    return "Ulnar";
                case "theinfinitecycle":
                    return "Ulnar";
                case "harbingerofoblivion":
                    return "Ulnar";
                case "forbiddenweapon":
                    return "PurifierA";
                case "unsealingthepast":
                    return "PurifierB";
                case "purification":
                    return "PurifierB";
                case "stepsoftherite":
                    return "Taldarim";
                case "rak'shir":
                    return "Taldarim";
                case "templar'sreturn":
                    return "Aiur2";
                case "thehost":
                    return "Aiur2";
                case "salvation":
                    return "Aiur2";
                case "intothevoid":
                    return "Epilogue";
                case "theessenceofeternity":
                    return "Epilogue";
                case "amon'sfall":
                    return "Epilogue";
                default:
                    return "ERROR";
            }
        }
    }
}