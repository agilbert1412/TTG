using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TTGHotS.Commands;
using TTGHotS.Discord;
using TTGHotS.Events;

namespace TTGHotS
{
    internal class TTGModule
    {
        private readonly IBotCommunicator _communications;

        private readonly ChannelSet _activeChannels = ChannelSet.ExtraLifeChannels;

        private readonly CreditsCommandsHandler _creditsCommandsHandler;
        private readonly EventsCommandsHandler _eventsCommandsHandler;
        private readonly DonationsCommandsHandler _donationsCommandsHandler;
        private readonly CommandReader _commandReader;
        private readonly XmlHandler _xml;
        private readonly HelpProvider _helpProvider;
        private readonly CreditAccounts _accounts;
        private readonly EventCollection _events;
        private readonly EventQueue _eventQueue;
        private readonly Goals _goals;

        public static Random random = new Random();
        public static string hybridstr = "0000000000000000000000000000000000000000111111111111111111111111111111111111111122222222444444443333";
        public static List<string> SOA_EVENTS = new List<string> { "deploypylon", "chronosurge", "reinforcements", "orbitalstrike", "temporalfield", "solarlance", "massrecall", "shieldovercharge", "fenix", "purifierbeam", "solarbombardment", "globalcloak" };
        public static List<string> SOA_IDS = new List<string> { "GPTier1Power1", "GPTier1Power2", "GPTier1Power3", "GPTier2Power1", "GPTier2Power2", "GPTier2Power3", "GPTier4Power1", "GPTier4Power2", "GPTier4Power3", "GPTier6Power1", "GPTier6Power3", "GPTier6Power2" };
        public static Dictionary<string, string> SOA_DICT = SOA_EVENTS.Zip(SOA_IDS, (k, v) => new { Key = k, Value = v }).ToDictionary(x => x.Key, x => x.Value);

        public const string EVENTS_FILE = "EventsList.json";
        public const string GOALS_FILE = "GoalsList.json";
        public const string CREDITS_FILE = "Credits.json";
        
        private string _mission;

        //public static Goal campaignGoal = new Goal(500000, "campaign", "onetime");

        public TTGModule(IBotCommunicator communications)
        {
            _communications = communications;
            _commandReader = new CommandReader();
            _xml = new XmlHandler();
            _goals = new Goals(_communications, _xml, _activeChannels.EventsChannel);
            _helpProvider = new HelpProvider(_communications, _xml, _activeChannels, _goals);
            _creditsCommandsHandler = new CreditsCommandsHandler(_communications, _commandReader);
            _eventsCommandsHandler = new EventsCommandsHandler(_communications, _commandReader, _xml, _helpProvider);
            _donationsCommandsHandler = new DonationsCommandsHandler(_communications, _activeChannels);
            _accounts = new CreditAccounts(_communications);
            _events = new EventCollection(_communications);
            _eventQueue = new EventQueue(_communications);

            SetupData();
            Console.WriteLine($"SoA Read: {_xml.GetSpearOfAdunAbility(0)}");

            Console.WriteLine(_goals.Count + " is the total goal count.");
            Console.WriteLine(_events.Count + " is the total events count.");

            // ClearBankDEVONLY();
            //return;
        }

        public async Task InitializeAsync()
        {
            await _goals.AssignCreditsToGoal(0, _eventQueue);
        }

        private void CheckSoA()
        {
            var soaQueue = _eventQueue.GetSoaQueue();
            Console.WriteLine($"SoA queue size: {soaQueue.Count}");
            if (soaQueue.Count == 0)
            {
                return;
            }

            foreach (var soaEvent in soaQueue)
            {
                Console.WriteLine("SoA Queue: " + soaEvent);
            }

            for (var i = 0; i < 4; i++)
            {
                var ability = _xml.GetSpearOfAdunAbility(i);
                if (ability == null || ability.ToLower() == "none")
                {
                    Console.WriteLine($"SoA Slot {i}: {ability}");
                    var abilityToSend = soaQueue.First();
                    soaQueue.RemoveAt(0);
                    var abilityId = SOA_DICT[abilityToSend];
                    Console.WriteLine($"Writing {abilityId} to Bank Slot {i}");
                    _xml.SetSpearOfAdunAbility(i, abilityId);
                    if (soaQueue.Count == 0)
                    {
                        return;
                    }
                }
            }
        }

        public async Task ExecuteTTGCommand(SocketUserMessage message)
        {
            CheckCurrentMission();
            CheckSoA();

            var messageText = message.Content.ToLower();
            var sender = message.Author;
            var senderName = sender.Username;

            Console.WriteLine($"{senderName} said '{messageText}'");

            HandleAdminCommands(message, messageText);
            await HandleUserCommands(message, messageText, senderName);
            await HandleDonationCommands(message);
            ExportData();

            DequeueUntilQueueEmpty();
        }

        private void CheckCurrentMission()
        {
            var currentMission = _xml.GetCurrentMission(Format.LowerCase);

            if (currentMission == _mission)
            {
                return;
            }

            _mission = currentMission;
            _helpProvider.SendEventsHelpForCurrentMission(_events);
        }

        private async void HandleAdminCommands(SocketUserMessage message, string messageText)
        {
            if (!IsInAdminChannel(message))
            {
                return;
            }

            if (messageText == "test")
            {
                await message.ReplyAsync("Toast");
            }

            _creditsCommandsHandler.HandleCreditsAdminCommands(message, messageText, _accounts);
            _eventsCommandsHandler.HandleEventsAdminCommands(message, messageText, _events, _eventQueue, _goals);

            if (messageText.Equals("!help"))
            {
                _helpProvider.SendAllHelpMessages(_events);
            }
        }

        private async Task HandleUserCommands(SocketUserMessage message, string messageText, string senderName)
        {
            if (!(IsInAdminChannel(message) || IsInEventsChannel(message)))
            {
                return;
            }

            await _creditsCommandsHandler.HandleCreditsUserCommands(message, messageText, _accounts);
            await _eventsCommandsHandler.HandleEventsUserCommands(message, messageText, _accounts, _events, _eventQueue, _goals);
        }

        private async Task HandleDonationCommands(SocketUserMessage message)
        {
            if (!(IsInDonationsChannel(message)))
            {
                return;
            }

            await _donationsCommandsHandler.HandleEventsDonationCommands(message, _accounts);
        }

        private void ExportData()
        {
            ExportEvents();
            ExportGoals();
            ExportCredits();
            // Export Queue
        }

        private /*async*/ void DequeueUntilQueueEmpty()
        {
            if (_eventQueue.IsEmpty)
            {
                return;
            }

            Dequeue();

            /*var nextDequeue = */
            Task.Delay(new TimeSpan(0, 0, 5)).ContinueWith(o => { DequeueUntilQueueEmpty(); });
            /*await nextDequeue;*/
        }

        private void Dequeue()
        {
            if (_eventQueue.IsEmpty || _eventQueue.IsPaused())
            {
                return;
            }

            if (_xml.IsBankFileLocked())
            {
                return;
            }

            var eventToSend = _eventQueue.First;
            var baseEvent = eventToSend.BaseEvent;
            var baseEventName = baseEvent.name;
            Console.WriteLine(
                $"Attempting to dequeue {eventToSend.queueCount} instances of {eventToSend.baseEventName} triggered by {eventToSend.username}.");
            _eventQueue.RemoveFirst();
            _eventQueue.PrintToConsole();

            
            var currentMission = _xml.GetCurrentMission(Format.AsIs);

            if ((eventToSend.baseEventName == "apocalypse" || eventToSend.baseEventName == "puresolarite") && currentMission == "Templar's Return")
            {
                Console.WriteLine("Doubled Apoc/Solarite on Templar's Return.");
                eventToSend.queueCount = eventToSend.queueCount * 2;
            }

            if (baseEventName.Contains("missiongoal"))
            {
                var eventMission = baseEventName.Split("missiongoal")[1];
                if (eventMission != currentMission)
                {
                    Console.WriteLine("Blocked mission goal from " + eventMission + " since we are on " +
                                      currentMission + ".");
                    return;
                }
                else
                {
                    Console.WriteLine("Renaming it to missiongoal.");
                    baseEventName = "missiongoal";
                }
            }

            if (baseEventName.ToLower() == "planetgoalphantoms of the void")
            {
                _xml.WriteXML(XmlHandler.XML_ARGS, GenHybrid(eventToSend.queueCount));
            }

            if (baseEventName.Contains("planetgoal"))
            {
                baseEventName = "planetgoal";
            }

            _xml.WriteXML(XmlHandler.XML_EVENT, baseEventName);
            _xml.WriteXML(XmlHandler.XML_VALUE, eventToSend.queueCount.ToString());
            _xml.WriteXML(XmlHandler.XML_USERNAME, eventToSend.username);
            if (baseEventName.ToLower() == "infinitecycle")
            {
                _xml.WriteXML(XmlHandler.XML_ARGS, GenHybrid(eventToSend.queueCount));
            }

            /*if (eventToSend.baseEvent.name.ToLower() == "infinitecycle")
            {
                WriteXML(XMLARGS, GenHybrid(eventToSend.queueCount));
            }*/
            if (SOA_EVENTS.Any(eventToSend.baseEventName.Contains))
            {
                for (int i = 0; i < eventToSend.queueCount; i++)
                {
                    _eventQueue.AddSpearOfAdun(eventToSend.baseEventName);
                    Console.WriteLine($"Added {eventToSend.baseEventName} to the Spear Queue");
                }
            }

            _xml.LockBankFile();
        }

        private void ClearBankDEVONLY()
        {
            //TODO: Plug into an admin command

            Console.WriteLine("DEV ONLY: Reset the bank for everything!");
            _goals.ClearAllBanks();
            _events.ClearAllBanks();
            
            ExportData();
            Environment.Exit(0);
        }

        public string GenHybrid(int count)
        {
            var hybridoutstr = "";
            for (var j = 0; j < count; j++)
            {
                var index = random.Next(hybridstr.Length);
                hybridoutstr += hybridstr[index] + " ";
                hybridstr = hybridstr.Remove(index, 1);
                if (hybridstr.Length <= 5)
                {
                    hybridstr += "0000000000000000000000000000000000000000111111111111111111111111111111111111111122222222444444443333";
                }
            }
            Console.WriteLine("We're sending off " + hybridoutstr + " to ruin Grant's day!");
            return hybridoutstr;
        }

        private void SetupData()
        {
            ImportEvents();
            ImportGoals();
            ImportCredits();
            // Import queue;
        }

        private void ImportEvents()
        {
            _events.ImportFrom(EVENTS_FILE);
        }

        private void ImportGoals()
        {
            _goals.ImportFrom(GOALS_FILE);
        }

        private void ImportCredits()
        {
            _accounts.ImportFrom(CREDITS_FILE);
        }

        private void ExportEvents()
        {
            _events.ExportTo(EVENTS_FILE);
        }

        private void ExportGoals()
        {
            _goals.ExportTo(GOALS_FILE);
        }

        private void ExportCredits()
        {
            _accounts.ExportTo(CREDITS_FILE);
        }

        private bool IsInAdminChannel(SocketMessage message)
        {
            return message.Channel.Id == _activeChannels.AdminChannel;
        }

        private bool IsInEventsChannel(SocketMessage message)
        {
            return message.Channel.Id == _activeChannels.EventsChannel;
        }

        private bool IsInDonationsChannel(SocketMessage message)
        {
            return message.Channel.Id == _activeChannels.DonationsChannel;
        }
    }
}
